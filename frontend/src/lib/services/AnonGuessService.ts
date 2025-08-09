import type { GuessDomain } from "$lib/domain";
import { get } from "$lib/middleware";
import { err, ok, type Result } from "$lib/result";
import { isoDateNoTime } from "$lib/util";
import type { IGuessService } from "./IGuessService";
import { GuessDtoToDomain } from "$lib/mappers";
import { GuessServiceShared } from "./GuessServiceShared";
import Logger from "$lib/logger";
import { isGameSummaryDto, isImageDto, type GameSummaryDto, type ImageDto } from "$lib/dto";

export class AnonGuessService extends GuessServiceShared implements IGuessService {
    private readonly anonUserIdKey = 'anonUserId';

    constructor() {
        super();
    }

    public async getGameSummary(): Promise<Result<GameSummaryDto>> {
        const anonUserId = await this.getAnonUserId();

        if (anonUserId === undefined) {
            return err(GuessServiceShared.unableToLoadGameSummaryError);
        }

        let result = await get(
            '/gameSummary/anon',
            { date: isoDateNoTime(), userId: anonUserId },
            null
        );

        if (!result.ok) {
            Logger.log("AnonGuessService.getGameSummary(): got bad response from server")
            return err(GuessServiceShared.unableToLoadGameSummaryError);
        }

        const data = JSON.parse(result.data!);
        if (!isGameSummaryDto(data)) {
            Logger.log("AnonGuessService.getGameSummary(): got invalid object {0}", data)
            return err(GuessServiceShared.unableToLoadGameSummaryError);
        }

        return ok(data);
    }

    public async getVisualClue(): Promise<Result<ImageDto>> {
        const anonUserId = await this.getAnonUserId();

        if (anonUserId === undefined) {
            return err(GuessServiceShared.guessError);
        }

        let result = await get(
            '/target/image/anon',
            { date: isoDateNoTime(), userId: anonUserId },
            null,
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

    public async getPreviousGuesses(): Promise<Result<GuessDomain[]>> {
        Logger.log("GuessService.getPreviousGuesses()");

        const anonUserId = await this.getAnonUserId();

        if (anonUserId === undefined) {
            return err(AnonGuessService.unableToGetPreviousError);
        }

        let prev = await get('/guesses/anon', {date: isoDateNoTime(), userId: anonUserId })

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
            return err(GuessServiceShared.guessError);
        }

        let result = await get(
            `/guess/anon/${id}`,
            { date: isoDateNoTime(), userId: anonUserId },
        );

        const title = skipTitleMap !== true ? guess : this.getTitle(guess);

        if (this._guesses.includes(title)) {
            return err(AnonGuessService.duplicateGuessError);
        }

        if (result.ok) {
            try {
                let dto = JSON.parse(result.data as string);
                let domain = GuessDtoToDomain(dto, title);

                if (domain.ok) {
                    this._guesses.push(title);
                    return ok(domain.data as GuessDomain);
                } else {
                    return err(AnonGuessService.guessError)
                }
            }
            catch {
                return err(AnonGuessService.guessError)
            }
        }

        return err(AnonGuessService.guessError);
    }
}
