<script lang="ts">
    import type { CardDomain } from "./domain";

    import { ArrowUp } from "@lucide/svelte";
    import { ArrowDown } from "@lucide/svelte";
    import { Sparkles } from "@lucide/svelte";

    export let props: CardDomain = {
        color: "gray",
        title: "Unknown Movie",
        data: [],
        direction: 0,
        modifiers: {},
    };
</script>

<div class="p-2 h-full">
    <div
        class={`p-4 flex flex-col h-full rounded-lg shadow-md bg-${props.color}-300`}
    >
        <!-- TODO: Fix hardcoded numbers -->
        <h2 class="text-lg font-bold mb-2 flex">
            {props.title}
            {#if props.direction === 2}
                <ArrowUp class="ml-4" /><ArrowUp />15
            {:else if props.direction === 1}
                <ArrowUp class="ml-4" />10
            {:else if props.direction === -1}10
                <ArrowDown class="ml-4" />
            {:else if props.direction === -2}15
                <ArrowDown class="ml-4" /><ArrowDown />
            {/if}
        </h2>
        <ul class="list-none list-inside flex-grow">
            {#each props.data as datum}
                {#if props.modifiers[datum]?.includes("bold") === true}
                    <li class="text-black flex items-center">{datum} <Sparkles class="scale-75" /></li>
                {:else}
                    <li class="text-black">{datum}</li>
                {/if}
            {/each}
        </ul>
    </div>
</div>
