<script lang="ts">
    import Guess from "$lib/Guess.svelte";
    import { Input } from "$lib/components/ui/input";
    import { Search } from "@lucide/svelte";
    import { Button } from "$lib/components/ui/button";
    import { match } from "$lib/result";
    import {
        get,
        getPossibleMovies,
        loadPreviousGuesses,
        ping,
    } from "$lib/middleware";
    import { type GuessDomain, type PossibleMediaDomain } from "$lib/domain";
    import { GuessDtoToDomain } from "$lib/mappers";
    import { onMount } from "svelte";
    import { find } from "$lib/fuzzy";
    import { isoDateNoTime } from "$lib/util";
    import { browser } from "$app/environment";
    import { v4 as uuidv4 } from "uuid";
    import { Skeleton } from "$lib/components/ui/skeleton";

    let guessValue = $state("");
    let errorMessage = $state("");
    let guesses = $state([] as GuessDomain[]);

    let searchOpen = $state(false);

    let possibleGuesses = $state({} as PossibleMediaDomain);
    let filteredGuesses = $derived(
        find(guessValue, Object.keys(possibleGuesses)).filter((_, i) => i < 10),
    );

    let uid = $state(
        (browser ? localStorage.getItem("cinemadleUuid") : null) || "",
    );

    let win = $derived(guesses.filter((x) => x.win).length > 0);

    let done = $derived(guesses.length >= 10 || win);

    let loading = $state(true);
    let healthPing = $state(0);

    let serverDown = $state(false);

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

            const prev = await loadPreviousGuesses(uid);

            if (prev.ok) {
                for (const id of prev.data!) {
                    await makeGuess(id, true);
                }
            } else {
                throw new Error(prev.error!);
            }

            loading = false;
        } catch (e) {
            console.error(e);
        }

        if (browser && (!uid || uid === "")) {
            uid = uuidv4();
            console.log(uid);
            localStorage.setItem("cinemadleUuid", uid);
        }
    });

    function guessChange(_event: Event): void {
        console.log(filteredGuesses);
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

        let result = await get(
            `/guess/movie/${isoDateNoTime()}/${id}`,
            null,
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
                } else {
                    errorMessage = "Invalid response from server";
                }
            },
            () => {
                errorMessage = result.error as string;
            },
        );
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
        <h1 class="m-4 text-4xl font-extrabold leading-none tracking-tight">
            cinemadle
        </h1>
        <h2 class="m-4 text-2xl font-semibold leading-non tracking-tight">
            {isoDateNoTime()}
        </h2>
        {#if win}
            <h2 class="m-4 text-3xl font-semibold text-green-400 leading-non tracking-tight">
                You win!
            </h2>
        {/if}
        {#if !loading && !serverDown}
            <div class="flex">
                <Input
                    type="text"
                    placeholder="Guess..."
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

            {#if errorMessage !== ""}
                <p class="text-red">
                    {errorMessage}
                </p>
            {/if}

            <div class="guesses z-10">
                {#each [...guesses].reverse() as guess}
                    <Guess props={guess} />
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
