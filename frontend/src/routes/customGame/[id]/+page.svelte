<script lang="ts">
    import Guess from "$lib/ui/Guess.svelte";
    import BuyMeAPizza from "$lib/ui/BuyMeAPizza.svelte";
    import HintsDisplay from "$lib/ui/HintsDisplay.svelte";
    import { Input } from "$lib/components/ui/input";
    import { Info, Search } from "@lucide/svelte";
    import { Button } from "$lib/components/ui/button";
    import { flip } from "svelte/animate";
    import { isDarkMode } from "$lib/stores/theme";
    import {
        PING_LIMIT,
        validateAndRefreshToken,
        healthcheck,
        getCustomGame,
    } from "$lib/middleware";
    import { FeatureFlags } from "$lib/domain";
    import { onDestroy, onMount } from "svelte";
    import { Skeleton } from "$lib/components/ui/skeleton";
    import { guessStore, userStore, hintsStore } from "$lib/stores";
    import { toast } from "svelte-sonner";
    import type { LoginDto } from "$lib/dto";
    import { Container, type IGuessService } from "$lib/services";
    import Logger from "$lib/logger";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    import VisualClue from "$lib/ui/VisualClue.svelte";
    import type { PurchasesService } from "$lib/services/PurchasesService.svelte";
    import { goto } from "$app/navigation";
    import Dialog from "$lib/ui/Dialog.svelte";
    import { CustomGameState } from "./page.state.svelte";
    import { get } from "svelte/store";

    export let data: { id: string };

    const customGameId = data.id;
    const pageState = new CustomGameState(customGameId);

    let guessService = (): IGuessService => Container.it().GuessService;
    let purchasesService = (): PurchasesService => Container.it().PurchasesService;

    const paymentsEnabled = Container.it().FeatureFlagService.getFeatureFlag(FeatureFlags.PaymentsEnabled);

    onMount(async () => {
        Logger.log("customGame/+page.svelte.onMount {0}", customGameId);

        try {
            if (!(await healthcheck())) {
                pageState.serverDown = true;
                return;
            }

            if (!get(userStore).loggedIn) {
                goto(`/login?redirect=/customGame/${customGameId}`);
                return;
            }

            const jwt = get(userStore).jwt;
            const refresh = get(userStore).refreshToken;

            const refreshed = await validateAndRefreshToken(jwt, refresh);

            if (!refreshed.ok) {
                userStore.setLoggedOut();
                goto(`/login?redirect=/customGame/${customGameId}`);
                return;
            }

            const j = refreshed.data!;
            if (j !== "") {
                const loginDto = JSON.parse(j) as LoginDto;
                userStore.setLoggedIn(
                    get(userStore).email,
                    loginDto.accessToken,
                    loginDto.refreshToken,
                );
            }

            const verification = await getCustomGame(customGameId, get(userStore).jwt);
            if (!verification.ok) {
                pageState.errorMessage = "custom game unavailable. please check your link.";
                pageState.errorOpen.set(true);
                pageState.loading = false;
                return;
            }

            if (await paymentsEnabled) {
                let quantitiesResult = await purchasesService().getQuantities();
                pageState.paymentsEnabled = true;
                if (quantitiesResult.ok) {
                    const q = quantitiesResult.data!.quantities;
                    if ("VisualClue" in q) {
                        pageState.visualClueCount = q["VisualClue"];
                    }
                }
            }
            else {
                pageState.visualClueCount = -1;
                pageState.paymentsEnabled = false;
            }

            while (!guessService().isInitialized()) {
                if (pageState.guessServicePing === PING_LIMIT) {
                    pageState.serverDown = true;
                    return;
                }

                pageState.guessServicePing += 1;
                await new Promise((x) => setTimeout(x, 1000));
            }

            guessStore.update((s) => ({ ...s, guesses: [] }));

            const prev = await guessService().getPreviousGuesses(customGameId);
            if (!prev.ok) {
                Logger.log("customGame/+page.svelte.onMount: failed previous guesses {0}", prev.error);
                throw new Error(prev.error!);
            }

            for (const g of prev.data!) {
                if (!get(guessStore).guesses.includes(g)) {
                    guessStore.update((s) => ({ ...s, guesses: [...s.guesses, g] }));
                }
            }

            // Fetch hints asynchronously (non-blocking) for custom game
            hintsStore.fetchHints(get(userStore).jwt, customGameId);

            pageState.loading = false;
        } catch (e) {
            Logger.log("customGame/+page.svelte.onMount: caught error {0}", JSON.stringify(e));
            pageState.errorMessage = "Unable to contact server.";
            pageState.errorOpen.set(true);
            pageState.loading = false;
        }
    });

    onDestroy(() => {
        guessStore.update((s) => ({ ...s, guesses: [] }));
    });

    async function showShareSheet(_event: Event): Promise<void> {
        const result = await guessService().getGameSummary(customGameId);

        if (!result.ok) {
            pageState.shareData = [result.error!];
        } else {
            pageState.shareData = result.data!.summary;
        }

        pageState.shareOpen.set(true);
    }

    async function deviceShare(): Promise<void> {
        if (navigator.share) {
            navigator.share({
                title: "cinemadle",
                text: pageState.shareData.join("\n"),
                url: `https://cinemadle.com/customGame/${customGameId}`,
            });
        } else {
            navigator.clipboard.writeText(pageState.shareData.join("\n"));
            toast("copied to clipboard");
        }
    }

    function guessChange(_event: Event): void {
        Logger.log(
            "customGame/+page.svelte.guessChange(): Input changed: {0}",
            pageState.guessInput,
        );
        if (pageState.guessInput !== "") {
            pageState.searchOpen = true;
        } else {
            pageState.searchOpen = false;
        }
    }

    async function makeGuess(guess: string): Promise<void> {
        let result = await guessService().guessCustomGame(customGameId, guess);

        if (!result.ok) {
            pageState.errorMessage = result.error!;
            pageState.errorOpen.set(true);
        } else {
            // Refresh hints after successful guess (non-blocking)
            hintsStore.invalidate();
            hintsStore.fetchHints(get(userStore).jwt, customGameId);
        }
    }

    function showVisualClue(_event: Event): void {
        if (!pageState.visualCluesDecremented) {
            pageState.visualClueCount -= 1;
        }

        pageState.visualClueOpen.set(true);
    }

    function showAnswerButton(_event: Event): void {
        pageState.answerOpen.set(true);
    }

    async function handleSelect(value: string): Promise<void> {
        pageState.guessInput = "";
        makeGuess(value);
    }

    async function handleGuess(event: Event | null): Promise<void> {
        event?.preventDefault();

        makeGuess(pageState.guessInput);
        pageState.guessInput = "";
    }

</script>

<PageWrapper>
    <Header showDate={true} customGameId={customGameId} />
    {#if pageState.win}
        <div class="flex items-center mb-4">
            <h2
                class="flex-1 text-3xl font-semibold text-green-400 leading-none tracking-tight"
                data-testid="customgame-youwin"
            >
                you win!
            </h2>
            <Button
                class="bg-green-400"
                onclick={showShareSheet}
                data-testid="customgame-share-button"
            >
                share
            </Button>
        </div>
    {/if}
    {#if pageState.lose}
        <div class="flex items-center mb-4">
            <h2
                class="flex-1 text-3xl font-semibold text-red-400 leading-none tracking-tight"
                data-testid="customgame-youlose"
            >
                better luck next time!
            </h2>
            <Button
                class="bg-red-400"
                onclick={showShareSheet}
                data-testid="customgame-share-button"
            >
                share
            </Button>
            <Button
                class="bg-red-400 ml-2"
                onclick={showAnswerButton}
                data-testid="customgame-seeanswer-button"
            >
                see answer
            </Button>
        </div>
    {/if}

    {#if !pageState.loading && !pageState.serverDown}
        {#if pageState.remaining > 0 && !pageState.win}
            <div class="flex space-x-2 mb-4">
                <Input
                    type="text"
                    placeholder={`Guess... ${pageState.remaining} remaining`}
                    bind:value={pageState.guessInput}
                    onchange={guessChange}
                    class="flex-1 text-base"
                    disabled={pageState.done}
                    data-testid="customgame-guess-input"
                />

                <Button
                    type="submit"
                    size="icon"
                    onclick={handleGuess}
                    class="relative -z-1000"
                    disabled={pageState.done || pageState.guessInput.trim() === ""}
                    data-testid="customgame-submit-button"
                >
                    <Search />
                </Button>
            </div>
        {/if}

        {#if pageState.filteredGuesses.length > 0}
            <ul
                class="mt-1 {$isDarkMode ? 'bg-gray-900' : 'bg-white'} {$isDarkMode ? 'border-gray-700' : 'border-gray-300'} border rounded shadow-xl absolute z-[9999999]"
            >
                {#each pageState.filteredGuesses as possibleGuess}
                    <li class="p-2 text-lg">
                        <button
                            onclick={() => {
                                handleSelect(possibleGuess);
                            }}
                            data-testid={`customgame-guess-${possibleGuess.replaceAll(" ", "-")}-button`}
                        >
                            {possibleGuess}
                        </button>
                    </li>
                {/each}
            </ul>
        {/if}

        {#if $guessStore.guesses.length > 0}
            <HintsDisplay />
        {/if}

        {#if $userStore.loggedIn}
            {#if $guessStore.guesses.length >= 6}
                {#if pageState.visualClueCount > 0 || !pageState.paymentsEnabled}
                    <div
                        class="mb-4 p-4 {$isDarkMode ? 'bg-gradient-to-r from-indigo-600 to-purple-800 border-indigo-800' : 'bg-gradient-to-r from-indigo-50 to-purple-50 border-indigo-200'} rounded-lg border {$isDarkMode ? 'border-indigo-800' : 'border-indigo-200'}"
                    >
                        <div class="flex items-center justify-between">
                            <div class="flex items-center space-x-2">
                                <Info class={$isDarkMode ? 'text-white' : 'text-indigo-600'} />
                                <span
                                    class="text-sm {$isDarkMode ? 'text-white' : 'text-indigo-400'}"
                                    data-testid="customgame-hint-text"
                                    >need a hint? {pageState.paymentsEnabled ? `(remaining: ${pageState.visualClueCount})` : ""}</span
                                >
                            </div>
                            <Button
                                onclick={showVisualClue}
                                variant="secondary"
                                size="sm"
                                class="bg-indigo-600 hover:bg-indigo-700 text-white"
                                data-testid="customgame-visualclue-button"
                            >
                                view visual clue
                            </Button>
                        </div>
                    </div>
                {:else}
                    <div
                        class="mb-4 p-4 {$isDarkMode ? 'bg-gradient-to-r from-indigo-600 to-purple-800 border-indigo-800' : 'bg-gradient-to-r from-indigo-50 to-purple-50 border-indigo-200'} rounded-lg border {$isDarkMode ? 'border-indigo-800' : 'border-indigo-200'}"
                    >
                        <div class="flex items-center justify-between">
                            <div class="flex items-center space-x-2">
                                <Info class={$isDarkMode ? 'text-white' : 'text-indigo-400'} />
                                <span class="text-sm {$isDarkMode ? 'text-white' : 'text-indigo-400'}"
                                    data-testid="customgame-hint-text"
                                    >need a hint?</span
                                >
                            </div>
                            <Button
                                onclick={() => goto("/purchase")}
                                variant="secondary"
                                size="sm"
                                class="bg-indigo-600 hover:bg-indigo-700 text-white"
                                data-testid="customgame-purchase-button"
                            >
                                purchase visual clues
                            </Button>
                        </div>
                    </div>
                {/if}
            {:else if pageState.visualClueCount !== -1}
                <div
                    class="mb-4 p-4 {$isDarkMode ? 'bg-gradient-to-r from-indigo-600 to-purple-800 border-indigo-800' : 'bg-gradient-to-r from-indigo-50 to-purple-50 border-indigo-200'} rounded-lg border {$isDarkMode ? 'border-indigo-800' : 'border-indigo-200'}"
                >
                    <div class="flex items-center justify-between">
                        <div class="flex items-center space-x-2">
                            <Info class={$isDarkMode ? 'text-white' : 'text-indigo-400'} />
                            <span class="text-sm {$isDarkMode ? 'text-white' : 'text-gray-700'}" data-testid="customgame-visualcluesremaining-text">
                                visual clues remaining: {pageState.visualClueCount}
                            </span>
                        </div>
                    </div>
                </div>
            {/if}
        {/if}

        <Dialog open={pageState.errorOpen} title="uh-oh!" id="customgame-error" confirmButton="ok">
            {pageState.errorMessage}
        </Dialog>

        <Dialog open={pageState.shareOpen} title="your results" id="customgame-results" confirmButton="close" cancelButton="share" cancelCallback={() => deviceShare()}>
            {#each pageState.shareData as line}
                <div class="leading-none">{line}</div>
            {/each}
        </Dialog>

        <Dialog open={pageState.visualClueOpen} title="visual clue" id="customgame-visualclue" confirmButton="ok">
            <VisualClue customGameId={customGameId} />
        </Dialog>

        <Dialog open={pageState.answerOpen} title="the answer is..." id="customgame-answer" confirmButton="ok">
            {#if pageState.answer !== null}
                <Guess props={pageState.answer} />
            {:else}
                unable to pull answer from server
            {/if}
        </Dialog>

        <div class="guesses z-10">
            {#each [...$guessStore.guesses].reverse() as guess, i (guess)}
                <div animate:flip={{ duration: 1000 }}>
                    <Guess props={guess} index={i} />
                </div>
            {/each}
        </div>
    {:else if pageState.loading && !pageState.serverDown}
        <div class="min-w-3xl w-full">
            <Skeleton class="h-12 w-full mb-4" />
        </div>
        <div class="grid grid-cols-2 gap-4">
            {#each [1, 2, 3, 4] as _}
                <Skeleton class="flex-grow h-24 w-full rounded-rect" />
            {/each}
        </div>
    {/if}

    {#if pageState.serverDown}
        <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="customgame-serverdown-text">
            server down. please try again later
        </h2>
    {/if}
    {#if $guessStore.guesses.length > 0}
        <BuyMeAPizza />
    {/if}
</PageWrapper>
