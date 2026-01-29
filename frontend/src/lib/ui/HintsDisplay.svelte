<script lang="ts">
    import type { Hints } from "$lib/dto";
    import { hintsStore } from "$lib/stores";
    import { isDarkMode } from "$lib/stores/theme";

    // Ordered list of hint categories (most useful first)
    const hintCategories = [
        { key: 'year', label: 'YEAR' },
        { key: 'genre', label: 'GENRE' },
        { key: 'rating', label: 'RATING' },
        { key: 'cast', label: 'CAST' },
        { key: 'creatives', label: 'CREATIVES' },
        { key: 'boxOffice', label: 'BOX OFFICE' },
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

<div class="mb-4" data-testid="hints-display">
    <h3 
        class="text-lg font-semibold mb-3 {$isDarkMode ? 'text-gray-300' : 'text-gray-700'}"
        data-testid="hints-display-header"
    >
        You're looking for a movie...
    </h3>
    
    <div class="grid grid-cols-2 md:grid-cols-3 gap-2">
        {#each hintCategories as category, i}
            {@const hints = getHintsForKey(category.key)}
            {@const content = getHintContent(hints)}
            <div 
                class="p-3 rounded-lg shadow-md {$isDarkMode 
                    ? 'bg-[rgba(255,255,255,0.08)] border border-white/20' 
                    : 'bg-gradient-to-br from-gray-100 to-gray-200 border border-gray-300'}"
                data-testid={`hints-display-card-${category.key}`}
            >
                <div 
                    class="text-xs font-bold mb-1 {$isDarkMode ? 'text-gray-400' : 'text-gray-500'}"
                    data-testid={`hints-display-card-${category.key}-label`}
                >
                    {category.label}
                </div>
                <div 
                    class="text-sm {$isDarkMode ? 'text-gray-200' : 'text-gray-800'} break-words"
                    data-testid={`hints-display-card-${category.key}-content`}
                >
                    {content}
                </div>
            </div>
        {/each}
    </div>
</div>
