<script lang="ts">
    import type { CardDomain } from "../domain";

    import { ArrowUp } from "@lucide/svelte";
    import { ArrowDown } from "@lucide/svelte";
    import { Sparkles } from "@lucide/svelte";
    import { isDarkMode } from "$lib/stores/theme";

    const {props = {
        color: "gray",
        title: "Unknown Movie",
        data: [],
        direction: 0,
        modifiers: {},
    }, index = -1, guessIndex= -1} = $props();

    const darkModeMap: { [key: string]: string } = {
        'gray': 'bg-[rgba(255,255,255,0.08)]',
        'yellow': 'bg-gradient-to-br from-[#ffd700] to-[#ffeb3b]',
        'green': 'bg-gradient-to-br from-[#00ff88] to-[#00ffcc]'
    }

    let bg: string = $state("");
    $effect(() => {
        bg = $isDarkMode ? darkModeMap[props.color] : `bg-${props.color}-300`;
    })

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
        class={`p-4 flex flex-col h-full rounded-lg shadow-md overflow-hidden ${bg}`}
    >
        <h2 class="text-lg font-bold mb-2 flex sm:flex-row sm:justify-between sm:items-center gap-1">
            <span class="flex items-center flex-shrink min-w-0">
                <span data-testid={`card-${guessIndex}-${index}-title-text`} class={$isDarkMode ? 'text-gray-500' : 'text-black'}>{mapTitle(props.title)}</span>
                {#if props.direction === 2}
                    <span class="flex-shrink-0 ml-2" data-testid={`card-${guessIndex}-${index}-arrowup-1`}>
                        <ArrowUp class="inline w-4 h-4 {$isDarkMode ? 'text-gray-500' : 'text-black'}"/>
                    </span>
                    <span class="flex-shrink-0 ml-2" data-testid={`card-${guessIndex}-${index}-arrowup-2`}>
                        <ArrowUp class="inline w-4 h-4 {$isDarkMode ? 'text-gray-500' : 'text-black'}" />
                    </span>
                {:else if props.direction === 1}
                    <span class="flex-shrink-0 ml-2" data-testid={`card-${guessIndex}-${index}-arrowup-1`}>
                        <ArrowUp class="inline w-4 h-4 {$isDarkMode ? 'text-gray-500' : 'text-black'}" />
                    </span>
                {:else if props.direction === -1}
                    <span class="flex-shrink-0 ml-2" data-testid={`card-${guessIndex}-${index}-arrowdown-1`}>
                        <ArrowDown class="inline w-4 h-4 {$isDarkMode ? 'text-gray-500' : 'text-black'}" />
                    </span>
                {:else if props.direction === -2}
                    <span class="flex-shrink-0 ml-2" data-testid={`card-${guessIndex}-${index}-arrowdown-1`}>
                        <ArrowDown class="inline w-4 h-4 {$isDarkMode ? 'text-gray-500' : 'text-black'}" />
                    </span>
                    <span class="flex-shrink-0 ml-2" data-testid={`card-${guessIndex}-${index}-arrowdown-2`}>
                        <ArrowDown class="inline w-4 h-4 {$isDarkMode ? 'text-gray-500' : 'text-black'}" />
                    </span>
                {/if}
            </span>
            <span class="text-sm font-light flex-shrink-0 ml-2 {$isDarkMode ? 'text-gray-500' : 'text-black'}" data-testid={`card-${guessIndex}-${index}-direction-text`}>
                {#if props.direction === 2 || props.direction === -2}
                    &gt;{getNumber()}
                {:else if props.direction === 1 || props.direction === -1}
                    &le;{getNumber()}
                {/if}
            </span>
        </h2>
        <ul class="list-none list-inside flex-grow overflow-y-auto">
            {#each props.data as datum, i}
                {#if props.modifiers[datum]?.includes("bold") === true}
                    <li class="{$isDarkMode ? 'text-gray-500' : 'text-black'} flex items-center break-words">
                        <span class="truncate flex-grow" data-testid={`card-${guessIndex}-${index}-tiledata-${i}`}>{formatNumber(datum)}</span>
                        <Sparkles class="scale-75 flex-shrink-0 ml-1" data-testid={`card-${guessIndex}-${index}-sparkles-${i}`} />
                    </li>
                {:else}
                    <li class="{$isDarkMode ? 'text-gray-500' : 'text-black'} break-words" data-testid={`card-${guessIndex}-${index}-tiledata-${i}`}>{formatNumber(datum)}</li>
                {/if}
            {/each}
        </ul>
    </div>
</div>
