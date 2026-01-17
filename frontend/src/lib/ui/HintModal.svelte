<script lang="ts">
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import type { Hints } from "$lib/dto";
    import type { Writable } from "svelte/store";

    interface HintModalProps {
        open: Writable<boolean>;
        title: string;
        hints: Hints | null;
    }

    let { open, title, hints }: HintModalProps = $props();

    const hintLabelMap: { [key: string]: string } = {
        'boxOffice': 'Possible Box Office',
        'creatives': 'Known Creatives',
        'rating': 'Possible Ratings',
        'genre': 'Known Genres',
        'cast': 'Known Cast',
        'year': 'Possible Years'
    };

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

    function getHintLabel(): string {
        return hintLabelMap[title] ?? 'HINTS';
    }

    function getHintContent(): string {
        if (!hints) return "No information known yet";

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
        return "No information known yet";
    }

    function formatTitle(x: string): string {
        let y = x;
        if (x === "boxOffice") {
            y = "box office";
        }
        return y.charAt(0).toUpperCase() + y.slice(1);
    }

    const close = () => {
        open.set(false);
    }
</script>

<AlertDialog.Root bind:open={$open}>
    <AlertDialog.Content>
        <AlertDialog.Title>{getHintLabel()}</AlertDialog.Title>
        <AlertDialog.Description>
            <div class="space-y-2">
                <div class="text-lg">{getHintContent()}</div>
            </div>
        </AlertDialog.Description>
        <AlertDialog.Footer>
            <AlertDialog.Action onclick={() => close()}>
                Close
            </AlertDialog.Action>
        </AlertDialog.Footer>
    </AlertDialog.Content>
</AlertDialog.Root>
