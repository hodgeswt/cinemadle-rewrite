<script lang="ts">
    import Guess from "$lib/Guess.svelte";
    import BuyMeAPizza from "$lib/BuyMeAPizza.svelte";
    import { Input } from "$lib/components/ui/input";
    import { Search } from "@lucide/svelte";
    import { Button } from "$lib/components/ui/button";
    import { flip } from "svelte/animate";
    import { ping, getAnswer, validateAndRefreshToken } from "$lib/middleware";
    import { type GuessDomain, type PossibleMediaDomain } from "$lib/domain";
    import { onMount, untrack } from "svelte";
    import { find } from "$lib/fuzzy";
    import { isoDateNoTime } from "$lib/util";
    import { Skeleton } from "$lib/components/ui/skeleton";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import { writable } from "svelte/store";
    import { userStore } from "$lib/stores";
    import { toast } from "svelte-sonner";
    import type { LoginDto } from "$lib/dto";
    import { Container, type IGuessService } from "$lib/services";
    import Logger from "$lib/logger";

    let guessValue = $state("");
    let errorMessage = $state("");
    let shareData = $state([] as string[]);

    let openError = writable(false);
    let openShare = writable(false);
    let showAnswer = writable(false);
    let guesses = $state([] as GuessDomain[]);

    let searchOpen = $state(false);

    let titles = $derived(guesses.map((x) => x.title));

    let guessService = (): IGuessService => Container.it().GuessService;

    let possibleGuesses = $state({} as PossibleMediaDomain);

    let filteredGuesses = $derived(
        find(guessValue, Object.keys(possibleGuesses))
            .filter((_, i) => i < 10)
            .filter((x) => !titles.includes(x)),
    );

    const LIMIT = 10;
    const PING_LIMIT = 10;

    let remaining = $derived(LIMIT - guesses.length);
    let win = $derived(guesses.filter((x) => x.win).length > 0);
    let lose = $derived(guesses.length >= LIMIT && !win);

    let done = $derived(guesses.length >= LIMIT && win);

    let loading = $state(true);
    let healthPing = $state(0);
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
            let alive = await ping();

            while (!alive) {
                if (healthPing === PING_LIMIT) {
                    serverDown = true;
                    return;
                }
                alive = await ping();
                healthPing += 1;
                await new Promise((x) => setTimeout(x, 1000));
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

    function showShareSheet(_event: Event): void {
        shareData = guesses.map((x) =>
            x.cards
                .map((y) => {
                    switch (y.color) {
                        case "green":
                            return "🟩";
                        case "yellow":
                            return "🟨";
                        default:
                            return "⬛";
                    }
                })
                .join(""),
        );
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

<div class="p-4 flex justify-center min-h-screen">
    <div class="w-full lg:w-1/2 md:w-1/2 sm:w-full">
        <div class="w-full flex justify-between items-center mb-4">
            <h1
                class="flex-1 text-4xl font-extrabold leading-none tracking-tight"
            >
                cinemadle
            </h1>
            <div class="flex-1 flex flex-col text-right justify-center">
                {#if !$userStore.loggedIn}
                    <a href="/login" class="underline">log in</a>
                    <a href="/signup" class="underline">sign up</a>
                {/if}
                <a href="/about" class="underline">about</a>
                <a href="/devinfo" class="underline">dev info</a>
            </div>
        </div>
        <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
            {isoDateNoTime()}
        </h2>
        {#if win}
            <div class="flex items-center mb-4">
                <h2
                    class="flex-1 text-3xl font-semibold text-green-400 leading-none tracking-tight"
                >
                    you win!
                </h2>
                <Button class="bg-green-400" on:click={showShareSheet}>
                    share
                </Button>
            </div>
        {/if}
        {#if lose}
            <div class="flex items-center mb-4">
                <h2
                    class="flex-1 text-3xl font-semibold text-red-400 leading-none tracking-tight"
                >
                    better luck next time!
                </h2>
                <Button class="bg-red-400" on:click={showShareSheet}>
                    share
                </Button>
                <Button class="bg-red-400 ml-2" on:click={showAnswerButton}>
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
                    />

                    <Button
                        type="submit"
                        size="icon"
                        onclick={handleGuess}
                        disabled={done || guessValue.trim() === ""}
                    >
                        <Search />
                    </Button>
                </div>
            {/if}

            {#if filteredGuesses.length > 0}
                <ul
                    class="mt-1 bg-white border border-gray-300 rounded shadow-xl absolute"
                >
                    {#each filteredGuesses as possibleGuess}
                        <li class="p-2 text-lg">
                            <button
                                onclick={() => {
                                    handleSelect(possibleGuess);
                                }}
                            >
                                {possibleGuess}
                            </button>
                        </li>
                    {/each}
                </ul>
            {/if}

            <AlertDialog.Root bind:open={$openError}>
                <AlertDialog.Content>
                    <AlertDialog.Title>Uh-oh!</AlertDialog.Title>
                    <AlertDialog.Description>
                        {errorMessage}
                    </AlertDialog.Description>
                    <AlertDialog.Footer>
                        <AlertDialog.Action on:click={closeDialog}>
                            Ok
                        </AlertDialog.Action>
                    </AlertDialog.Footer>
                </AlertDialog.Content>
            </AlertDialog.Root>

            <AlertDialog.Root bind:open={$openShare}>
                <AlertDialog.Content>
                    <AlertDialog.Title>your results</AlertDialog.Title>
                    <AlertDialog.Description>
                        {#each shareData as line}
                            <p class="m-0 p-0">{line}</p>
                        {/each}
                        <p>cinemadle {isoDateNoTime()}</p>
                        <p>
                            play at <a href="cinemadle.com" class="underline"
                                >cinemadle.com</a
                            >
                        </p></AlertDialog.Description
                    >
                    <AlertDialog.Footer>
                        <AlertDialog.Action on:click={closeShare} class="m-2">
                            Close
                        </AlertDialog.Action>
                        <AlertDialog.Action on:click={deviceShare} class="m-2">
                            Share
                        </AlertDialog.Action>
                    </AlertDialog.Footer>
                </AlertDialog.Content>
            </AlertDialog.Root>

            <AlertDialog.Root bind:open={$showAnswer}>
                <AlertDialog.Content>
                    <AlertDialog.Title>The answer is...</AlertDialog.Title>
                    <AlertDialog.Description>
                        {#if answer !== null}
                            <Guess props={answer} />
                        {:else}
                            Unable to pull answer from server
                        {/if}
                    </AlertDialog.Description>
                    <AlertDialog.Footer>
                        <AlertDialog.Action on:click={closeAnswer} class="m-2">
                            Close
                        </AlertDialog.Action>
                    </AlertDialog.Footer>
                </AlertDialog.Content>
            </AlertDialog.Root>

            {#if !$userStore.loggedIn}
                <p>
                    cinemadle is better when you <a
                        href="/login"
                        class="underline">log in</a
                    >
                </p>
            {/if}

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
            <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
                Server down. Please try again later
            </h2>
        {/if}
        {#if guesses.length > 0}
            <BuyMeAPizza />
        {/if}
    </div>
</div>
