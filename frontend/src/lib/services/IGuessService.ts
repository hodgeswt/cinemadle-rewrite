import type { GuessDomain, PossibleMediaDomain } from "$lib/domain";
import type { ImageDto } from "$lib/dto";
import type { Result } from "$lib/result";

export interface IGuessService {
    guess(guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>>;
    isInitialized(): boolean;
    initialize(): Promise<boolean>;
    possibleGuesses(): PossibleMediaDomain;
    getPreviousGuesses(): Promise<Result<GuessDomain[]>>;
    getVisualClue(): Promise<Result<ImageDto>>;
}
