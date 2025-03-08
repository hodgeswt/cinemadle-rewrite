<script lang="ts">
    import type { GuessDomain } from "./domain";

    export let props: GuessDomain = {
        title: "Unknown Movie",
        cards: [],
    };
</script>

<div class="card">
    <div class="title">{props.title}</div>
    <div class="grid">
        {#each props.cards as { color, data }}
            <div class="square {color}">
                {#each data as datum}
                    <div>{datum}</div>
                {/each}
            </div>
        {/each}
    </div>
</div>

<style>
    .card {
        border: 1px solid #ccc;
        width: fit-content;
        border-radius: 8px;
        padding: 10px;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .title {
        font-size: 16px;
        font-weight: bold;
        margin-bottom: 10px;
        text-align: center;
    }

    .grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 10px;
    }

    .square {
        width: 90px;
        height: 90px;
        margin: 5px;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        border-radius: 4px;
        color: #333;
        font-size: 12px;
        text-align: center;
    }

    .grey {
        background-color: #ccc;
    }

    .yellow {
        background-color: #ffeb3b;
    }

    .green {
        background-color: #4caf50;
    }
</style>
