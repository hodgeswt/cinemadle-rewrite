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

    function mapTitle(x: string): string {
        if (x === "boxOffice") {
            return "box office";
        }

        return x;
    }

    function formatNumber(x: string | number): string {
        let n = 0;
        if (typeof x === "string") {
            const r = /^\d+$/;
            if (!x.match(r)) {
                return x;
            }

            n = +x;
            if (isNaN(n)) {
                n = 0;
            }
        } else {
            n = x;
        }

        if (n <= 9999) {
            return n.toString();
        }

        const formatter = new Intl.NumberFormat("en-US", {
            style: "currency",
            currency: "USD",
            notation: "compact",
            minimumFractionDigits: 0,
            maximumFractionDigits: 0,
        });
        return formatter.format(n);
    }

    function getNumber(): number | string {
        if (props.title === "year") {
            return 10;
        }

        if (props.title === "boxOffice") {
            return formatNumber(100_000_000);
        }

        return 0;
    }
</script>

<div class="p-2 h-full">
    <div
        class={`p-4 flex flex-col h-full rounded-lg shadow-md bg-${props.color}-300`}
    >
        <h2 class="text-lg font-bold mb-2 flex">
            {mapTitle(props.title)}
            {#if props.direction === 2}
                <ArrowUp class="ml-4" /><ArrowUp />
                &gt;{getNumber()}
            {:else if props.direction === 1}
                <ArrowUp class="ml-4" />
                &le;{getNumber()}
            {:else if props.direction === -1}
                <ArrowDown class="ml-4" />
                &le;{getNumber()}
            {:else if props.direction === -2}
                <ArrowDown class="ml-4" /><ArrowDown />
                &gt;{getNumber()}
            {/if}
        </h2>
        <ul class="list-none list-inside flex-grow">
            {#each props.data as datum}
                {#if props.modifiers[datum]?.includes("bold") === true}
                    <li class="text-black flex items-center">
                        {formatNumber(datum)}
                        <Sparkles class="scale-75" />
                    </li>
                {:else}
                    <li class="text-black">{formatNumber(datum)}</li>
                {/if}
            {/each}
        </ul>
    </div>
</div>
