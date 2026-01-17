<script lang="ts">
    import { onMount } from "svelte";
    import Card from "./Card.svelte";
    import type { GuessDomain } from "../domain";
    import Logger from "../logger";

    export let props: GuessDomain = {
         title: "unknown movie",
         cards: [],
         win: false
     };

     export let index: number = -1;

     onMount(() => {
        Logger.log("Guess.svelte.onMount(): Mounting with {0}", props);
     })
</script>

<div class="min-w-3xl w-full pb-4">
    <h1 class="m-4 text-3xl font-extrabold leading-none tracking-tight" data-testid={`guess-${index}-title`}>
        {props.title}
    </h1>
    <div class="grid grid-cols-2">
        {#each props.cards as card, i}
            <Card props={card} index={i} guessIndex={index} />
        {/each}
    </div>
</div>
