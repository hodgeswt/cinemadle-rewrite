<script lang="ts">
  import { Button } from "$lib/components/ui/button";
  import { getGameState } from "../Game.state.svelte";

  let { showShareSheet, gameId } = $props();

  let game = getGameState(gameId);

  let text = $derived(game.win ? "you win!" : "better luck next time!");
  let textColor = $derived(game.win ? "text-green-400" : "text-red-600");
  let bgColor = $derived(game.win ? "bg-green-400" : "bg-red-400");
  let dataTestId = $derived(game.win ? "youwin" : "youlose");

  let dataTestIdPrefix = $derived(game.isCustomGame ? "customgame-" : "");
</script>

<div class="flex items-center mb-4">
  <h2
    class={`flex-1 text-3xl font-semibold ${textColor} leading-none tracking-tight`}
    data-testid={dataTestIdPrefix + dataTestId}
  >
    {text}
  </h2>
  <Button class={bgColor} onclick={showShareSheet} data-testid={`${dataTestIdPrefix}share-button`}>
    share
  </Button>
</div>
