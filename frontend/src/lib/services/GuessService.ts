import type { GuessDomain } from "$lib/domain";
import type { IGuessService } from "./IGuessService";

export class GuessService implements IGuessService {
    constructor() { }

    guess(id: string): GuessDomain | null {
        return null;
    }
}
