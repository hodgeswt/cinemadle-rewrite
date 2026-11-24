import type { GuessDomain, PossibleMediaDomain } from "$lib/domain.ts";
import type { GameSummaryDto, ImageDto } from "$lib/datamodel/dto.ts";
import Logger from "$lib/logger.ts";
import { type Result } from "$lib/result.ts";
import type { IGuessService } from "./IGuessService.ts";
import { Api } from "$lib/api/api.ts";

export abstract class GuessServiceShared implements IGuessService {
    static guessError = "error making that guess! try another option.";
    static duplicateGuessError = "guess already made! try another option.";
    static unableToGetPreviousError = "unable to get previous guesses. try again later.";
    static unableToLoadImageError = "unable to load visual clue.";
    static unableToLoadGameSummaryError = "unable to load game summary.";

    private _initialized;
    protected _guesses: string[] = [];

    protected _possibleGuessTitles: string[] = [];
    protected _possibleGuesses: PossibleMediaDomain = { self: new Map<string, number>() };

    constructor() {
        this._initialized = false;
    }

    possibleGuesses(): PossibleMediaDomain {
        return this._possibleGuesses;
    }

    possibleGuessTitles(): string[] {
        return this._possibleGuessTitles;
    }

    abstract guess(guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>>;
    abstract guessCustomGame(customGameId: string, guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>>;
    abstract getVisualClue(customGameId?: string): Promise<Result<ImageDto>>;
    abstract getGameSummary(customGameId?: string): Promise<Result<GameSummaryDto>>;

    isInitialized(): boolean {
        return this._initialized;
    }

    async initialize(): Promise<boolean> {
        if (this._initialized) {
            return true;
        }
        
        Logger.log("GuessServiceShared.initialize()");
        const result = await Api.getPossibleMovies();

        if (result.ok) {
            Logger.log("GuessServiceShared.initialize(): initialized");

            this._possibleGuesses = result.data!;
            this._possibleGuessTitles = Object.keys(result.data!);

            this._initialized = true;
        }

        return this._initialized;
    }

    protected getTitle(guess: string): string {
        Logger.log("GuessServiceShared.getTitle({0})", guess);
        return Object.keys(this._possibleGuesses).find(
            (x) => this._possibleGuesses.self.get(x)?.toString() === guess,
        ) ?? "unknown";
    }

    abstract getPreviousGuesses(customGameId?: string): Promise<Result<GuessDomain[]>>;
}