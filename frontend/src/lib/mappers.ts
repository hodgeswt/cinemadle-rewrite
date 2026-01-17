import { isGuessDto, isPossibleMediaDto, isMediaDto, isCustomGameCreateDto, isCustomGameDto } from "$lib/dto";
import type { CardDomain, GuessDomain, PossibleMediaDomain, CustomGameCreateDomain, CustomGameDomain } from "$lib/domain";
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

    // Define the desired card order (year and rating first)
    const cardOrder = ['year', 'rating', 'genre', 'boxOffice', 'cast', 'creatives'];

    // Sort the field keys according to the desired order
    const sortedKeys = Object.keys(guess.fields).sort((a, b) => {
        const indexA = cardOrder.indexOf(a);
        const indexB = cardOrder.indexOf(b);
        // If not in the order list, put at the end
        const orderA = indexA === -1 ? cardOrder.length : indexA;
        const orderB = indexB === -1 ? cardOrder.length : indexB;
        return orderA - orderB;
    });

    for (const k of sortedKeys) {
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

    const creatives = {
        title: "creatives",
        data: media.creatives.map((x) => `${x.role}: ${x.name}`),
        color: color,
        modifiers: {},
    } as CardDomain;

    const boxOffice = {
        title: "box office",
        data: [`${media.boxOffice}`] as string[],
        color: color,
        modifiers: {},
    } as CardDomain;


    o.cards = [boxOffice, creatives, cast, genre, year, rating] as CardDomain[];

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

export function CustomGameCreateDtoToDomain(dto: any): Result<CustomGameCreateDomain> {
    if (!isCustomGameCreateDto(dto)) {
        return err("Invalid data type");
    }

    return ok({ id: dto.id });
}

export function CustomGameCreateDomainToDto(domain: CustomGameCreateDomain): Result<any> {
    return ok({ id: domain.id });
}

export function CustomGameDtoToDomain(dto: any): Result<CustomGameDomain> {
    if (!isCustomGameDto(dto)) {
        return err("Invalid data type");
    }

    return ok({ id: dto.id, targetMovieId: dto.targetMovieId });
}

