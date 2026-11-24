import { RequestBuilder, StatusOnly } from "./request.ts";
import { PossibleMediaDomain, type GuessDomain } from '$lib/domain.ts';
import { type Result, ok } from "$lib/result.ts";
import { MediaDto, PossibleMediaDto } from '$lib/datamodel/dto.ts';
import { MediaDtoToGuessDomain } from "$lib/mappers.ts";
import { activate } from "../business/typing/activator.ts";

export class Api {
  static async ping(): Promise<boolean> {
    const req = new RequestBuilder(StatusOnly).To("healthz");

    let pingCount = 0;
    const PING_LIMIT = 3;

    do {
      const res = await req.ExecuteStatusOnly();
      if (res.ok && res.data! === 200) {
        return true
      }

      pingCount++;
      await new Promise((x) => setTimeout(x, 300));
    } while (pingCount < PING_LIMIT);

    return false;
  }

  static getAnswer(date: string): Promise<Result<GuessDomain>> {
    return new RequestBuilder<typeof MediaDto, GuessDomain>(MediaDto)
      .To("api/cinemadle/target")
      .WithQueryParam("date", date)
      .WithMapper(x => MediaDtoToGuessDomain(x, true))
      .Execute();
  }

  static getPossibleMovies(): Promise<Result<PossibleMediaDomain>> {
    return new RequestBuilder<typeof PossibleMediaDto, PossibleMediaDomain>(PossibleMediaDto)
      .To("api/cinemadle/movielist")
      .WithMapper(x => ok(activate(PossibleMediaDomain, x).data!))
      .Execute();
  }
}