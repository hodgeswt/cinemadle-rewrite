import { ok, err } from "$lib/result";
import type { Result } from "$lib/result";
import type { GuessDomain, PossibleMediaDomain } from "./domain";
import { MediaDtoToGuessDomain, PossibleMediaDtoToDomain } from "./mappers";
import { isoDateNoTime } from "./util";

export async function ping(): Promise<boolean> {
    const data = await get("healthcheck", null, "")

    return data.ok
}

export async function getAnswer(uid: string): Promise<Result<GuessDomain>> {
    const data = await get(`guess/movie/${isoDateNoTime()}/answer`, null, uid)

    if (data.ok) {
        const raw = data.data!
        try {
            let dto = JSON.parse(raw)
            let domain = MediaDtoToGuessDomain(dto, true)
            if (domain.ok) {
                return ok(domain.data!)
            } else {
                return err("Invalid data")
            }
        } catch (_e) {
            return err("Invalid data")
        }
    } else {
        return err(data.error!)
    }
}

export async function getPossibleMovies(uid: string): Promise<Result<PossibleMediaDomain>> {
    const data = await get("media/list/movie", null, uid)

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

export async function loadPreviousGuesses(uid: string): Promise<Result<string[]>> {
    const data = await get("users/guesses", null, uid)

    if (!data.ok) {
        return err(data.error!)
    }

    const raw = data.data!
    return ok(JSON.parse(raw))
}

export async function get(
    endpoint: string,
    queryParams: { [key: string]: string } | null,
    uid: string,
    headers?: { [key: string]: string } | null,
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
        const response = await fetch(uri, {
            headers: {
                "x-uuid": uid,
                ...headers,
            },
        });
        let responseData: string;
        let good = true;
        if (response.ok) {
            if (response.status != 204) {
                responseData = JSON.stringify(await response.json());
            } else {
                responseData = "[]";
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
