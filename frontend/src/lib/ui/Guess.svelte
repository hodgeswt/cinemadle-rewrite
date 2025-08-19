<script lang="ts">
    import { onMount } from "svelte";
    import Card from "./Card.svelte";
    import type { GuessDomain } from "../domain";
    import Logger from "../logger";
    import Divider from "./Divider.svelte";

    export let props: GuessDomain = {
         title: "Unknown Movie",
         cards: [],
         win: false
     };

     export let index: number = -1;

     onMount(() => {
        Logger.log("Guess.svelte.onMount(): Mounting with {0}", props);
     })
</script>

<div class="min-w-3xl w-full">
    <h1 class="m-4 text-3xl font-extrabold leading-none tracking-tight" data-testid={`guess-${index}-title`}>
        {props.title}
    </h1>
    <div class="grid grid-cols-2">
        {#each props.cards as card}
            <Card props={card} index={index}/>
        {/each}
    </div>
    <Divider />
</div>
