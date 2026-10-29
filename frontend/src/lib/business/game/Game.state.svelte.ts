import type { GuessDomain, PossibleMediaDomain } from "$lib/domain";
import { getAnswer } from "$lib/middleware";
import { guessStore } from "$lib/stores";
import { find } from "$lib/fuzzy";
import { untrack } from "svelte";
import { writable } from "svelte/store";
import { SvelteMap } from "svelte/reactivity";
import GameDialogs from "./subcomponents/GameDialogs.svelte";
import type { IGuessService } from "$lib/services/IGuessService";

class GameState {
    readonly LIMIT = 10;


    guessInput = $state("");
    errorMessage = $state("");
    shareData = $state([] as string[]);
    searchOpen = $state(false);
    loading = $state(true);
    guessServicePing = $state(0);
    answer = $state(null as GuessDomain | null);
    dialogs: GameDialogs | undefined = $state();

    answerOpen = writable(false);

    private guesses: GuessDomain[] = $state([]);

    anyGuesses = $derived(this.guesses.length > 0);
    remainingGuesses = $derived(this.LIMIT - this.guesses.length);
    visualClueEligible = $derived(this.guesses.length >= 6);

    guessService: IGuessService | undefined = $state();

    win = $derived(this.guesses.filter((x) => x.win).length > 0);
    lose = $derived(this.guesses.length >= this.LIMIT && !this.win);
    done = $derived(this.guesses.length >= this.LIMIT && this.win);
    gameOver = $derived(this.win || this.lose);

    titles = $derived(this.guesses.map((x) => x.title));

    filteredGuesses = $derived(
        this.guessService ?
            find(
                this.guessInput,
                this.guessService.possibleGuessTitles()
            )
                .filter((x) => !this.titles.includes(x))
                .filter((_, i) => i < 10) : []
    );

    noWinGuesses = $derived(this.guesses.filter(g => !g.win))

    constructor() {
        guessStore.subscribe((x) => {
            this.guesses = [...x.guesses].reverse()
        });

        $effect(() => {
            if (!this.lose) {
                return;
            }

            untrack(async () => {
                let a = await getAnswer();

                if (a.ok) {
                    this.answer = a.data!;
                    return;
                }
            });
        });
    }
};

const gameStates = new SvelteMap<string, GameState>();

export const getGameState = (id: string) => gameStates.getOrInsert(id, new GameState());
export const removeGameState = (id: string) => (() => gameStates.delete(id))();