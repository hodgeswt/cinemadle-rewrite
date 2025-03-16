<script lang="ts">
    import Guess from "$lib/Guess.svelte";
    import { Input } from "$lib/components/ui/input";
    import { Search } from "@lucide/svelte";
    import { Button } from "$lib/components/ui/button";
    import { match } from "$lib/result";
    import { get, getPossibleMovies } from "$lib/middleware";
    import { type GuessDomain } from "$lib/domain";
    import { GuessDtoToDomain } from "$lib/mappers";
    import { onMount } from "svelte";
    import { find } from "$lib/fuzzy";

    let guessValue = $state("");
    let errorMessage = $state("");
    let guesses = $state([] as GuessDomain[]);

    let possibleGuesses = $state([] as string[]);
    let filteredGuesses = $derived(find(guessValue, possibleGuesses).filter((x, i) => i < 10));
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

    async function handleGuess(event: Event): Promise<void> {
        event.preventDefault();

        let result = await get("/guess/movie/2025-03-08/1825", null);

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
</script>

<div class="bg-red-500 p-4 flex justify-center min-h-screen">
    <div class="w-1/2 bg-blue-500 p-4">
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
            <div class="mt-1 bg-white border border-gray-300 rounded shadow-lg z-10">
                {#each filteredGuesses as possibleGuess}
                    <p>{possibleGuess}</p>
                {/each}
            </div>
        {/if}

        {#if errorMessage !== ""}
            <div class="text-red">
                {errorMessage}
            </div>
        {/if}

        <div class="guesses z-10">
            {#each guesses as guess}
                <Guess props={guess} />
            {/each}
        </div>
    </div>
</div>
