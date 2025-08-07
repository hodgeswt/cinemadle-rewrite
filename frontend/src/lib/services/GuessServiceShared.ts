import type { GuessDomain, PossibleMediaDomain } from "$lib/domain";
import Logger from "$lib/logger";
import { getPossibleMovies, loadPreviousGuesses } from "$lib/middleware";
import { err, ok, type Result } from "$lib/result";
import { userStore } from "$lib/stores";
import type { IGuessService } from "./IGuessService";
import { get as sget } from 'svelte/store';

export abstract class GuessServiceShared implements IGuessService {
    static guessError = "Error making that guess! Try another option.";
    static duplicateGuessError = "Guess already made! Try another option.";


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

    public async getPreviousGuesses(): Promise<Result<GuessDomain[]>> {
        Logger.log("GuessServiceShared.getPreviousGuesses()");

        const jwt = sget(userStore).jwt;

        const prev = await loadPreviousGuesses(jwt);

        let o: GuessDomain[] = [] as GuessDomain[];

        if (prev.ok) {
            for (const id of prev.data!) {
                const g = await this.guess(`${id}`, true);

                if (g.ok) {
                    o.push(g.data!);
                }
            }

            return ok(o);
        }

        return err("Unable to load previous guesses. Try again later.");
    }
}