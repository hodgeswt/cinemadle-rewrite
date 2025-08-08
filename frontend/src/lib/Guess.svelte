<script lang="ts">
    import { onMount } from "svelte";
    import Card from "./Card.svelte";
    import type { GuessDomain } from "./domain";
    import Logger from "./logger";

    export let props: GuessDomain = {
         title: "Unknown Movie",
         cards: [],
         win: false
     };

     onMount(() => {
        Logger.log("Guess.svelte.onMount(): Mounting with {0}", props);
     })
</script>

<div class="min-w-3xl w-full">
    <h1 class="m-4 text-3xl font-extrabold leading-none tracking-tight">
        {props.title}
    </h1>
    <div class="grid grid-cols-2">
        {#each props.cards as card}
            <Card props={card} />
        {/each}
    </div>
    <div class="flex justify-center items-center">
        <hr class="my-12 h-0.5 w-1/2 border-t-0 bg-gray-300 dark:bg-white/10" />
    </div>
</div>
