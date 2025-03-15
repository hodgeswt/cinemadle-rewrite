<script lang="ts">
    import Guess from "$lib/Guess.svelte";
    import { match } from "$lib/result";
    import { get, getPossibleMovies } from "$lib/middleware";
    import { type GuessDomain } from "$lib/domain";
    import { GuessDtoToDomain } from "$lib/mappers";
    import { onMount } from "svelte";

    let guessValue = $state("");
    let errorMessage = $state("");
    let guesses = $state([] as GuessDomain[]);

    let possibleGuesses = $state([] as string[]);

    onMount(async () => {
        try {
            const result = await getPossibleMovies();

            if (result.ok) {
                possibleGuesses = result.data!
            }
        } catch (e) {
            console.error(e);
        }
    });

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

<form onsubmit={handleGuess}>
    <input type="text" bind:value={guessValue} placeholder="Guess..." />
    <button type="submit">Submit</button>
</form>

{#if errorMessage !== ""}
    <div class={"red"}>
        {errorMessage}
    </div>
{/if}

<div class="guesses">
    {#each guesses as guess}
        <Guess props={guess} />
    {/each}
</div>

<style>
    .red {
        color: red;
    }
</style>
