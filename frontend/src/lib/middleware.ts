import { ok, err } from "$lib/result";
import type { Result } from "$lib/result";
import type { GuessDomain, PossibleMediaDomain, CustomGameCreateDomain, CustomGameDomain } from "./domain";
import { isLoginDto, isGameSummaryDto, isImageDto, type GameSummaryDto, type ImageDto } from "./dto";
import Logger from "./logger";
import { MediaDtoToGuessDomain, PossibleMediaDtoToDomain, CustomGameCreateDomainToDto, CustomGameDtoToDomain } from "./mappers";
import { isoDateNoTime } from "./util";

export const PING_LIMIT = 10;

export async function ping(): Promise<boolean> {
    const data = await get("heartbeat", null)

    return data.ok
}

export async function healthcheck(): Promise<boolean> {
    let alive = await ping();
    let healthPing = 0;
    while (!alive) {
        if (healthPing === PING_LIMIT) {
            return false;
        }
        alive = await ping();
        healthPing += 1;
        await new Promise((x) => setTimeout(x, 1000));
    }

    return true;
}

export async function getAnswer(): Promise<Result<GuessDomain>> {
    const data = await get(`target`, { date: isoDateNoTime() })

    if (data.ok) {
        const raw = data.data!
        try {
            let dto = JSON.parse(raw)
            console.log(dto);
            let domain = MediaDtoToGuessDomain(dto, true)
            if (domain.ok) {
                return ok(domain.data!)
            } else {
                console.log('failed parse')
                return err("Invalid data")
            }
        } catch (_e) {
            return err("Invalid data")
        }
    } else {
        return err(data.error!)
    }
}

export async function getPossibleMovies(): Promise<Result<PossibleMediaDomain>> {
    const data = await get("movielist", null)

    if (data.ok) {
        const raw = data.data!
        try {
            let dto = JSON.parse(raw)
            let domain = PossibleMediaDtoToDomain(dto)

            if (domain.ok) {
                return ok(domain.data!)
            } else {
                return err("Invalid data")
            }
        } catch (e) {
            return err("Invalid data")
        }
    } else {
        return err(data.error!)
    }
}

export async function validateAndRefreshToken(userToken: string, refreshToken: string): Promise<Result<string>> {
    const data = await get("validate", null, { "Authorization": userToken });

    if (data.ok && data.data! === "true") {
        return ok("");
    }

    const refresh = await post("refresh", true, { refreshToken: refreshToken });

    if (refresh.ok) {
        const j = JSON.parse(refresh.data!)
        if (isLoginDto(j)) {
            return ok(refresh.data!);
        }
    }

    return err("unable to login or refresh");
}

export async function loadPreviousGuesses(userToken: string): Promise<Result<number[]>> {
    const data = await get("guesses", { date: isoDateNoTime() }, { "Authorization": userToken })

    if (!data.ok) {
        return err(data.error!)
    }

    const raw = data.data!
    return ok(JSON.parse(raw))
}

export async function loadCustomGamePreviousGuesses(customGameId: string, userToken: string): Promise<Result<number[]>> {
    const data = await get(`custom/${customGameId}/guesses`, null, { "Authorization": userToken });

    if (!data.ok) {
        return err(data.error!);
    }

    try {
        const parsed = JSON.parse(data.data!);
        if (!Array.isArray(parsed)) {
            return err("Invalid custom game guesses data");
        }

        return ok(parsed as number[]);
    } catch (e) {
        Logger.log("loadCustomGamePreviousGuesses: failed to parse response {0}", JSON.stringify(e));
        return err("Invalid custom game guesses data");
    }
}

export async function createCustomGame(movieId: number, userToken: string): Promise<Result<CustomGameDomain>> {
    Logger.log("middleware.createCustomGame: movieId {0}", movieId);

    const customGameCreate: CustomGameCreateDomain = { id: movieId };
    const dtoResult = CustomGameCreateDomainToDto(customGameCreate);

    if (!dtoResult.ok) {
        Logger.log("middleware.createCustomGame: failed to convert domain to DTO");
        return err("Invalid custom game data");
    }

    const result = await post(
        "custom/create",
        false,
        JSON.stringify(dtoResult.data),
        {
            "Authorization": userToken,
            "Content-Type": "application/json"
        }
    );

    if (!result.ok) {
        Logger.log("middleware.createCustomGame: got bad response from server: {0}", result.error);
        return err(result.error!);
    }

    try {
        const parsed = JSON.parse(result.data!);
        const domainResult = CustomGameDtoToDomain(parsed);

        if (!domainResult.ok) {
            Logger.log("middleware.createCustomGame: failed to map dto to domain");
            return err("Invalid custom game data");
        }

        const domain = domainResult.data!;
        Logger.log("middleware.createCustomGame: successfully created custom game {0}", domain.id);
        return ok(domain);
    } catch (e) {
        Logger.log("middleware.createCustomGame: failed to parse response {0}", JSON.stringify(e));
        return err("Invalid custom game data");
    }
}

export async function getCustomGame(customGameId: string, userToken: string): Promise<Result<CustomGameDomain>> {
    Logger.log("middleware.getCustomGame: customGameId {0}", customGameId);

    const result = await get(`custom/${customGameId}`, null, { "Authorization": userToken });

    if (!result.ok) {
        Logger.log("middleware.getCustomGame: request failed {0}", result.error);
        return err(result.error!);
    }

    try {
        const parsed = JSON.parse(result.data!);
        const domainResult = CustomGameDtoToDomain(parsed);

        if (!domainResult.ok) {
            Logger.log("middleware.getCustomGame: invalid payload");
            return err("Invalid custom game data");
        }

        return ok(domainResult.data!);
    } catch (e) {
        Logger.log("middleware.getCustomGame: failed to parse response {0}", JSON.stringify(e));
        return err("Invalid custom game data");
    }
}

export async function getCustomGameSummary(customGameId: string, userToken: string): Promise<Result<GameSummaryDto>> {
    Logger.log("middleware.getCustomGameSummary: customGameId {0}", customGameId);

    const result = await get(`custom/${customGameId}/gameSummary`, null, { "Authorization": userToken });

    if (!result.ok) {
        Logger.log("middleware.getCustomGameSummary: request failed {0}", result.error);
        return err(result.error!);
    }

    try {
        const parsed = JSON.parse(result.data!);
        if (!isGameSummaryDto(parsed)) {
            Logger.log("middleware.getCustomGameSummary: invalid payload {0}", parsed);
            return err("Invalid custom game summary data");
        }

        return ok(parsed as GameSummaryDto);
    } catch (e) {
        Logger.log("middleware.getCustomGameSummary: failed to parse response {0}", JSON.stringify(e));
        return err("Invalid custom game summary data");
    }
}

export async function getCustomGameAnswer(customGameId: string, userToken: string): Promise<Result<GuessDomain>> {
    Logger.log("middleware.getCustomGameAnswer: customGameId {0}", customGameId);

    const result = await get(`custom/${customGameId}/target`, null, { "Authorization": userToken });

    if (!result.ok) {
        Logger.log("middleware.getCustomGameAnswer: request failed {0}", result.error);
        return err(result.error!);
    }

    try {
        const parsed = JSON.parse(result.data!);
        const domain = MediaDtoToGuessDomain(parsed, true);

        if (!domain.ok) {
            Logger.log("middleware.getCustomGameAnswer: failed to convert media dto to domain");
            return err("Invalid custom game answer data");
        }

        return ok(domain.data!);
    } catch (e) {
        Logger.log("middleware.getCustomGameAnswer: failed to parse response {0}", JSON.stringify(e));
        return err("Invalid custom game answer data");
    }
}

export async function getCustomGameVisualClue(customGameId: string, userToken: string): Promise<Result<ImageDto>> {
    Logger.log("middleware.getCustomGameVisualClue: customGameId {0}", customGameId);

    const result = await get(`custom/${customGameId}/target/image`, null, { "Authorization": userToken });

    if (!result.ok) {
        Logger.log("middleware.getCustomGameVisualClue: request failed {0}", result.error);
        return err(result.error!);
    }

    try {
        const parsed = JSON.parse(result.data!);
        if (!isImageDto(parsed)) {
            Logger.log("middleware.getCustomGameVisualClue: invalid payload {0}", parsed);
            return err("Invalid custom game visual clue data");
        }

        return ok(parsed as ImageDto);
    } catch (e) {
        Logger.log("middleware.getCustomGameVisualClue: failed to parse response {0}", JSON.stringify(e));
        return err("Invalid custom game visual clue data");
    }
}

export async function post(
    endpoint: string,
    useNoBase: boolean = false,
    body?: any | null,
    headers?: { [key: string]: string } | null,
    queryParams?: { [key: string]: string } | null,
): Promise<Result<string>> {
    let host = import.meta.env.VITE_API_ENDPOINT

    try {
        endpoint = endpoint.replace(/^\//, "");
        let uri = useNoBase ? `${host}/${endpoint}` : `${host}/api/cinemadle/${endpoint}`;

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

        Logger.log("post: making post request to {0}", uri);

        const response = await fetch(uri, {
            method: 'POST',
            headers: { ...headers },
            body: body,
        });

        let responseData: string;
        let good = true;
        if (response.ok) {
            try {
                let j = await response.json();
                responseData = JSON.stringify(j);
            } catch {
                responseData = "true"
            }
        } else {
            responseData = JSON.stringify(await response.json());
            Logger.log("post: error response {0}", responseData);
            good = false;
        }

        if (!good) {
            return err(responseData);
        }

        return ok(responseData);
    } catch (e) {
        Logger.log("Caught error: {0}", JSON.stringify(e));
        return err("unknown error");
    }

}

export async function get(
    endpoint: string,
    queryParams: { [key: string]: string } | null,
    headers?: { [key: string]: any } | null,
    raw?: boolean,
    useNoBase?: boolean,
): Promise<Result<string>> {
    let host = import.meta.env.VITE_API_ENDPOINT;

    try {
        endpoint = endpoint.replace(/^\//, "");
        let uri = useNoBase ? `${host}/${endpoint}` : `${host}/api/cinemadle/${endpoint}`;
        Logger.log("middleware.get: uri {0}", uri);

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
        const response = await fetch(uri, {
            headers: {
                ...headers,
            },
        });
        let responseData: string;
        let good = true;
        if (response.ok) {
            try {
                if (raw !== true) {
                    Logger.log("middleware.ts: get(): loading json")
                    if (response.status != 204) {
                        responseData = JSON.stringify(await response.json());
                    } else {
                        responseData = "true";
                    }
                } else {
                    Logger.log("middleware.ts: get(): loading raw")
                    responseData = await response.text();
                }
            }
            catch {
                responseData = "false";
            }
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
