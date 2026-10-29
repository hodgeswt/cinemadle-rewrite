<script lang="ts">
    import { healthcheck } from "$lib/middleware";
    import { onMount } from "svelte";
    import { Container, type IGuessService } from "$lib/services";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    import { getGameState } from "$lib/business/game/Game.state.svelte";
    import Game from "$lib/business/game/Game.svelte";

    let { data }: { data: { id: string } } = $props();
    let gameId = data.id;
    let guessService = (): IGuessService => Container.it().GuessService;

    let serverDown = $state(false);
    let game = getGameState(gameId);

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
        <Header showDate={true} customGameId={gameId} lose={false} />
        <h2
            class="mb-4 text-2xl font-semibold leading-none tracking-tight"
            data-testid="customgame-serverdown-text"
        >
            server down. please try again later
        </h2>
    {/if}

    {#if !serverDown}
        <Header showDate={true} customGameId={gameId} lose={game.lose} />
        <Game {gameId} guessService={guessService()} isCustomGame={true} />
    {/if}
</PageWrapper>
