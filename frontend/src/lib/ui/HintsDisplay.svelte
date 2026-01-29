<script lang="ts">
    import type { Hints } from "$lib/dto";
    import { hintsStore } from "$lib/stores";
    import { isDarkMode } from "$lib/stores/theme";

    // Ordered list of hint categories to match the screenshot layout
    const hintCategories = [
        { key: 'year', label: 'YEAR RANGE' },
        { key: 'rating', label: 'RATING' },
        { key: 'genre', label: 'GENRES' },
        { key: 'cast', label: 'CAST MEMBERS' },
        { key: 'creatives', label: 'CREATIVES' },
        { key: 'boxOffice', label: 'BOX OFFICE RANGE' },
    ];

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

    function getHintContent(hints: Hints | null | undefined): string {
        if (!hints) return "?";

        if (hints.knownValues && hints.knownValues.length > 0) {
            return hints.knownValues.join(", ");
        }
        if (hints.possibleValues && hints.possibleValues.length > 0) {
            return hints.possibleValues.join(", ");
        }
        if (hints.min || hints.max) {
            const min = hints.min ? formatNumber(hints.min) : "?";
            const max = hints.max ? formatNumber(hints.max) : "?";
            return `${min} – ${max}`;
        }
        return "?";
    }

    function getHintsForKey(key: string): Hints | null | undefined {
        return $hintsStore.hints?.[key];
    }
</script>

<div 
    class="mb-4 p-4 md:p-6 rounded-xl border-2 {$isDarkMode 
        ? 'bg-slate-800/50 border-green-500/60' 
        : 'bg-slate-50 border-green-500'}"
    data-testid="hints-display"
>
    <h3 
        class="text-lg md:text-xl font-semibold mb-4 md:mb-6 flex items-center gap-2 md:gap-3 {$isDarkMode ? 'text-white' : 'text-slate-800'}"
        data-testid="hints-display-header"
    >
        You're looking for a movie like...
    </h3>
    
    <div class="grid grid-cols-2 gap-3 md:gap-6">
        {#each hintCategories as category, i}
            {@const hints = getHintsForKey(category.key)}
            {@const content = getHintContent(hints)}
            <div 
                class="space-y-1 md:space-y-2"
                data-testid={`hints-display-card-${category.key}`}
            >
                <div 
                    class="text-xs md:text-xs font-bold tracking-wide {$isDarkMode ? 'text-gray-400' : 'text-gray-500'}"
                    data-testid={`hints-display-card-${category.key}-label`}
                >
                    {category.label}
                </div>
                <div 
                    class="text-lg md:text-lg font-semibold {$isDarkMode ? 'text-green-400' : 'text-green-600'}"
                    data-testid={`hints-display-card-${category.key}-content`}
                >
                    {#if category.key === 'genre' && hints?.knownValues}
                        <div class="flex flex-wrap gap-1 md:gap-2">
                            {#each hints.knownValues as genre}
                                <span class="px-2 md:px-3 py-0.5 md:py-1 rounded-full text-lg md:text-sm {$isDarkMode 
                                    ? 'bg-green-500/20 border border-green-500/40 text-green-300' 
                                    : 'bg-green-100 border border-green-300 text-green-700'}">
                                    {genre}
                                </span>
                            {/each}
                        </div>
                    {:else if category.key === 'cast' && hints?.knownValues}
                        <div class="flex flex-wrap gap-1 md:gap-2">
                            {#each hints.knownValues as actor}
                                <span class="px-2 md:px-3 py-0.5 md:py-1 rounded-full text-lg md:text-sm {$isDarkMode 
                                    ? 'bg-green-500/20 border border-green-500/40 text-green-300' 
                                    : 'bg-green-100 border border-green-300 text-green-700'}">
                                    {actor}
                                </span>
                            {/each}
                        </div>
                    {:else}
                        {content}
                    {/if}
                </div>
            </div>
        {/each}
    </div>
</div>