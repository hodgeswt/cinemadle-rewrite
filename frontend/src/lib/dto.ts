import { isArray, isDictionary, hasValue } from "$lib/util";

export type SignUpErrorDto = {
    errors: { [key: string]: string[] }
}

export type LoginDto = {
    tokenType: string,
    accessToken: string,
    refreshToken: string,
    expiresIn: number,
}

export type PossibleMediaDto = {
    [key: string]: number;
}

export type GuessDto = {
    fields: { [key: string]: Field };
};

export type Field = {
    color: string;
    direction: number;
    values: string[];
    modifiers: { [key: string]: string[] };
};

export type MediaDto = {
    title: string;
    id: number;
    cast: PersonDto[];
    genres: string[];
    year: number;
    rating: string;
};

export type PersonDto = {
    name: string;
    role: string;
}

export function isSignUpErrorDto(obj: any): obj is SignUpErrorDto {
    if (!hasValue(obj)) {
        return false;
    }

    return isDictionary(obj.errors, 'string', 'string', true)
}

export function isLoginDto(obj: any): obj is LoginDto {
    if (!hasValue(obj)) {
        return false;
    }

    return typeof obj.tokenType === 'string'
        && typeof obj.accessToken === 'string'
        && typeof obj.refreshToken === 'string'
        && typeof obj.expiresIn === 'number'
}

export function isMediaDto(obj: any): obj is MediaDto {
    if (!hasValue(obj)) {
        return false;
    }

    const basic =
        typeof obj.title === 'string'
        && typeof obj.id === 'number'
        && isArray(obj.genres, 'string')
        && typeof obj.year === 'string'
        && typeof obj.rating === 'string';

    if (!basic) {
        return false;
    }

    return Array.isArray(obj.cast)
        && obj.cast.every((x: any) => isPersonDto(x));
}

export function isPersonDto(obj: any): obj is PersonDto {
    if (!hasValue(obj)) {
        return false;
    }

    if (typeof obj.name !== 'string' || typeof obj.role !== 'string') {
        return false;
    }

    return true;
}

export function isPossibleMediaDto(obj: any): obj is PossibleMediaDto {
    if (!hasValue(obj)) {
        return false;
    }

    for (const key in obj) {
        if (typeof key !== 'string' || typeof obj[key] !== 'number') {
            return false
        }
    }
    return true
}

export function isGuessDto(obj: any): obj is GuessDto {
    if (!hasValue(obj) || !hasValue(obj.fields)) {
        return false;
    }

    let valid = true;

    for (const key in obj.fields) {
        if (typeof key !== "string") {
            valid = false;
            break;
        }

        if (!isField(obj.fields[key])) {
            valid = false;
            break;
        }
    }

    return valid;
}

export function isField(obj: any): obj is Field {
    return (
        hasValue(obj) &&
        hasValue(obj.color) &&
        typeof obj.color === "string" &&
        hasValue(obj.direction) &&
        typeof obj.direction === "number" &&
        isArray(obj.values, "string") &&
        isDictionary(obj.modifiers, "string", "string", true)
    );
}
