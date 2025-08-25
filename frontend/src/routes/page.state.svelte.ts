import type { GuessDomain, PossibleMediaDomain } from "$lib/domain";
import { getAnswer } from "$lib/middleware";
import { guessStore } from "$lib/stores";
import { find } from "$lib/fuzzy";
import { untrack } from "svelte";
import { writable } from "svelte/store";
import Logger from "$lib/logger";

export class MainState {
    guessInput = $state("");
    errorMessage = $state("");
    shareData = $state([] as string[]);
    searchOpen = $state(false);
    visualCluesDecremented = $state(false);
    visualClueCount = $state(0);
    loading = $state(true);
    guessServicePing = $state(0);
    serverDown = $state(false);
    answer = $state(null as GuessDomain | null);
    paymentsEnabled = $state(false);

    shareOpen = writable(false);
    visualClueOpen = writable(false);
    errorOpen = writable(false);
    answerOpen = writable(false);

    private guesses: GuessDomain[] = $state([]);
    private possibleGuesses: PossibleMediaDomain = $state({});

    LIMIT = 10;
    remaining = $derived(this.LIMIT - this.guesses.length);
    win = $derived(this.guesses.filter((x) => x.win).length > 0);
    lose = $derived(this.guesses.length >= this.LIMIT && !this.win);
    done = $derived(this.guesses.length >= this.LIMIT && this.win);

    titles = $derived(this.guesses.map((x) => x.title));

    filteredGuesses = $derived(
        find(this.guessInput, Object.keys(this.possibleGuesses ?? {}))
            .filter((_, i) => i < 10)
            .filter((x) => !this.titles.includes(x)),
    );


    constructor() {
        guessStore.subscribe((x) => {this.guesses = x.guesses;});
        guessStore.subscribe((x) => this.possibleGuesses = x.possibleGuesses ?? {});

        $effect(() => {
            if (!this.lose) {
                return;
            }

            untrack(async () => {
                let a = await getAnswer();

                if (a.ok) {
                    this.answer = a.data!;
                    this.answerOpen.set(true);
                    return;
                }

                this.errorMessage =
                    "unable to pull answer from server. try again later";
                this.errorOpen.set(true);
            });
        });
    }
}