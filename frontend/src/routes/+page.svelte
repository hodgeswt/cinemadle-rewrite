<script lang="ts">
    import Guess from "$lib/Guess.svelte";
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
    } from "$lib/middleware";
    import { type GuessDomain, type PossibleMediaDomain } from "$lib/domain";
    import { GuessDtoToDomain } from "$lib/mappers";
    import { onMount, untrack } from "svelte";
    import { find } from "$lib/fuzzy";
    import { isoDateNoTime } from "$lib/util";
    import { browser } from "$app/environment";
    import { v4 as uuidv4 } from "uuid";
    import { Skeleton } from "$lib/components/ui/skeleton";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import { writable } from "svelte/store";

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

    let uid = $state(
        (browser ? localStorage.getItem("cinemadleUuid") : null) || "",
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
                let a = await getAnswer(uid);

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

            const result = await getPossibleMovies(uid);

            if (result.ok) {
                possibleGuesses = result.data!;
            } else {
                throw new Error(result.error!);
            }

           /* const prev = await loadPreviousGuesses(uid);

            if (prev.ok) {
                for (const id of prev.data!) {
                    await makeGuess(id, true);
                }
            } else {
                throw new Error(prev.error!);
            }*/

            loading = false;
        } catch (e) {
            errorMessage = "Unable to contact server.";
            openError.set(true);
        }

        if (browser && (!uid || uid === "")) {
            uid = uuidv4();
            localStorage.setItem("cinemadleUuid", uid);
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

    function deviceShare(_event: Event): void {
        if (navigator.share) {
            navigator.share({
                title: "cinemadle",
                text: shareData.join("\n"),
                url: "http://cinemadle.hodgeswill.com",
            });
        } else {
            openShare.set(false);
            shareData = [] as string[];

            errorMessage = "Share permissions not provided.";
            openError.set(true);
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
                  (x) => possibleGuesses[x] === guess,
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
            uid,
            skip ? { "x-duplicate": "true" } : null,
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
    <div class="w-full lg:w-1/2 md:w-1/2 sm:w-full p-4">
        <div class="w-full flex justify-between items-center">
            <h1
                class="flex-1 m-4 text-4xl font-extrabold leading-none tracking-tight"
            >
                cinemadle
            </h1>
            <div
                class="w-full flex-1 flex flex-col m-4 text-right justify-center"
            >
                <a href="/about" class="underline">About</a>
            </div>
        </div>

        <h2 class="m-4 text-2xl font-semibold leading-non tracking-tight">
            {isoDateNoTime()}
        </h2>
        {#if win}
            <div class="flex items-center">
                <h2
                    class="m-4 text-3xl font-semibold text-green-400 leading-non tracking-tight"
                >
                    you win!
                </h2>
                <Button class="bg-green-400" on:click={showShareSheet}>
                    share
                </Button>
            </div>
        {/if}
        {#if lose}
            <div class="flex items-center">
                <h2
                    class="m-4 text-3xl font-semibold text-red-400 leading-non tracking-tight"
                >
                    better luck next time!
                </h2>
                <Button class="bg-red-400" on:click={showShareSheet}>
                    share
                </Button>
                <Button class="bg-red-400 ml-4" on:click={showAnswerButton}>
                    see answer
                </Button>
            </div>
        {/if}

        {#if !loading && !serverDown}
            {#if remaining > 0}
                <div class="flex">
                    <Input
                        type="text"
                        placeholder={`Guess... ${remaining} remaining`}
                        bind:value={guessValue}
                        onchange={guessChange}
                        class="m-1"
                        disabled={done}
                    />

                    <Button
                        type="submit"
                        size="icon"
                        onclick={handleGuess}
                        class="m-1"
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
                    <AlertDialog.Title>Your Results</AlertDialog.Title>
                    <AlertDialog.Description>
                        {#each shareData as line}
                            <p class="m-0">{line}</p>
                        {/each}
                    </AlertDialog.Description>
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
                        <AlertDialog.Action on:click={closeShare} class="m-2">
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
            <h2 class="m-4 text-2xl font-semibold leading-non tracking-tight">
                Server down. Please try again later
            </h2>
        {/if}
    </div>
</div>
