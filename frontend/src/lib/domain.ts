import { hasValue } from "./util";

export type PossibleMediaDomain = {
    [key: string]: string;
}

export type GuessDomain = {
    title: string;
    cards: CardDomain[];
    win: boolean;
};

export type CardDomain = {
    color: string;
    title: string;
    data: string[];
    direction: number;
};

export function isPossibleMediaDomain(obj: any): obj is PossibleMediaDomain {
    if (!hasValue(obj)) {
        return false;
    }

    for (const key in obj) {
        if (typeof key !== 'string' || typeof obj[key] !== 'string') {
            return false
        }
    }

    return true
}
