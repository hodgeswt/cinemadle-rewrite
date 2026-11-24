import {
    post,
} from "$lib/middleware.ts";

import { ok, err, type Result } from "$lib/result.ts";
import { Validate } from "./business/typing/validations.ts";
import { LoginDto, SignUpErrorDto } from "./datamodel/dto.ts";

export async function logIn(email: string, password: string): Promise<Result<LoginDto>> {
    const data = await post(
        "login", false,
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
    if (!Validate(LoginDto, raw)) {
        return err("invalid data received")
    }

    return ok(raw as LoginDto)
}

export async function signUp(email: string, password: string): Promise<Result<SignUpErrorDto | null>> {
    const data = await post(
        "register",
        false,
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
        const errData = JSON.parse(data.error!);
        if (Validate(SignUpErrorDto, errData)) {
            return ok(errData as SignUpErrorDto);
        }

        return err("unknown error")
    }

    return ok(null)
}
