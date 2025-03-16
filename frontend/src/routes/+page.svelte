<script lang="ts">
    import Guess from "$lib/Guess.svelte";
    import { Input } from "$lib/components/ui/input";
    import { Search } from "@lucide/svelte";
    import { Button } from "$lib/components/ui/button";
    import { match } from "$lib/result";
    import { get, getPossibleMovies } from "$lib/middleware";
    import { type GuessDomain, type PossibleMediaDomain } from "$lib/domain";
    import { GuessDtoToDomain } from "$lib/mappers";
    import { onMount } from "svelte";
    import { find } from "$lib/fuzzy";

    let guessValue = $state("");
    let errorMessage = $state("");
    let guesses = $state([] as GuessDomain[]);

    let possibleGuesses = $state({} as PossibleMediaDomain);
    let filteredGuesses = $derived(
        find(guessValue, Object.keys(possibleGuesses)).filter((_, i) => i < 10),
    );
    let searchOpen = $state(false);

    onMount(async () => {
        try {
            const result = await getPossibleMovies();

            if (result.ok) {
                possibleGuesses = result.data!;
            }
        } catch (e) {
            console.error(e);
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

    async function makeGuess(guess: string): Promise<void> {
        const id = possibleGuesses[guess]
        let result = await get(
            `/guess/movie/${new Date().toISOString().split("T")[0]}/${id}`,
            null,
        );
        match(
            result,
            () => {
                let dto = JSON.parse(result.data as string);
                let domain = GuessDtoToDomain(dto);

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
    <div class="w-1/2 p-4">
        <div class="flex">
            <Input
                type="text"
                placeholder="Guess..."
                bind:value={guessValue}
                onchange={guessChange}
                class="m-1"
            />

            <Button type="submit" size="icon" onclick={handleGuess} class="m-1">
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
            {#each guesses as guess}
                <Guess props={guess} />
            {/each}
        </div>
    </div>
</div>
