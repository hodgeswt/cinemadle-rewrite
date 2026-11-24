import type { GuessDomain, PossibleMediaDomain } from "$lib/domain.ts";
import type { GameSummaryDto, ImageDto } from "$lib/datamodel/dto.ts";
import type { Result } from "$lib/result.ts";

export interface IGuessService {
    guess(guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>>;
    guessCustomGame(customGameId: string, guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>>;
    isInitialized(): boolean;
    initialize(): Promise<boolean>;
    possibleGuesses(): PossibleMediaDomain;
    possibleGuessTitles(): string[];
    getPreviousGuesses(customGameId?: string): Promise<Result<GuessDomain[]>>;
    getVisualClue(customGameId?: string): Promise<Result<ImageDto>>;
    getGameSummary(customGameId?: string): Promise<Result<GameSummaryDto>>;
}
