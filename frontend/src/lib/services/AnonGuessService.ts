import type { GuessDomain } from "$lib/domain";
import { err, type Result } from "$lib/result";
import { GuessServiceShared } from "./GuessServiceShared";
import type { IGuessService } from "./IGuessService";

export class AnonGuessService extends GuessServiceShared implements IGuessService {

    constructor() {
        super();
    }

    async guess(guess: string): Promise<Result<GuessDomain>> {
        return err("");
    }
}
