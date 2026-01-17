import { isArray, isDictionary, hasValue } from "$lib/util";

export type SignUpErrorDto = {
    errors: { [key: string]: string[] }
}

export type ImageDto = {
    imageData: string,
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

export type Hints = {
    min?: string;
    max?: string;
    knownValues?: string[];
    possibleValues?: string[];
};

export type Field = {
    color: string;
    direction: number;
    values: string[];
    modifiers: { [key: string]: string[] };
    hints?: Hints;
};

export type MediaDto = {
    title: string;
    id: number;
    cast: PersonDto[];
    genres: string[];
    year: number;
    rating: string;
    creatives: PersonDto[];
    boxOffice: number;
};

export type PersonDto = {
    name: string;
    role: string;
}

export type GameSummaryDto = {
    summary: string[];
}

export type PurchaseResponseDto = {
    sessionId: string;
}

export type PurchaseDetailsDto = {
    productId: string;
    quantity: number;
}

export type QuantitiesDto = {
    quantities: { [key: string]: number }
}

export type FeatureFlagsDto = {
    featureFlags: { [key: string]: boolean }
}

export type CustomGameCreateDto = {
    id: number;
}

export type CustomGameDto = {
    id: string;
    targetMovieId: number;
}

export function isCustomGameCreateDto(obj: any): obj is CustomGameCreateDto {
    if (!hasValue(obj)) {
        return false;
    }

    return typeof obj.id === 'number';
}

export function isCustomGameDto(obj: any): obj is CustomGameDto {
    if (!hasValue(obj)) {
        return false;
    }

    return typeof obj.id === 'string'
        && typeof obj.targetMovieId === 'number';
}

export function isFeatureFlagsDto(obj: any): obj is FeatureFlagsDto {
    if (!hasValue(obj)) {
        return false;
    }

    return isDictionary(obj.featureFlags, 'string', 'boolean', false);
}

export function isQuantitiesDto(obj: any): obj is QuantitiesDto {
    if (!hasValue(obj)) {
        return false;
    }

    return isDictionary(obj.quantities, 'string', 'number', false);
}

export function isPurchaseResponseDto(obj: any): obj is PurchaseResponseDto {
    if (!hasValue(obj)) {
        return false;
    }

    return typeof(obj.sessionId) === 'string';
}

export function isGameSummaryDto(obj: any): obj is GameSummaryDto {
    if (!hasValue(obj)) {
        return false;
    }

    return isArray(obj.summary, 'string');
}

export function isImageDto(obj: any): obj is ImageDto {
    if (!hasValue(obj)) {
        return false;
    }

    return typeof obj.imageData === 'string';
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
    const baseValid = (
        hasValue(obj) &&
        hasValue(obj.color) &&
        typeof obj.color === "string" &&
        hasValue(obj.direction) &&
        typeof obj.direction === "number" &&
        isArray(obj.values, "string") &&
        isDictionary(obj.modifiers, "string", "string", true)
    );

    if (!baseValid) {
        return false;
    }

    // hints is optional, but if present it should have the right structure
    if (obj.hints !== undefined && obj.hints !== null) {
        if (typeof obj.hints !== 'object') {
            return false;
        }
        // Allow null or string for min/max
        if (obj.hints.min !== undefined && obj.hints.min !== null && typeof obj.hints.min !== 'string') {
            return false;
        }
        if (obj.hints.max !== undefined && obj.hints.max !== null && typeof obj.hints.max !== 'string') {
            return false;
        }
        // Allow null or string array for knownValues/possibleValues
        if (obj.hints.knownValues !== undefined && obj.hints.knownValues !== null && !isArray(obj.hints.knownValues, 'string')) {
            return false;
        }
        if (obj.hints.possibleValues !== undefined && obj.hints.possibleValues !== null && !isArray(obj.hints.possibleValues, 'string')) {
            return false;
        }
    }

    return true;
}
