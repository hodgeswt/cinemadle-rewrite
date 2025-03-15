import { ok, err } from "$lib/result";
import type { Result } from "$lib/result";

export async function getPossibleMovies(): Promise<Result<string[]>> {
    const data = await get("media/list/movie", null)

    if (data.ok) {
        const listStr = data.data!
        try {
            return ok(JSON.parse(listStr))
        } catch (e) {
            return err("Invalid list")
        }
    } else {
        return err(data.error!)
    }
}

export async function get(
  endpoint: string,
  queryParams: { [key: string]: string } | null,
): Promise<Result<string>> {
  let host = "http://192.168.0.23:8080";

  try {
    endpoint = endpoint.replace(/^\//, "");
    let uri = `${host}/api/v1/${endpoint}`;

    if (queryParams !== null) {
      let first = true;
      for (const queryKey in queryParams) {
        if (!queryParams.hasOwnProperty(queryKey)) {
          continue;
        }

        if (first) {
          uri += "?";
          first = false;
        } else {
          uri += "&";
        }

        uri += `${queryKey}=${queryParams[queryKey]}`;
      }
    }
    const response = await fetch(uri);
    let responseData: string;
    let good = true;
    if (response.ok) {
      responseData = JSON.stringify(await response.json());
    } else {
      responseData = `Error: ${response.status} ${response.statusText}`;
      good = false;
    }

    if (!good) {
      return err(responseData);
    }

    return ok(responseData);
  } catch {
    return err("Unknown error");
  }
}
