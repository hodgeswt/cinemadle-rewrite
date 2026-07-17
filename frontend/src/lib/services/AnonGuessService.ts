import type { GuessDomain } from "$lib/domain";
import { get, getCustomGameSummary as fetchCustomGameSummary } from "$lib/middleware";
import { err, ok, type Result } from "$lib/result";
import { isoDateNoTime } from "$lib/util";
import type { IGuessService } from "./IGuessService";
import { GuessDtoToDomain } from "$lib/mappers";
import { GuessServiceShared } from "./GuessServiceShared";
import Logger from "$lib/logger";
import { isGameSummaryDto, isImageDto, type GameSummaryDto, type ImageDto } from "$lib/dto";
import { guessStore } from "$lib/stores";

export class AnonGuessService extends GuessServiceShared implements IGuessService {
    private readonly anonUserIdKey = 'anonUserId';

    constructor() {
        super();
    }

    public async getGameSummary(customGameId?: string): Promise<Result<GameSummaryDto>> {
        const anonUserId = await this.getAnonUserId();
        if (!anonUserId) {
            return err(AnonGuessService.unableToGetBrowserId);
        }

        if (customGameId) {
            const result = await fetchCustomGameSummary(customGameId, anonUserId);

            if (!result.ok) {
                return err(AnonGuessService.unableToLoadGameSummaryError);
            }

            return ok(result.data!);
        }

        let result = await get(
            '/gameSummary',
            { date: isoDateNoTime() },
            { 'X-Cinemadle-UserId': anonUserId }
        );

        if (!result.ok) {
            Logger.log("AnonGuessService.getGameSummary(): got bad response from server")
            return err(AnonGuessService.unableToLoadGameSummaryError);
        }

        const data = JSON.parse(result.data!);
        if (!isGameSummaryDto(data)) {
            Logger.log("AnonGuessService.getGameSummary(): got invalid object {0}", data)
            return err(AnonGuessService.unableToLoadGameSummaryError);
        }

        return ok(data);
    }

    public async getVisualClue(customGameId?: string): Promise<Result<ImageDto>> {
        const anonUserId = await this.getAnonUserId();

        if (anonUserId === undefined) {
            return err(GuessServiceShared.guessError);
        }

        let result = await get(
            '/target/image',
            { date: isoDateNoTime() },
            { "X-Cinemadle-UserId": anonUserId },
            true
        )

        if (!result.ok) {
            Logger.log("AnonGuessService.getVisualClue(): got bad response from server")
            return err(AnonGuessService.unableToLoadImageError);
        }

        const data = JSON.parse(result.data!);

        if (!isImageDto(data)) {
            Logger.log("AnonGuessService.getVisualClue(): got invalid object {0}", data);
            return err(AnonGuessService.unableToLoadImageError);
        }
        return ok(data as ImageDto);
    }

    private async getAnonUserId(): Promise<string | undefined> {
        if (typeof window === 'undefined') {
            Logger.log("AnonGuessService.getAnonUserId(): Window undefined")
            return undefined;
        }

        let storedAnonUserId = localStorage.getItem(this.anonUserIdKey);
        Logger.log("AnonGuessService.getAnonUserId(): Loaded anon id: {0}", storedAnonUserId);

        if (storedAnonUserId === null || storedAnonUserId === "") {
            let result = await get('/anonUserId', null, null, true);

            if (!result.ok) {
                return undefined;
            }

            Logger.log("AnonGuessService.getAnonUserId(): Acquired anon id: {0}", result.data!);

            const data = result.data!
            localStorage.setItem(this.anonUserIdKey, data);
            storedAnonUserId = data;
        }

        return storedAnonUserId;
    }

    public async getPreviousGuesses(customGameId?: string): Promise<Result<GuessDomain[]>> {
        if (customGameId) {
            Logger.log("AnonGuessService.getPreviousGuesses(): custom games unsupported for anon users");
            return err(AnonGuessService.unableToGetPreviousError);
        }

        Logger.log("+GuessService.getPreviousGuesses()");

        const anonUserId = await this.getAnonUserId();

        if (anonUserId === undefined) {
            Logger.log("GuessService.getPreviousGuesses(): unknown anon user id");
            Logger.log("-GuessService.getPreviousGuesses()");
            return err(AnonGuessService.unableToGetPreviousError);
        }

        let prev = await get('/guesses', { date: isoDateNoTime() }, { "X-Cinemadle-UserId": anonUserId })

        let o: GuessDomain[] = [] as GuessDomain[];

        if (prev.ok) {
            const d = JSON.parse(prev.data!);
            for (const id of d) {
                const g = await this.guess(`${id}`, true);

                if (g.ok) {
                    o.push(g.data!);
                }
            }

            return ok(o);
        }

        return err("Unable to load previous guesses. Try again later.");
    }

    public async guess(guess: string, skipTitleMap?: boolean): Promise<Result<GuessDomain>> {
        if (guess.trim() === "") {
            return err("Invalid guess");
        }

        Logger.log("GuessService.svelte.ts: guess: {0}, skipTitleMap: {1}", guess, skipTitleMap);

        const id = skipTitleMap !== true ? this._possibleGuesses[guess] : guess;

        const anonUserId = await this.getAnonUserId();

        if (anonUserId === undefined) {
            Logger.log("GuessService.svelte.ts: undefined userid");
            return err(GuessServiceShared.guessError);
        }

        let result = await get(
            `/guess/${id}`,
            { date: isoDateNoTime() },
            { "X-Cinemadle-UserId": anonUserId },
        );

        const title = skipTitleMap !== true ? guess : this.getTitle(guess);
        Logger.log("GuessService.svelte.ts: title {0}", title);

        if (this._guesses.includes(title)) {
            Logger.log("GuessService.svelte.ts: bad title {0}", title);
            return err(AnonGuessService.duplicateGuessError);
        }

        if (result.ok) {
            try {
                let dto = JSON.parse(result.data as string);
                let domain = GuessDtoToDomain(dto, title);

                if (domain.ok) {
                    Logger.log("GuessService.svelte.ts: good title {0}", title);
                    if (!this._guesses.includes(title)) {
                        guessStore.update(s => ({ ...s, guesses: [...s.guesses, domain.data as GuessDomain] }));

                    }
                    return ok(domain.data as GuessDomain);
                } else {
                    Logger.log("GuessService.svelte.ts: bad data", title);
                    return err(AnonGuessService.guessError)
                }
            }
            catch (e) {
                Logger.log("GuessService.svelte.ts: caught error {0}", e);
                return err(AnonGuessService.guessError)
            }
        }

        Logger.log("GuessService.svelte.ts: bad result");
        return err(AnonGuessService.guessError);
    }

    public async guessCustomGame(_customGameId: string, _guess: string, _skipTitleMap?: boolean): Promise<Result<GuessDomain>> {
        Logger.log("AnonGuessService.guessCustomGame(): custom games unsupported for anon users");
        return err(AnonGuessService.guessError);
    }
}

