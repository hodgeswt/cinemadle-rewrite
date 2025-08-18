<script lang="ts">
    import Guess from "$lib/ui/Guess.svelte";
    import BuyMeAPizza from "$lib/ui/BuyMeAPizza.svelte";
    import { Input } from "$lib/components/ui/input";
    import { Info, Search } from "@lucide/svelte";
    import { Button } from "$lib/components/ui/button";
    import { flip } from "svelte/animate";
    import {
        PING_LIMIT,
        getAnswer,
        validateAndRefreshToken,
        healthcheck,
    } from "$lib/middleware";
    import { type GuessDomain, type PossibleMediaDomain } from "$lib/domain";
    import { onMount, untrack } from "svelte";
    import { find } from "$lib/fuzzy";
    import { Skeleton } from "$lib/components/ui/skeleton";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import { writable } from "svelte/store";
    import { userStore } from "$lib/stores";
    import { toast } from "svelte-sonner";
    import type { LoginDto } from "$lib/dto";
    import { Container, type IGuessService } from "$lib/services";
    import Logger from "$lib/logger";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    import VisualClue from "$lib/ui/VisualClue.svelte";
    import type { PurchasesService } from "$lib/services/PurchasesService.svelte";
    import { goto } from "$app/navigation";

    let guessValue = $state("");
    let errorMessage = $state("");
    let shareData = $state([] as string[]);

    let openError = writable(false);
    let openShare = writable(false);
    let openVisualClue = writable(false);
    let visualClueCountDecremented = $state(false);
    let showAnswer = writable(false);
    let guesses = $state([] as GuessDomain[]);

    let searchOpen = $state(false);

    let titles = $derived(guesses.map((x) => x.title));

    let guessService = (): IGuessService => Container.it().GuessService;
    let purchasesService = (): PurchasesService =>
        Container.it().PurchasesService;

    let possibleGuesses = $state({} as PossibleMediaDomain);

    let visualClueCount = $state(0);

    let filteredGuesses = $derived(
        find(guessValue, Object.keys(possibleGuesses))
            .filter((_, i) => i < 10)
            .filter((x) => !titles.includes(x)),
    );

    const LIMIT = 10;

    let remaining = $derived(LIMIT - guesses.length);
    let win = $derived(guesses.filter((x) => x.win).length > 0);
    let lose = $derived(guesses.length >= LIMIT && !win);

    let done = $derived(guesses.length >= LIMIT && win);

    let loading = $state(true);
    let guessServicePing = $state(0);

    let serverDown = $state(false);

    let answer = $state(null as GuessDomain | null);

    $effect(() => {
        if (lose) {
            untrack(async () => {
                let a = await getAnswer();

                if (a.ok) {
                    answer = a.data!;
                    showAnswer.set(true);
                } else {
                    errorMessage =
                        "Unable to pull answer from server. Try again later";
                    openError.set(true);
                }
            });
        }
    });

    onMount(async () => {
        try {
            if (!(await healthcheck())) {
                serverDown = true;
                return;
            }

            if ($userStore.loggedIn) {
                const refreshed = await validateAndRefreshToken(
                    $userStore.jwt,
                    $userStore.refreshToken,
                );

                if (!refreshed.ok) {
                    userStore.setLoggedOut();
                } else {
                    const j = refreshed.data!;
                    if (j !== "") {
                        const loginDto = JSON.parse(j) as LoginDto;
                        userStore.setLoggedIn(
                            $userStore.email,
                            loginDto.accessToken,
                            loginDto.refreshToken,
                        );
                    }
                }

                let quantitiesResult = await purchasesService().getQuantities();
                if (quantitiesResult.ok) {
                    const q = quantitiesResult.data!.quantities;
                    if ("VisualClue" in q) {
                        visualClueCount = q["VisualClue"];
                    }
                }
            }

            while (!guessService().isInitialized()) {
                if (guessServicePing === PING_LIMIT) {
                    serverDown = true;
                    return;
                }

                guessServicePing += 1;
                await new Promise((x) => setTimeout(x, 1000));
            }

            possibleGuesses = guessService().possibleGuesses();
            Logger.log(
                "+page.svelte.onMount: possibleGuesses: {0}",
                possibleGuesses,
            );

            const prev = await guessService().getPreviousGuesses();
            if (!prev.ok) {
                throw new Error(prev.error!);
            }

            for (const g of prev.data!) {
                Logger.log(
                    "+page.svelte.onMount: Making previous guess {0}",
                    g.title,
                );
                guesses.push(g);
            }

            loading = false;
        } catch (e) {
            errorMessage = "Unable to contact server.";
            openError.set(true);
            loading = false;
        }
    });

    async function showShareSheet(_event: Event): Promise<void> {
        const result = await guessService().getGameSummary();

        if (!result.ok) {
            shareData = [result.error!];
        } else {
            shareData = result.data!.summary;
        }

        openShare.set(true);
    }

    function closeShare(_event: Event): void {
        shareData = [] as string[];
        openShare.set(false);
    }

    async function deviceShare(): Promise<void> {
        if (navigator.share) {
            navigator.share({
                title: "cinemadle",
                text: shareData.join("\n"),
                url: "https://cinemadle.com",
            });
        } else {
            navigator.clipboard.writeText(shareData.join("\n"));
            toast("copied to clipboard");
        }
    }

    function guessChange(_event: Event): void {
        Logger.log(
            "+page.svelte.guessChange(): Input changed: {0}",
            guessValue,
        );
        if (guessValue !== "") {
            searchOpen = true;
        } else {
            searchOpen = false;
        }
    }

    async function makeGuess(guess: string): Promise<void> {
        let result = await guessService().guess(guess);

        if (result.ok) {
            guesses.push(result.data!);
        } else {
            errorMessage = result.error!;
            openError.set(true);
        }
    }

    function showVisualClue(_event: Event): void {
        if (!visualClueCountDecremented) {
            visualClueCount -= 1;
        }

        openVisualClue.set(true);
    }

    function closeVisualClue(): void {
        openVisualClue.set(false);
    }

    function closeDialog() {
        openError.set(false);
        errorMessage = "";
    }

    function showAnswerButton(_event: Event): void {
        showAnswer.set(true);
    }

    async function handleSelect(value: string): Promise<void> {
        guessValue = "";
        makeGuess(value);
    }

    async function handleGuess(event: Event | null): Promise<void> {
        event?.preventDefault();

        makeGuess(guessValue);
        guessValue = "";
    }

    function closeAnswer(_event: Event): void {
        showAnswer.set(false);
    }
</script>

<PageWrapper>
    <Header showDate={true} />
    {#if win}
        <div class="flex items-center mb-4">
            <h2
                class="flex-1 text-3xl font-semibold text-green-400 leading-none tracking-tight"
                data-testid="youwin"
            >
                you win!
            </h2>
            <Button
                class="bg-green-400"
                on:click={showShareSheet}
                data-testid="share-button"
            >
                share
            </Button>
        </div>
    {/if}
    {#if lose}
        <div class="flex items-center mb-4">
            <h2
                class="flex-1 text-3xl font-semibold text-red-400 leading-none tracking-tight"
                data-testid="youlose"
            >
                better luck next time!
            </h2>
            <Button
                class="bg-red-400"
                on:click={showShareSheet}
                data-testid="share-button">share</Button
            >
            <Button
                class="bg-red-400 ml-2"
                on:click={showAnswerButton}
                data-testid="seeanswer-button"
            >
                see answer
            </Button>
        </div>
    {/if}

    {#if !loading && !serverDown}
        {#if remaining > 0 && !win}
            <div class="flex space-x-2 mb-4">
                <Input
                    type="text"
                    placeholder={`Guess... ${remaining} remaining`}
                    bind:value={guessValue}
                    onchange={guessChange}
                    class="flex-1 text-base"
                    disabled={done}
                    data-testid="guess-input"
                />

                <Button
                    type="submit"
                    size="icon"
                    onclick={handleGuess}
                    class="relative -z-1000"
                    disabled={done || guessValue.trim() === ""}
                    data-testid="submit-button"
                >
                    <Search />
                </Button>
            </div>
        {/if}

        {#if filteredGuesses.length > 0}
            <ul
                class="mt-1 bg-white border border-gray-300 rounded shadow-xl absolute z-[9999999]"
            >
                {#each filteredGuesses as possibleGuess}
                    <li class="p-2 text-lg">
                        <button
                            onclick={() => {
                                handleSelect(possibleGuess);
                            }}
                            data-testid={`guess-${possibleGuess.replaceAll(" ", "-")}-button`}
                        >
                            {possibleGuess}
                        </button>
                    </li>
                {/each}
            </ul>
        {/if}

        {#if !$userStore.loggedIn}
            <p class="mb-4">
                cinemadle is better when you <a
                    href="/login"
                    class="underline"
                    data-testid="login-page-link">log in</a
                >
            </p>
        {/if}

        {#if $userStore.loggedIn}
            {#if guesses.length >= 6}
                {#if visualClueCount > 0}
                    <div
                        class="mb-4 p-4 bg-gradient-to-r from-indigo-50 to-purple-50 rounded-lg border border-indigo-200"
                    >
                        <div class="flex items-center justify-between">
                            <div class="flex items-center space-x-2">
                                <Info class="text-indigo-600" />
                                <span
                                    class="text-sm text-gray-700"
                                    data-testid="hint-text"
                                    >need a hint? (remaining: {visualClueCount})</span
                                >
                            </div>
                            <Button
                                on:click={showVisualClue}
                                variant="secondary"
                                size="sm"
                                class="bg-indigo-600 hover:bg-indigo-700 text-white"
                                data-testid="visualclue-button"
                            >
                                view visual clue
                            </Button>
                        </div>
                    </div>
                {:else}
                    <div
                        class="mb-4 p-4 bg-gradient-to-r from-indigo-50 to-purple-50 rounded-lg border border-indigo-200"
                    >
                        <div class="flex items-center justify-between">
                            <div class="flex items-center space-x-2">
                                <Info class="text-indigo-600" />
                                <span class="text-sm text-gray-700"
                                    data-testid="hint-text"
                                    >need a hint?</span
                                >
                            </div>
                            <Button
                                on:click={() => goto("/purchase")}
                                variant="secondary"
                                size="sm"
                                class="bg-indigo-600 hover:bg-indigo-700 text-white"
                                data-testid="purchase-button"
                            >
                                purchase visual clues
                            </Button>
                        </div>
                    </div>
                {/if}
            {:else}
                <div
                    class="mb-4 p-4 bg-gradient-to-r from-indigo-50 to-purple-50 rounded-lg border border-indigo-200"
                >
                    <div class="flex items-center justify-between">
                        <div class="flex items-center space-x-2">
                            <Info class="text-indigo-600" />
                            <span class="text-sm text-gray-700" data-testid="visualcluesremaining-text">
                                visual clues remaining: {visualClueCount}
                            </span>
                        </div>
                    </div>
                </div>
            {/if}
        {/if}

        <AlertDialog.Root bind:open={$openError}>
            <AlertDialog.Content>
                <AlertDialog.Title data-testid="error-title-text">uh-oh!</AlertDialog.Title>
                <AlertDialog.Description data-testid="error-body-text">
                    {errorMessage}
                </AlertDialog.Description>
                <AlertDialog.Footer>
                    <AlertDialog.Action on:click={closeDialog} data-testid="error-ok-button">
                        ok
                    </AlertDialog.Action>
                </AlertDialog.Footer>
            </AlertDialog.Content>
        </AlertDialog.Root>

        <AlertDialog.Root bind:open={$openShare}>
            <AlertDialog.Content>
                <AlertDialog.Title data-testid="results-title-text">your results</AlertDialog.Title>
                <AlertDialog.Description data-testid="results-body-text">
                    {#each shareData as line}
                        <div class="leading-none">{line}</div>
                    {/each}
                </AlertDialog.Description>
                <AlertDialog.Footer>
                    <AlertDialog.Action on:click={closeShare} class="m-2" data-testid="share-close-button">
                        close
                    </AlertDialog.Action>
                    <AlertDialog.Action on:click={deviceShare} class="m-2" data-testid="share-share-button">
                        share
                    </AlertDialog.Action>
                </AlertDialog.Footer>
            </AlertDialog.Content>
        </AlertDialog.Root>

        <AlertDialog.Root bind:open={$openVisualClue}>
            <AlertDialog.Content>
                <AlertDialog.Title data-testid="visualclue-title-text">visual clue</AlertDialog.Title>
                <AlertDialog.Description>
                    <VisualClue />
                </AlertDialog.Description>
                <AlertDialog.Footer>
                    <AlertDialog.Action on:click={closeVisualClue} class="m-2" data-testid="visualclue-close-button">
                        close
                    </AlertDialog.Action>
                </AlertDialog.Footer>
            </AlertDialog.Content>
        </AlertDialog.Root>

        <AlertDialog.Root bind:open={$showAnswer}>
            <AlertDialog.Content>
                <AlertDialog.Title data-testid="answer-title-text">the answer is...</AlertDialog.Title>
                <AlertDialog.Description data-testid="answer-body">
                    {#if answer !== null}
                        <Guess props={answer} />
                    {:else}
                        unable to pull answer from server
                    {/if}
                </AlertDialog.Description>
                <AlertDialog.Footer>
                    <AlertDialog.Action on:click={closeAnswer} class="m-2" data-testid="answer-close-button">
                        close
                    </AlertDialog.Action>
                </AlertDialog.Footer>
            </AlertDialog.Content>
        </AlertDialog.Root>

        <div class="guesses z-10">
            {#each [...guesses].reverse() as guess (guess)}
                <div animate:flip={{ duration: 1000 }}>
                    <Guess props={guess} />
                </div>
            {/each}
        </div>
    {:else if loading && !serverDown}
        <div class="min-w-3xl w-full">
            <Skeleton class="h-12 w-full mb-4" />
        </div>
        <div class="grid grid-cols-2 gap-4">
            {#each [1, 2, 3, 4] as _}
                <Skeleton class="flex-grow h-24 w-full rounded-rect" />
            {/each}
        </div>
    {/if}

    {#if serverDown}
        <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="serverdown-text">
            server down. please try again later
        </h2>
    {/if}
    {#if guesses.length > 0}
        <BuyMeAPizza />
    {/if}
</PageWrapper>
