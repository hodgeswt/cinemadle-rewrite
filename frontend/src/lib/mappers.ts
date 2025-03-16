import { isGuessDto, isPossibleMediaDto } from "$lib/dto";
import type { CardDomain, GuessDomain, PossibleMediaDomain } from "$lib/domain";
import { isPossibleMediaDomain } from "$lib/domain";
import { err, ok, type Result } from "$lib/result";

export function GuessDtoToDomain(guess: any, title: string): Result<GuessDomain> {
    if (!isGuessDto(guess)) {
        return err("Invalid data type");
    }

    let out = {
        title: title,
        cards: [],
    } as GuessDomain;

    for (const k in guess.fields) {
        const v = guess.fields[k];

        let card = {
            color: v.color === "grey" ? "gray" : v.color,
            data: v.values,
            title: k,
        } as CardDomain;

        out.cards.push(card);
    }

    return ok(out);
}

export function PossibleMediaDtoToDomain(possibleMedia: any): Result<PossibleMediaDomain> {
    if (!isPossibleMediaDto(possibleMedia)) {
        return err("Invalid data type");
    }

    if (!isPossibleMediaDomain(possibleMedia)) {
        return err("Invalid data type");
    }

    const keys = Object.keys(possibleMedia);
    const values = Object.values(possibleMedia);

    let o = {} as PossibleMediaDomain

    for (let i = 0; i < values.length; i++) {
        o[values[i]] = keys[i]
    }

    return ok(o)
}
