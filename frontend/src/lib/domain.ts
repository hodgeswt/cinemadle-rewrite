import { DataType } from "$lib/business/typing/datatype.ts";

export class PossibleMediaDomain {
    @DataType('number', { isMap: true })
    self!: Map<string, number>;
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
    modifiers: Map<string, string[]>
};

export enum FeatureFlags {
}

export type CustomGameCreateDomain = {
    id: number;
}

export type CustomGameDomain = {
    id: string;
    targetMovieId: number;
}
