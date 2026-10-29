<script lang="ts">
    import { healthcheck } from "$lib/middleware";
    import { onMount } from "svelte";
    import { Container, type IGuessService } from "$lib/services";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    import Game from "$lib/business/game/Game.svelte";
    import { isoDateNoTime } from "$lib/util";

    let guessService = (): IGuessService => Container.it().GuessService;
    let serverDown = $state(false);

    onMount(async () => {
        try {
            serverDown = !(await healthcheck());
        } catch (e) {
            serverDown = true;
        }
    });
</script>

<PageWrapper>
    {#if serverDown}
        <h2
            class="mb-4 text-2xl font-semibold leading-none tracking-tight"
            data-testid="serverdown-text"
        >
            server down. please try again later
        </h2>
    {/if}
    {#if !serverDown}
        <Game gameId={isoDateNoTime()} guessService={guessService()} />
    {/if}
</PageWrapper>
