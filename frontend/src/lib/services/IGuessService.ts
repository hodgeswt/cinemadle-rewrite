import type { GuessDomain, PossibleMediaDomain } from "$lib/domain";
import type { GameSummaryDto, ImageDto } from "$lib/dto";
import type { Result } from "$lib/result";

export interface IGuessService {
    guess(guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>>;
    guessCustomGame(customGameId: string, guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>>;
    isInitialized(): boolean;
    initialize(): Promise<boolean>;
    possibleGuesses(): PossibleMediaDomain;
    getPreviousGuesses(customGameId?: string): Promise<Result<GuessDomain[]>>;
    getVisualClue(customGameId?: string): Promise<Result<ImageDto>>;
    getGameSummary(customGameId?: string): Promise<Result<GameSummaryDto>>;
}
