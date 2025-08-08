import { ok, err } from "$lib/result";
import type { Result } from "$lib/result";
import type { GuessDomain, PossibleMediaDomain } from "./domain";
import { isLoginDto } from "./dto";
import Logger from "./logger";
import { MediaDtoToGuessDomain, PossibleMediaDtoToDomain } from "./mappers";
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
            good = false;
        }

        if (!good) {
            return err(responseData);
        }

        return ok(responseData);
    } catch (e) {
        console.error(e);
        return err("unknown error");
    }

}

export async function get(
    endpoint: string,
    queryParams: { [key: string]: string } | null,
    headers?: { [key: string]: any } | null,
    raw?: boolean,
): Promise<Result<string>> {
    let host = import.meta.env.VITE_API_ENDPOINT;

    try {
        endpoint = endpoint.replace(/^\//, "");
        let uri = `${host}/api/cinemadle/${endpoint}`;

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
