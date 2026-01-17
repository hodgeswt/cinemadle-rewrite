<script lang="ts">
    import type { CardDomain } from "../domain";

    import { Info } from "@lucide/svelte";
    import { isDarkMode } from "$lib/stores/theme";

    const {props = {
        color: "gray",
        title: "unknown category",
        data: [],
        direction: 0,
        modifiers: {},
    }, index = -1, guessIndex= -1} = $props();

    const darkModeMap: { [key: string]: string } = {
        'gray': 'bg-[rgba(255,255,255,0.08)]',
        'yellow': 'bg-gradient-to-br from-[#ffeb3b] to-[#ffd700]',
        'green': 'bg-gradient-to-br from-[#00ff88] to-[#00ffcc]'
    }

    const lightModeMap: { [key: string]: string } = {
        'gray': 'bg-gradient-to-br from-gray-200 to-gray-300',
        'yellow': 'bg-gradient-to-br from-[#ffeb3b] to-[#ffd700]',
        'green': 'bg-gradient-to-br from-[#00ff88] to-[#00ffcc]'
    }

    let bg: string = $state("");
    $effect(() => {
        bg = $isDarkMode ? darkModeMap[props.color] : lightModeMap[props.color];
    })

    function mapTitle(x: string): string {
        let y = x;
        if (x === "boxOffice") {
            y = "box office";
        }

        return y.toUpperCase();
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

    function getHintText(): string | null {
        const hints = props.hints;
        if (!hints) return null;

        // For known values (genre, cast, creatives)
        if (hints.knownValues && hints.knownValues.length > 0) {
            return `Known: ${hints.knownValues.join(", ")}`;
        }

        // For possible values (rating)
        if (hints.possibleValues && hints.possibleValues.length > 0) {
            return `Possible: ${hints.possibleValues.join(", ")}`;
        }

        // For range-based hints (year, box office) - only show if at least one bound is set
        if (hints.min || hints.max) {
            const min = hints.min ? formatNumber(hints.min) : "?";
            const max = hints.max ? formatNumber(hints.max) : "?";
            return `Range: ${min} – ${max}`;
        }

        return null;
    }
</script>

<div class="p-2 h-full">
    <div
        class={`p-4 flex flex-col h-full rounded-lg shadow-md overflow-hidden ${bg} 
            backdrop-blur-md bg-opacity-70 border border-white/40`
        }
    >
        <h2 class="text-lg font-bold mb-2">
            <span data-testid={`card-${guessIndex}-${index}-title-text`} class={`${$isDarkMode ? 'text-gray-700' : 'text-gray-500'} text-sm`}>{mapTitle(props.title)}</span>
        </h2>
        <ul class="list-none list-inside flex-grow overflow-y-auto">
            {#each props.data as datum, i}
                <li class="{$isDarkMode ? 'text-gray-500' : 'text-black'} text-xl break-words" data-testid={`card-${guessIndex}-${index}-tiledata-${i}`}>{formatNumber(datum)}</li>
            {/each}
        </ul>
        {#if getHintText()}
            <div class="mt-2 pt-2 border-t border-white/20 flex items-center gap-1" data-testid={`card-${guessIndex}-${index}-hints`}>
                <Info class="w-3 h-3 flex-shrink-0 {$isDarkMode ? 'text-gray-600' : 'text-gray-500'}" />
                <span class="text-xs {$isDarkMode ? 'text-gray-600' : 'text-gray-500'} truncate">{getHintText()}</span>
            </div>
        {/if}
    </div>
</div>
