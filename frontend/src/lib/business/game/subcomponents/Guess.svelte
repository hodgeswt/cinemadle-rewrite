<script lang="ts">
    import Card from "$lib/ui/Card.svelte";
    import { getGameState } from "../Game.state.svelte";

    let { index, gameId } = $props();
    let game = getGameState(gameId);

    let guess = $derived(game.noWinGuesses.at(index));
</script>

<div class="w-full max-w-3xl pb-4">
    <h1 class="m-4 text-2xl sm:text-3xl font-extrabold leading-none tracking-tight" data-testid={`guess-${index}-title`}>
        {guess?.title ?? "unknown movie"}
    </h1>
    <div class="grid grid-cols-2 gap-1 sm:gap-0">
        {#each (guess?.cards ?? []) as card, i}
            <Card props={card} index={i} guessIndex={index} />
        {/each}
    </div>
</div>
