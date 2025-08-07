import type { GuessDomain, PossibleMediaDomain } from "$lib/domain";
import Logger from "$lib/logger";
import { getPossibleMovies, loadPreviousGuesses } from "$lib/middleware";
import { type Result } from "$lib/result";
import type { IGuessService } from "./IGuessService";

export abstract class GuessServiceShared implements IGuessService {
    static guessError = "Error making that guess! Try another option.";
    static duplicateGuessError = "Guess already made! Try another option.";
    static unableToGetPreviousError = "Unable to get previous guesses. Try again later.";

    private _initialized;
    protected _possibleGuesses: PossibleMediaDomain;
    protected _guesses: string[];

    constructor() {
        this._possibleGuesses = {} as PossibleMediaDomain;
        this._initialized = false;
        this._guesses = [] as string[];
    }

    possibleGuesses(): PossibleMediaDomain {
        Logger.log("GuessServiceShared.possibleGuesses()");
        return this._possibleGuesses;
    }

    abstract guess(guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>>;

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
            this._possibleGuesses = result.data!;
            this._initialized = true;
        }

        return this._initialized;
    }

    protected getTitle(guess: string): string {
        return Object.keys(this._possibleGuesses).find(
            (x) => this._possibleGuesses[x].toString() === guess,
        ) ?? "Unknown";
    }

    abstract getPreviousGuesses(): Promise<Result<GuessDomain[]>>;
}