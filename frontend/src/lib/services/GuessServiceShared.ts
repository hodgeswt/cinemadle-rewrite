import type { GuessDomain, PossibleMediaDomain } from "$lib/domain";
import type { GameSummaryDto, ImageDto } from "$lib/dto";
import Logger from "$lib/logger";
import { getPossibleMovies, loadPreviousGuesses } from "$lib/middleware";
import { type Result } from "$lib/result";
import { guessStore } from "$lib/stores";
import type { IGuessService } from "./IGuessService";

export abstract class GuessServiceShared implements IGuessService {
    static guessError = "error making that guess! try another option.";
    static duplicateGuessError = "guess already made! try another option.";
    static unableToGetPreviousError = "unable to get previous guesses. try again later.";
    static unableToLoadImageError = "unable to load visual clue.";
    static unableToLoadGameSummaryError = "unable to load game summary.";

    private _initialized;
    protected _possibleGuesses: PossibleMediaDomain = {};
    protected _guesses: string[] = [];

    constructor() {
        guessStore.subscribe(x => this._possibleGuesses = x.possibleGuesses ?? {});
        guessStore.subscribe(x => this._guesses = x.guesses.map(x => x.title));
        this._initialized = false;
    }

    possibleGuesses(): PossibleMediaDomain {
        Logger.log("GuessServiceShared.possibleGuesses()");
        return this._possibleGuesses;
    }

    abstract guess(guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>>;
    abstract getVisualClue(): Promise<Result<ImageDto>>;
    abstract getGameSummary(): Promise<Result<GameSummaryDto>>;

    isInitialized(): boolean {
        return this._initialized;
    }

    async initialize(): Promise<boolean> {
        if (this._initialized) {
            return true;
        }
        
        Logger.log("GuessServiceShared.initialize()");
        const result = await getPossibleMovies();

        if (result.ok) {
            Logger.log("GuessServiceShared.initialize(): initialized");
            Logger.log("GuessServiceShared.initialize(): possible guesses: {0}", result.data!);
            guessStore.update(s => ({ ...s, possibleGuesses: result.data! }));
            this._initialized = true;
        }

        return this._initialized;
    }

    protected getTitle(guess: string): string {
        Logger.log("GuessServiceShared.getTitle({0})", guess);
        return Object.keys(this._possibleGuesses).find(
            (x) => this._possibleGuesses[x].toString() === guess,
        ) ?? "unknown";
    }

    abstract getPreviousGuesses(): Promise<Result<GuessDomain[]>>;
}