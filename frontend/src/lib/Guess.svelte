<script lang="ts">
    import Card from "./Card.svelte";
    import type { GuessDomain } from "./domain";

    export let props: GuessDomain = {
        title: "Unknown Movie",
        cards: [],
    };
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
    <hr class="my-12 h-0.5 border-t-0 bg-gray-600 dark:bg-white/10" />
</div>
