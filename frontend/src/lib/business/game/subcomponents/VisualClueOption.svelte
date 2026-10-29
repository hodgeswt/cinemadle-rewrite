<script lang="ts">
    import { Button } from "$lib/components/ui/button";
    import { isDarkMode } from "$lib/stores/theme";
    import { Info } from "@lucide/svelte";
    import { getGameState } from "../Game.state.svelte";

    let { onclick, gameId } = $props();

    let game = getGameState(gameId);

    let dataTestIdPrefix = $derived(game.isCustomGame ? "customgame-" : "");

</script>

<div
  class="mb-4 p-4 {$isDarkMode
    ? game.lose
      ? 'bg-gradient-to-r from-red-600 to-red-800 border-red-800'
      : game.win
        ? 'bg-gradient-to-r from-green-600 to-green-800 border-green-800'
        : 'bg-gradient-to-r from-indigo-600 to-purple-800 border-indigo-800'
    : game.lose
      ? 'bg-gradient-to-r from-red-50 to-red-100 border-red-200'
      : game.win
        ? 'bg-gradient-to-r from-green-50 to-green-100 border-green-200'
        : 'bg-gradient-to-r from-indigo-50 to-purple-50 border-indigo-200'} rounded-lg border {$isDarkMode
    ? game.lose
      ? 'border-red-800'
      : game.win
        ? 'border-green-800'
        : 'border-indigo-800'
    : game.lose
      ? 'border-red-200'
      : game.win
        ? 'border-green-200'
        : 'border-indigo-200'}"
>

  <div class="flex items-center justify-between">
    <div class="flex items-center space-x-2">
      <Info
        class={$isDarkMode
          ? "text-white"
          : game.lose
            ? "text-red-600"
            : game.win
              ? "text-green-600"
              : "text-indigo-600"}
      />
      <span
        class="text-sm {$isDarkMode
          ? 'text-white'
          : game.lose
            ? 'text-red-600'
            : game.win
              ? 'text-green-400'
              : 'text-indigo-400'}"
        data-testid={`${dataTestIdPrefix}hint-text`}
        >{game.lose
          ? "needed a hint?"
          : game.win
            ? "needed a hint?"
            : "need a hint?"}</span
      >
    </div>
    <Button
      onclick={onclick}
      variant="secondary"
      size="sm"
      class="{game.lose
        ? 'bg-red-600 hover:bg-red-700'
        : game.win
          ? 'bg-green-600 hover:bg-green-700'
          : 'bg-indigo-600 hover:bg-indigo-700'} text-white"
      data-testid={`${dataTestIdPrefix}visualclue-button`}
    >
      view visual clue
    </Button>
  </div>
</div>
