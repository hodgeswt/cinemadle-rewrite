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
    } from "$lib/middleware";
    import { FeatureFlags } from "$lib/domain";
    import { onMount } from "svelte";
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
    import { MainState } from "./page.state.svelte";

    const mainState = new MainState();

    let guessService = (): IGuessService => Container.it().GuessService;
    let purchasesService = (): PurchasesService => Container.it().PurchasesService;

    const paymentsEnabled = Container.it().FeatureFlagService.getFeatureFlag(FeatureFlags.PaymentsEnabled);

    onMount(async () => {
        Logger.log("+page.svelte.onMount");
        try {
            if (!(await healthcheck())) {
                mainState.serverDown = true;
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

                if (await paymentsEnabled) {
                    let quantitiesResult = await purchasesService().getQuantities();
                    mainState.paymentsEnabled = true;
                    if (quantitiesResult.ok) {
                        const q = quantitiesResult.data!.quantities;
                        if ("VisualClue" in q) {
                            mainState.visualClueCount = q["VisualClue"];
                        }
                    }
                }
                else {
                    mainState.visualClueCount = -1;
                    mainState.paymentsEnabled = false;
                }
                
            }

            while (!guessService().isInitialized()) {
                if (mainState.guessServicePing === PING_LIMIT) {
                    mainState.serverDown = true;
                    return;
                }

                mainState.guessServicePing += 1;
                await new Promise((x) => setTimeout(x, 1000));
            }

            const prev = await guessService().getPreviousGuesses();
            if (!prev.ok) {
                Logger.log("+page.svelte.onMount: failed guesses");
                throw new Error(prev.error!);
            }

            Logger.log("+page.svelte.onMount: Prev guesses {0}", prev.data!);

            for (const g of prev.data!) {
                Logger.log(
                    "+page.svelte.onMount: Making previous guess {0}",
                    g.title,
                );
                if (!$guessStore.guesses.includes(g)) {
                    $guessStore.guesses.push(g);
                }
            }

            // Fetch hints asynchronously (non-blocking)
            if ($userStore.loggedIn) {
                hintsStore.fetchHints($userStore.jwt);
            } else {
                // Anonymous user - get their anon ID from localStorage
                const anonUserId = localStorage.getItem('anonUserId');
                if (anonUserId) {
                    hintsStore.fetchHintsAnon(anonUserId);
                }
            }

            mainState.loading = false;
        } catch (e) {
            mainState.errorMessage = "Unable to contact server.";
            mainState.errorOpen.set(true);
            mainState.loading = false;
        }
    });

    async function showShareSheet(_event: Event): Promise<void> {
        const result = await guessService().getGameSummary();

        if (!result.ok) {
            mainState.shareData = [result.error!];
        } else {
            mainState.shareData = result.data!.summary;
        }

        mainState.shareOpen.set(true);
    }

    async function deviceShare(): Promise<void> {
        if (navigator.share) {
            navigator.share({
                title: "cinemadle",
                text: mainState.shareData.join("\n"),
                url: "https://cinemadle.com",
            });
        } else {
            navigator.clipboard.writeText(mainState.shareData.join("\n"));
            toast("copied to clipboard");
        }
    }

    function guessChange(_event: Event): void {
        Logger.log(
            "+page.svelte.guessChange(): Input changed: {0}",
            mainState.guessInput,
        );
        if (mainState.guessInput !== "") {
            mainState.searchOpen = true;
        } else {
            mainState.searchOpen = false;
        }
    }

    async function makeGuess(guess: string): Promise<void> {
        let result = await guessService().guess(guess);

        if (!result.ok) {
            mainState.errorMessage = result.error!;
            mainState.errorOpen.set(true);
        } else {
            // Refresh hints after successful guess (non-blocking)
            hintsStore.invalidate();
            if ($userStore.loggedIn) {
                hintsStore.fetchHints($userStore.jwt);
            } else {
                const anonUserId = localStorage.getItem('anonUserId');
                if (anonUserId) {
                    hintsStore.fetchHintsAnon(anonUserId);
                }
            }
        }
    }

    function showVisualClue(_event: Event): void {
        if (!mainState.visualCluesDecremented) {
            mainState.visualClueCount -= 1;
        }

        mainState.visualClueOpen.set(true);
    }

    function showAnswerButton(_event: Event): void {
        mainState.answerOpen.set(true);
    }

    async function handleSelect(value: string): Promise<void> {
        mainState.guessInput = "";
        makeGuess(value);
    }

    async function handleGuess(event: Event | null): Promise<void> {
        event?.preventDefault();

        makeGuess(mainState.guessInput);
        mainState.guessInput = "";
    }

    function closeAnswer(_event: Event): void {
        mainState.answerOpen.set(false);
    }
</script>

<PageWrapper>
    <Header showDate={true} />
    {#if mainState.win}
        <div class="flex items-center mb-4">
            <h2
                class="flex-1 text-3xl font-semibold text-green-400 leading-none tracking-tight"
                data-testid="youwin"
            >
                you win!
            </h2>
            <Button
                class="bg-green-400"
                onclick={showShareSheet}
                data-testid="share-button"
            >
                share
            </Button>
        </div>
    {/if}
    {#if mainState.lose}
        <div class="flex items-center mb-4">
            <h2
                class="flex-1 text-3xl font-semibold text-red-400 leading-none tracking-tight"
                data-testid="youlose"
            >
                better luck next time!
            </h2>
            <Button
                class="bg-red-400"
                onclick={showShareSheet}
                data-testid="share-button">share</Button
            >
            <Button
                class="bg-red-400 ml-2"
                onclick={showAnswerButton}
                data-testid="seeanswer-button"
            >
                see answer
            </Button>
        </div>
    {/if}

    {#if !mainState.loading && !mainState.serverDown}
        {#if mainState.remaining > 0 && !mainState.win}
            <div class="flex space-x-2 mb-4">
                <Input
                    type="text"
                    placeholder={`Guess... ${mainState.remaining} remaining`}
                    bind:value={mainState.guessInput}
                    onchange={guessChange}
                    class="flex-1 text-base"
                    disabled={mainState.done}
                    data-testid="guess-input"
                />

                <Button
                    type="submit"
                    size="icon"
                    onclick={handleGuess}
                    class="relative -z-1000"
                    disabled={mainState.done || mainState.guessInput.trim() === ""}
                    data-testid="submit-button"
                >
                    <Search />
                </Button>
            </div>
        {/if}

        {#if mainState.filteredGuesses.length > 0}
            <ul
                class="mt-1 {$isDarkMode ? 'bg-gray-900' : 'bg-white'} {$isDarkMode ? 'border-gray-700' : 'border-gray-300'} border rounded shadow-xl absolute z-[9999999]"
            >
                {#each mainState.filteredGuesses as possibleGuess}
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
            {#if $guessStore.guesses.length >= 6}
                {#if mainState.visualClueCount > 0 || !mainState.paymentsEnabled}
                    <div
                        class="mb-4 p-4 {$isDarkMode 
                            ? (mainState.lose ? 'bg-gradient-to-r from-red-600 to-red-800 border-red-800' : mainState.win ? 'bg-gradient-to-r from-green-600 to-green-800 border-green-800' : 'bg-gradient-to-r from-indigo-600 to-purple-800 border-indigo-800') 
                            : (mainState.lose ? 'bg-gradient-to-r from-red-50 to-red-100 border-red-200' : mainState.win ? 'bg-gradient-to-r from-green-50 to-green-100 border-green-200' : 'bg-gradient-to-r from-indigo-50 to-purple-50 border-indigo-200')} rounded-lg border {$isDarkMode 
                            ? (mainState.lose ? 'border-red-800' : mainState.win ? 'border-green-800' : 'border-indigo-800') 
                            : (mainState.lose ? 'border-red-200' : mainState.win ? 'border-green-200' : 'border-indigo-200')}"
                    >
                        <div class="flex items-center justify-between">
                            <div class="flex items-center space-x-2">
                                <Info class={$isDarkMode ? 'text-white' : (mainState.lose ? 'text-red-600' : mainState.win ? 'text-green-600' : 'text-indigo-600')} />
                                <span
                                    class="text-sm {$isDarkMode ? 'text-white' : (mainState.lose ? 'text-red-400' : mainState.win ? 'text-green-400' : 'text-indigo-400')}"
                                    data-testid="hint-text"
                                    >{mainState.lose ? 'needed a hint?' : mainState.win ? 'needed a hint?' : 'need a hint?'} {mainState.paymentsEnabled ? `(remaining: ${mainState.visualClueCount})` : ""}</span
                                >
                            </div>
                            <Button
                                onclick={showVisualClue}
                                variant="secondary"
                                size="sm"
                                class="{mainState.lose ? 'bg-red-600 hover:bg-red-700' : mainState.win ? 'bg-green-600 hover:bg-green-700' : 'bg-indigo-600 hover:bg-indigo-700'} text-white"
                                data-testid="visualclue-button"
                            >
                                view visual clue
                            </Button>
                        </div>
                    </div>
                {:else}
                    <div
                        class="mb-4 p-4 {$isDarkMode 
                            ? (mainState.lose ? 'bg-gradient-to-r from-red-600 to-red-800 border-red-800' : mainState.win ? 'bg-gradient-to-r from-green-600 to-green-800 border-green-800' : 'bg-gradient-to-r from-indigo-600 to-purple-800 border-indigo-800') 
                            : (mainState.lose ? 'bg-gradient-to-r from-red-50 to-red-100 border-red-200' : mainState.win ? 'bg-gradient-to-r from-green-50 to-green-100 border-green-200' : 'bg-gradient-to-r from-indigo-50 to-purple-50 border-indigo-200')} rounded-lg border {$isDarkMode 
                            ? (mainState.lose ? 'border-red-800' : mainState.win ? 'border-green-800' : 'border-indigo-800') 
                            : (mainState.lose ? 'border-red-200' : mainState.win ? 'border-green-200' : 'border-indigo-200')}"
                    >
                        <div class="flex items-center justify-between">
                            <div class="flex items-center space-x-2">
                                <Info class={$isDarkMode ? 'text-white' : (mainState.lose ? 'text-red-400' : mainState.win ? 'text-green-400' : 'text-indigo-400')} />
                                <span class="text-sm {$isDarkMode ? 'text-white' : (mainState.lose ? 'text-red-400' : mainState.win ? 'text-green-400' : 'text-indigo-400')}"
                                    data-testid="hint-text"
                                    >{mainState.lose ? 'needed a hint?' : mainState.win ? 'needed a hint?' : 'need a hint?'}</span
                                >
                            </div>
                            <Button
                                onclick={() => goto("/purchase")}
                                variant="secondary"
                                size="sm"
                                class="{mainState.lose ? 'bg-red-600 hover:bg-red-700' : mainState.win ? 'bg-green-600 hover:bg-green-700' : 'bg-indigo-600 hover:bg-indigo-700'} text-white"
                                data-testid="purchase-button"
                            >
                                purchase visual clues
                            </Button>
                        </div>
                    </div>
                {/if}
            {:else if mainState.visualClueCount !== -1}
                <div
                    class="mb-4 p-4 {$isDarkMode 
                        ? (mainState.lose ? 'bg-gradient-to-r from-red-600 to-red-800 border-red-800' : mainState.win ? 'bg-gradient-to-r from-green-600 to-green-800 border-green-800' : 'bg-gradient-to-r from-indigo-600 to-purple-800 border-indigo-800') 
                        : (mainState.lose ? 'bg-gradient-to-r from-red-50 to-red-100 border-red-200' : mainState.win ? 'bg-gradient-to-r from-green-50 to-green-100 border-green-200' : 'bg-gradient-to-r from-indigo-50 to-purple-50 border-indigo-200')} rounded-lg border {$isDarkMode 
                        ? (mainState.lose ? 'border-red-800' : mainState.win ? 'border-green-800' : 'border-indigo-800') 
                        : (mainState.lose ? 'border-red-200' : mainState.win ? 'border-green-200' : 'border-indigo-200')}"
                >
                    <div class="flex items-center justify-between">
                        <div class="flex items-center space-x-2">
                            <Info class={$isDarkMode ? 'text-white' : (mainState.lose ? 'text-red-400' : mainState.win ? 'text-green-400' : 'text-indigo-400')} />
                            <span class="text-sm {$isDarkMode ? 'text-white' : 'text-gray-700'}" data-testid="visualcluesremaining-text">
                                visual clues remaining: {mainState.visualClueCount}
                            </span>
                        </div>
                    </div>
                </div>
            {/if}
        {/if}

        {#if $guessStore.guesses.length > 0}
            <HintsDisplay 
                gameOver={mainState.win || mainState.lose} 
                movieTitle={mainState.win ? $guessStore.guesses.find(g => g.win)?.title : mainState.answer?.title}
                isLoss={mainState.lose}
            />
        {/if}

        <Dialog open={mainState.errorOpen} title="uh-oh!" id="error" confirmButton="ok">
            {mainState.errorMessage}
        </Dialog>

        <Dialog open={mainState.shareOpen} title="your results" id="results" confirmButton="close" cancelButton="share" cancelCallback={() => deviceShare()}>
            {#each mainState.shareData as line}
                <div class="leading-none">{line}</div>
            {/each}
        </Dialog>

        <Dialog open={mainState.visualClueOpen} title="visual clue" id="visualclue" confirmButton="ok">
            <VisualClue />
        </Dialog>

        <Dialog open={mainState.answerOpen} title="the answer is..." id="answer" confirmButton="ok">
            {#if mainState.answer !== null}
                <Guess props={mainState.answer} />
            {:else}
                unable to pull mainState.answer from server
            {/if}
        </Dialog>

        <div class="guesses z-10">
            {#each [...$guessStore.guesses].filter(g => !g.win).reverse() as guess, i (guess)}
                <div animate:flip={{ duration: 1000 }}>
                    <Guess props={guess} index={i} />
                </div>
            {/each}
        </div>
    {:else if mainState.loading && !mainState.serverDown}
        <div class="min-w-3xl w-full">
            <Skeleton class="h-12 w-full mb-4" />
        </div>
        <div class="grid grid-cols-2 gap-4">
            {#each [1, 2, 3, 4] as _}
                <Skeleton class="flex-grow h-24 w-full rounded-rect" />
            {/each}
        </div>
    {/if}

    {#if mainState.serverDown}
        <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="mainState.serverDown-text">
            server down. please try again later
        </h2>
    {/if}
    {#if $guessStore.guesses.length > 0}
        <BuyMeAPizza />
    {/if}
</PageWrapper>
