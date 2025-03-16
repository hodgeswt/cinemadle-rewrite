import { ok, err } from "$lib/result";
import type { Result } from "$lib/result";
import type { PossibleMediaDomain } from "./domain";
import { PossibleMediaDtoToDomain } from "./mappers";

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

export async function get(
    endpoint: string,
    queryParams: { [key: string]: string } | null,
    uid: string,
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
            },
        });
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
