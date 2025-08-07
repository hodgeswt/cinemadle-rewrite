import type { GuessDomain } from "$lib/domain";

export interface IGuessService {
    guess(id: string): GuessDomain | null;
}
