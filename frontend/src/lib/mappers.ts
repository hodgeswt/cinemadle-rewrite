import { isGuessDto, isPossibleMediaDto, isMediaDto } from "$lib/dto";
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
        win: Object.values(guess.fields).every((x) => x.color === "green"),
    } as GuessDomain;

    for (const k in guess.fields) {
        const v = guess.fields[k];

        let card = {
            color: v.color === "grey" ? "gray" : v.color,
            data: v.values,
            direction: v.direction,
            title: k,
            modifiers: v.modifiers,
        } as CardDomain;

        out.cards.push(card);
    }

    return ok(out);
}

export function MediaDtoToGuessDomain(media: any, win: boolean): Result<GuessDomain> {
    if (!isMediaDto(media)) {
        console.log('failed validate')
        return err("Invalid data type");
    }

    let o = {} as GuessDomain;

    o.win = win;
    o.title = media.title;

    const color = win ? "green" : "gray";

    const cast = {
        title: "cast",
        data: media.cast.map((x) => x.name),
        color: color,
        modifiers: {},
    } as CardDomain;

    const genre = {
        title: "genre",
        data: media.genres,
        color: color,
        modifiers: {},
    } as CardDomain;

    const year = {
        title: "year",
        data: [`${media.year}`] as string[],
        color: color,
        modifiers: {},
    } as CardDomain;

    const rating = {
        title: "rating",
        data: [media.rating] as string[],
        color: color,
        modifiers: {},
    } as CardDomain;

    o.cards = [cast, genre, year, rating] as CardDomain[];

    return ok(o);
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

    for (let i = 0; i < keys.length; i++) {
        o[keys[i]] = values[i]
    }

    return ok(o)
}
