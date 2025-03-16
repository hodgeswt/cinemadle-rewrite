<script lang="ts">
    import * as Card from "$lib/components/ui/card";
    import type { GuessDomain } from "./domain";

    export let props: GuessDomain = {
        title: "Unknown Movie",
        cards: [],
    };
</script>

<div class="bg-gray-400 min-w-3xl w-full">
    <div class="h-1 m-8">
        {props.title}
    </div>
    <div class="grid grid-cols-2">
        {#each props.cards as { color, data }}
            <div class={`min-w-3xl w-1/2 h-1/2 bg-${color}-400`}>
                {#each data as datum}
                    <p>{datum}</p>
                {/each}
            </div>
        {/each}
    </div>
</div>
