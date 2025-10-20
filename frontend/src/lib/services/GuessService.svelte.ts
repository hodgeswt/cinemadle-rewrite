import type { GuessDomain } from "$lib/domain";
import { get, loadPreviousGuesses } from "$lib/middleware";
import { err, ok, type Result } from "$lib/result";
import { isoDateNoTime } from "$lib/util";
import type { IGuessService } from "./IGuessService";
import { userStore } from "$lib/stores";
import { GuessDtoToDomain } from "$lib/mappers";
import { GuessServiceShared } from "./GuessServiceShared";
import Logger from "$lib/logger";
import { isGameSummaryDto, isImageDto, type GameSummaryDto, type ImageDto } from "$lib/dto";
import { get as sget } from 'svelte/store';
import { guessStore } from "$lib/stores";

export class GuessService extends GuessServiceShared implements IGuessService {
    constructor() {
        super();
    }

    public async getGameSummary(): Promise<Result<GameSummaryDto>> {
        let result = await get(
            '/gameSummary',
            { date: isoDateNoTime() },
            { Authorization: sget(userStore).jwt }
        );

        if (!result.ok) {
            Logger.log("GuessService.getGameSummary(): got bad response from server")
            return err(GuessService.unableToLoadGameSummaryError);
        }

        const data = JSON.parse(result.data!);
        if (!isGameSummaryDto(data)) {
            Logger.log("GuessService.getGameSummary(): got invalid object {0}", data)
            return err(GuessService.unableToLoadGameSummaryError);
        }

        return ok(data);
    }

    public async getVisualClue(): Promise<Result<ImageDto>> {
        let result = await get(
            '/target/image',
            { date: isoDateNoTime() },
            { Authorization: sget(userStore).jwt }
        )

        if (!result.ok) {
            Logger.log("GuessService.getVisualClue(): got bad response from server")
            return err(GuessService.unableToLoadImageError);
        }

        const data = JSON.parse(result.data!);
        if (!isImageDto(data)) {
            Logger.log("GuessService.getVisualClue(): got invalid object {0}", data);
            return err(GuessService.unableToLoadImageError);
        }

        return ok(data as ImageDto);
    }

    public async guess(guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>> {
        if (guess.trim() === "") {
            return err("Invalid guess");
        }

        Logger.log("GuessService.svelte.ts: guess: {0}, skipTitleMap: {1}", guess, skipTitleMap);

        const id = skipTitleMap !== true ? this._possibleGuesses[guess] : guess;

        let result = await get(
            `/guess/${id}`,
            { date: isoDateNoTime() },
            { Authorization: sget(userStore).jwt },
        );

        const title = skipTitleMap !== true ? guess : this.getTitle(guess);

        if (this._guesses.includes(title)) {
            return err(GuessService.duplicateGuessError);
        }

        if (result.ok) {
            try {
                let dto = JSON.parse(result.data as string);
                let domain = GuessDtoToDomain(dto, title);

                if (domain.ok) {
                    if (!this._guesses.includes(title)) {
                        guessStore.update(s => ({ ...s, guesses: [...s.guesses, domain.data as GuessDomain] }));
                    }
                    return ok(domain.data as GuessDomain);
                } else {
                    return err(GuessService.guessError)
                }
            }
            catch {
                return err(GuessService.guessError)
            }
        }

        return err(GuessService.guessError);
    }

    public async guessCustomGame(customGameId: string, guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>> {
        if (guess.trim() === "") {
            return err("Invalid guess");
        }

        if (!customGameId || customGameId.trim() === "") {
            return err("Invalid custom game ID");
        }

        Logger.log("GuessService.svelte.ts: guessCustomGame: customGameId: {0}, guess: {1}, skipTitleMap: {2}", customGameId, guess, skipTitleMap);

        const id = skipTitleMap !== true ? this._possibleGuesses[guess] : guess;

        let result = await get(
            `/custom/${customGameId}/guess/${id}`,
            null,
            { Authorization: sget(userStore).jwt },
        );

        const title = skipTitleMap !== true ? guess : this.getTitle(guess);

        if (this._guesses.includes(title)) {
            return err(GuessService.duplicateGuessError);
        }

        if (result.ok) {
            try {
                let dto = JSON.parse(result.data as string);
                let domain = GuessDtoToDomain(dto, title);

                if (domain.ok) {
                    if (!this._guesses.includes(title)) {
                        guessStore.update(s => ({ ...s, guesses: [...s.guesses, domain.data as GuessDomain] }));
                    }
                    return ok(domain.data as GuessDomain);
                } else {
                    return err(GuessService.guessError)
                }
            }
            catch {
                return err(GuessService.guessError)
            }
        }

        return err(GuessService.guessError);
    }
    
    public async getPreviousGuesses(): Promise<Result<GuessDomain[]>> {
        Logger.log("GuessService.getPreviousGuesses()");

        const jwt = sget(userStore).jwt;

        const prev = await loadPreviousGuesses(jwt);

        let o: GuessDomain[] = [] as GuessDomain[];

        if (prev.ok) {
            Logger.log("GuessService.getPreviousGuesses(): guesses: {0}", prev.data!);
            for (const id of prev.data!) {
                const g = await this.guess(`${id}`, true);

                if (g.ok) {
                    o.push(g.data! as GuessDomain);
                }
            }

            return ok(o);
        }

        return err("Unable to load previous guesses. Try again later.");
    }
}
