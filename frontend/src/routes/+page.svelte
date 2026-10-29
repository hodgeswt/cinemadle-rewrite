<script lang="ts">
    import { healthcheck } from "$lib/middleware";
    import { onMount } from "svelte";
    import { Container, type IGuessService } from "$lib/services";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    import Game from "$lib/business/game/Game.svelte";
    import { isoDateNoTime } from "$lib/util";
    import { getGameState } from "$lib/business/game/Game.state.svelte";
    import Header from "$lib/ui/Header.svelte";

    let gameId = $state(isoDateNoTime());

    let guessService = (): IGuessService => Container.it().GuessService;
    let serverDown = $state(false);
    let game = $derived(getGameState(gameId));

    onMount(async () => {
        game.parentLoading = true;
        try {
            serverDown = !(await healthcheck());
        } catch (e) {
            serverDown = true;
        }

        game.parentLoading = false;
    });
</script>

<PageWrapper>
    {#if serverDown}
        <Header showDate={true} lose={false} />
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
