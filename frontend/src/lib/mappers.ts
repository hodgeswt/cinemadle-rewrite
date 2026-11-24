import { MediaDto, CustomGameCreateDto, GuessDto, CustomGameDto } from "$lib/datamodel/dto.ts";
import { type CardDomain, type GuessDomain, type CustomGameCreateDomain, type CustomGameDomain } from "$lib/domain.ts";
import { err, ok, type Result } from "$lib/result.ts";
import { Validate } from "./business/typing/validations.ts";

export function GuessDtoToDomain(guess: unknown): Result<GuessDomain> {
    if (!Validate(GuessDto, guess)) {
        return err("Invalid data type");
    }
    const out = {
        title: guess.title,
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
        const v = guess.fields.get(k)!;

        const card = {
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

export function MediaDtoToGuessDomain(media: unknown, win: boolean): Result<GuessDomain> {
    if (!Validate(MediaDto, media)) {
        return err("Invalid data type");
    }

    const o = {
        win: win,
        title: media.title
    } as GuessDomain;

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


    o.cards = [year, rating, genre, boxOffice, cast, creatives] as CardDomain[];

    return ok(o);
}

export function CustomGameCreateDtoToDomain(dto: unknown): Result<CustomGameCreateDomain> {
    if (!Validate(CustomGameCreateDto, dto)) {
        return err("Invalid data type");
    }

    return ok({ id: dto.id });
}

export function CustomGameCreateDomainToDto(domain: CustomGameCreateDomain): Result<CustomGameCreateDto> {
    return ok({ id: domain.id });
}

export function CustomGameDtoToDomain(dto: unknown): Result<CustomGameDomain> {
    if (!Validate(CustomGameDto, dto)) {
        return err("Invalid data type");
    }

    return ok({ id: dto.id, targetMovieId: dto.targetMovieId });
}

