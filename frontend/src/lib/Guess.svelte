<script lang="ts">
    import * as Card from "$lib/components/ui/card";
    import type { GuessDomain } from "./domain";

    export let props: GuessDomain = {
        title: "Unknown Movie",
        cards: [],
    };
</script>

<Card.Root>
    <Card.Header><Card.Title>{props.title}</Card.Title></Card.Header>
    <Card.Content>
        {#each props.cards as { color, data }}
            {#each data as datum}
                <Card.Root>
                    <Card.Content>
                        <div class="bg-{color} mg-4">
                            {datum}
                        </div>
                    </Card.Content>
                </Card.Root>
            {/each}
        {/each}
    </Card.Content>
</Card.Root>
