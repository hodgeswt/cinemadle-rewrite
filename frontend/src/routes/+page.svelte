<script lang="ts">
    import Guess from "$lib/Guess.svelte";
    import BuyMeAPizza from "$lib/BuyMeAPizza.svelte";
    import { Input } from "$lib/components/ui/input";
    import { Search } from "@lucide/svelte";
    import { Button } from "$lib/components/ui/button";
    import { match } from "$lib/result";
    import { flip } from "svelte/animate";
    import {
        get,
        getPossibleMovies,
        loadPreviousGuesses,
        ping,
        getAnswer,
        validateAndRefreshToken,
    } from "$lib/middleware";
    import { type GuessDomain, type PossibleMediaDomain } from "$lib/domain";
    import { GuessDtoToDomain } from "$lib/mappers";
    import { onMount, untrack } from "svelte";
    import { find } from "$lib/fuzzy";
    import { isoDateNoTime } from "$lib/util";
    import { Skeleton } from "$lib/components/ui/skeleton";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import { writable } from "svelte/store";
    import { userStore } from "$lib/stores";
    import { toast } from "svelte-sonner";
    import type { LoginDto } from "$lib/dto";

    let guessValue = $state("");
    let errorMessage = $state("");
    let shareData = $state([] as string[]);

    let openError = writable(false);
    let openShare = writable(false);
    let showAnswer = writable(false);
    let guesses = $state([] as GuessDomain[]);

    let searchOpen = $state(false);

    let possibleGuesses = $state({} as PossibleMediaDomain);

    let titles = $derived(guesses.map((x) => x.title));

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
    let healthPing = $state(0);

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
                if (healthPing == 10) {
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

            const result = await getPossibleMovies();

            if (result.ok) {
                possibleGuesses = result.data!;
            } else {
                throw new Error(result.error!);
            }

            if ($userStore.loggedIn) {
                const prev = await loadPreviousGuesses($userStore.jwt);

                if (prev.ok) {
                    for (const id of prev.data!) {
                        await makeGuess(id.toString(), true);
                    }
                } else {
                    throw new Error(prev.error!);
                }
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
        if (guessValue !== "") {
            searchOpen = true;
        } else {
            searchOpen = false;
        }
    }

    async function makeGuess(guess: string, skipMap?: boolean): Promise<void> {
        if (done || guess.trim() === "") {
            return;
        }

        const skip = skipMap === true;
        const id = skip ? guess : possibleGuesses[guess];
        const title = skip
            ? (Object.keys(possibleGuesses).find(
                  (x) => possibleGuesses[x].toString() === guess,
              ) ?? "Unknown")
            : guess;

        if (guesses.filter((x) => x.title === title).length != 0) {
            errorMessage = "Movie already guessed!";
            openError.set(true);
            return;
        }

        if (id === undefined) {
            errorMessage = "Unable to make that guess! Try another option";
            openError.set(true);
            return;
        }

        let result = await get(
            `/guess/${id}`,
            { date: isoDateNoTime() },
            { Authorization: $userStore.jwt },
        );

        match(
            result,
            () => {
                let dto = JSON.parse(result.data as string);
                let domain = GuessDtoToDomain(dto, title);

                if (domain.ok) {
                    guesses.push(domain.data as GuessDomain);
                    errorMessage = "";
                    openError.set(false);
                } else {
                    console.log("error");
                    errorMessage = "Invalid response from server";
                    openError.set(true);
                }
            },
            () => {
                errorMessage = "Error making that guess! Try another option";
                openError.set(true);
            },
        );
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
</script>

<div class="p-4 flex justify-center min-h-screen">
    <div class="w-full lg:w-1/2 md:w-1/2 sm:w-full">
        <div class="w-full flex justify-between items-center mb-4">
            <h1
                class="flex-1 text-4xl font-extrabold leading-none tracking-tight"
            >
                cinemadle
            </h1>
            <div
                class="flex-1 flex flex-col text-right justify-center"
            >
                {#if !$userStore.loggedIn}
                    <a href="/login" class="underline">Log In</a>
                    <a href="/signup" class="underline">Sign Up</a>
                {:else}
                    <p>{$userStore.email}</p>
                {/if}
                <a href="/about" class="underline">About</a>
            </div>
        </div>
        {#if $userStore.loggedIn}
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
                            <AlertDialog.Action on:click={closeDialog}
                                >Ok</AlertDialog.Action
                            >
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
                                play at <a
                                    href="cinemadle.com"
                                    class="underline">cinemadle.com</a
                                >
                            </p></AlertDialog.Description
                        >
                        <AlertDialog.Footer>
                            <AlertDialog.Action
                                on:click={closeShare}
                                class="m-2"
                            >
                                Close
                            </AlertDialog.Action>
                            <AlertDialog.Action
                                on:click={deviceShare}
                                class="m-2"
                            >
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
                            <AlertDialog.Action
                                on:click={closeShare}
                                class="m-2"
                            >
                                Close
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
                <h2
                    class="mb-4 text-2xl font-semibold leading-none tracking-tight"
                >
                    Server down. Please try again later
                </h2>
            {/if}
        {:else}
            <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
                cinemadle - the movie guessing game
            </h2>
            <p class="mb-4">
                Please <a href="/login" class="underline">sign in</a> or
                <a href="/signup" class="underline">create an account</a> to play
            </p>
        {/if}
        {#if $userStore.loggedIn && guesses.length > 0}
            <BuyMeAPizza />
        {/if}
    </div>
</div>
