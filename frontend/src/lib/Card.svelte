<script lang="ts">
    import type { CardDomain } from "./domain";

    export let props: CardDomain = {
        color: "gray",
        title: "Unknown Movie",
        data: [],
    };

    console.log(props.color);
</script>

<div class="p-2 h-full">
    <div class={`p-4 flex flex-col h-full rounded-lg shadow-md bg-${props.color}-300`}>
        <h2 class="text-lg font-bold mb-2">{props.title}</h2>
        <ul class="list-none list-inside flex-grow">
            {#each props.data as datum}
                <li class="text-black">{datum}</li>
            {/each}
        </ul>
    </div>
</div>
