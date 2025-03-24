<script lang="ts">
    import type { CardDomain } from "./domain";

    import { ArrowUp } from "@lucide/svelte";
    import { ArrowDown } from "@lucide/svelte";

    export let props: CardDomain = {
        color: "gray",
        title: "Unknown Movie",
        data: [],
        direction: 0,
    };
</script>

<div class="p-2 h-full">
    <div
        class={`p-4 flex flex-col h-full rounded-lg shadow-md bg-${props.color}-300`}
    >
        <h2 class="text-lg font-bold mb-2 flex">
            {props.title}
            {#if props.direction === 2}
                <ArrowUp class="ml-4" /><ArrowUp />
            {:else if props.direction === 1}
                <ArrowUp class="ml-4"/>
            {:else if props.direction === -1}
                <ArrowDown class="ml-4"/>
            {:else if props.direction === -2}
                <ArrowDown class="ml-4"/><ArrowDown />
            {/if}
        </h2>
        <ul class="list-none list-inside flex-grow">
            {#each props.data as datum}
                <li class="text-black">{datum}</li>
            {/each}
        </ul>
    </div>
</div>
