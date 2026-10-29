<script lang="ts">
  import { isDarkMode } from "$lib/stores/theme";
  import { getGameState } from "../Game.state.svelte";

  let { gameId, guessMadeCallback } = $props();

  let game = getGameState(gameId);
  let dataTestIdPrefix = $derived(game.isCustomGame ? "customgame-" : "");
</script>

<ul
  class={`mt-1 ${$isDarkMode ? "bg-gray-900" : "bg-white"} ${
    $isDarkMode ? "border-gray-700" : "border-gray-300"
  } border rounded shadow-xl absolute z-[9999999]`}
>
  {#each game.filteredGuesses as possibleGuess}
    <li class="p-2 text-lg">
      <button
        onclick={() => {
          game.guessInput = "";
          guessMadeCallback(possibleGuess);
        }}
        data-testid={`${dataTestIdPrefix}guess-${possibleGuess.replaceAll(" ", "-")}-button`}
      >
        {possibleGuess}
      </button>
    </li>
  {/each}
</ul>
