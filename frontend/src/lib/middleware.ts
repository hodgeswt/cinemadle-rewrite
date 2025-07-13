import { ok, err } from "$lib/result";
import type { Result } from "$lib/result";
import type { GuessDomain, PossibleMediaDomain } from "./domain";
import { MediaDtoToGuessDomain, PossibleMediaDtoToDomain } from "./mappers";
import { isoDateNoTime } from "./util";

export async function ping(): Promise<boolean> {
    const data = await get("heartbeat", null, "")

    return data.ok
}

export async function getAnswer(uid: string): Promise<Result<GuessDomain>> {
    const data = await get(`target`, { date: isoDateNoTime() }, uid)

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
    const data = await get("movielist", null, uid)

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
    const data = await get("guesses", { date: isoDateNoTime() }, uid)

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
    let host = "http://192.168.0.23:5566"

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
            let j = await response.json();
            try {
                responseData = JSON.stringify(j);
            } catch {
                responseData = "[]";
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
    uid: string,
    headers?: { [key: string]: string } | null,
): Promise<Result<string>> {
    let host = "http://192.168.0.23:5566";

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
