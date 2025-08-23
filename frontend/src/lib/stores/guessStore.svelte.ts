import { writable } from 'svelte/store';
import type { GuessDomain, PossibleMediaDomain } from '$lib/domain';

export type GuessStore = {
    guesses: GuessDomain[],
    possibleGuesses: PossibleMediaDomain | undefined,
}

export const guessStore = writable<GuessStore>({
    guesses: [],
    possibleGuesses: undefined
} as GuessStore);
