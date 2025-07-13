import {
    post,
} from "$lib/middleware";

import { ok, err } from "$lib/result";
import type { Result } from "$lib/result";
import { isLoginDto, isSignUpErrorDto, type LoginDto, type SignUpErrorDto } from "./dto";

export async function logIn(email: string, password: string): Promise<Result<LoginDto>> {
    const data = await post(
        "login", true,
        JSON.stringify({
            "email": email,
            "password": password
        }),
        {
            "Content-Type": "application/json"
        }
    )

    if (!data.ok) {
        return err(data.error!)
    }

    const raw = JSON.parse(data.data!)
    console.log(raw);
    if (!isLoginDto(raw)) {
        return err("invalid data received")
    }

    return ok(raw as LoginDto)
}

export async function signUp(email: string, password: string): Promise<Result<SignUpErrorDto | null>> {
    const data = await post(
        "register",
        true,
        JSON.stringify({
            "email": email,
            "password": password
        }),
        {
            "Content-Type": "application/json"
        }
    )

    if (!data.ok) {
        console.error(data.error!);
        let errData = JSON.parse(data.error!);
        if (isSignUpErrorDto(errData)) {
            return ok(errData as SignUpErrorDto);
        }

        return err("unknown error")
    }

    return ok(null)
}
