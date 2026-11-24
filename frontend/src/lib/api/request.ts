import { DataType } from "$lib/business/typing/datatype.ts";
import { Validate } from "$lib/business/typing/validations.ts";
import { err, ok, type Result } from '$lib/result.ts';
import { activate } from "../business/typing/activator.ts";

// deno-lint-ignore no-explicit-any
type Ctor = new (...args: any) => object;
type TypedMapper<T,R> = (x: T) => Result<R>;

class ApiRequest {
  @DataType('string')
  endpoint!: string

  @DataType('string')
  method!: string

  @DataType('string', { optional: true })
  authorization?: string

  @DataType('string')
  contentType!: string

  @DataType('string', { isMap: true, optional: true })
  queryParams!: Map<string, string>;

  // deno-lint-ignore no-explicit-any
  mapper!: (x: any) => Result<any>

  public toString(): string {
    return `Request [
      endpoint: ${this.endpoint};
      method: ${this.method};
      authorization: ${this.authorization ? "***" : null} ]
      contentType: ${this.contentType}
      queryParams: ${this.queryParams?.entries.length ?? 0}
    `;
  }
}

export class StatusOnly {
  constructor() {}
}

export class RequestBuilder<T extends Ctor, R> {
  ctx: ApiRequest
  ctor: T

  private readonly host = import.meta.env.VITE_API_ENDPOINT;


  constructor(ctor: T) {
    this.ctx = {
      mapper: (x: unknown) => ok(x),
      endpoint: "",
      method: "",
      contentType: "application/json",
      queryParams: new Map<string, string>()
    };
    this.ctor = ctor;
  }

  public To(endpoint: string): RequestBuilder<T, R> {
    this.ctx.endpoint = endpoint;
    return this;
  }

  public Method(method: 'GET' | 'POST' | 'DELETE'): RequestBuilder<T, R> {
    this.ctx.method = method;
    return this;
  }

  public WithAuthorizationToken(userToken: string): RequestBuilder<T, R> {
    this.ctx.authorization = userToken;
    return this;
  }

  public WithContentType(contentType: string): RequestBuilder<T, R> {
    this.ctx.contentType = contentType;
    return this;
  }

  public WithQueryParam(name: string, value: string): RequestBuilder<T, R> {
    this.ctx.queryParams.set(name, value);
    return this;
  }

  public WithMapper(mapper: (res: T) => Result<R>): RequestBuilder<T, R>  {
    this.ctx.mapper = mapper;
    return this;
  }

  public async Execute(): Promise<Result<R>> {
    const apiResponse = await this.DoRequest();
    if (!apiResponse.ok) {
      return err(apiResponse.error!)
    }

    let responseData: string;
    try {
      responseData = JSON.stringify(await apiResponse.data!.json());
    }
    catch (e) {
      return err(`Error: unable to parse api response: ${e}`)
    }

    const parsed = activate(this.ctor, JSON.parse(responseData));

    if (!parsed.ok) {
      return err(`Error: got a valid api response, but was unable to parse to desired type: ${this.ctx.toString()}`);
    }

    return this.GetMapper()(parsed.data!)
  }

  public async ExecuteStatusOnly(): Promise<Result<number>> {
    const apiResponse = await this.DoRequest();
    if (!apiResponse.ok) {
      return err(apiResponse.error!)
    }

    return ok(apiResponse.data!.status);
  }

  private GetMapper(): TypedMapper<InstanceType<T>, R> {
    return this.ctx.mapper as TypedMapper<InstanceType<T>, R>;
  }

  private async DoRequest(): Promise<Result<Response>> {
    if (!Validate(ApiRequest, this.ctx) || this.ctor === undefined) {
      return err(`Unable to validate API request: ${this.ctx.toString()}`)
    }

    const endpoint = this.GetEndpoint();
    const queryParams = this.GetQueryParams();
    const uri = endpoint + queryParams;

    const headers = this.BuildHeaders();

    try {
      return ok(await fetch(uri, { headers: headers }));
    } catch (e) {
      return err(`Unable to make request: ${e}`)
    }
  }

  private BuildHeaders(): Record<string, string> {
    const h: Record<string, string> = {
      "Content-Type": this.ctx.contentType
    };

    if (this.ctx.authorization) {
      h["Authorization"] = this.ctx.authorization;
    }

    return h;
  }

  private GetEndpoint(): string {
    // API host + our endpoint, trimming leading and trailing slash 
    return `${this.host}/${this.ctx.endpoint!.replace(/^\//, "").replace(/\/$/, "")}`;
  }

  private GetQueryParams(): string {
    let queryParams = '?';

    for (const [key, value] of this.ctx.queryParams.entries()) {
      queryParams + `${key}=${value}&`
    }

    queryParams = queryParams.slice(0, -1);

    return queryParams;
  }
}