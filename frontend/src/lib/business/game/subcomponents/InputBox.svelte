<script lang="ts">
    import Button from "$lib/components/ui/button/button.svelte";
    import Input from "$lib/components/ui/input/input.svelte";
    import { Search } from "@lucide/svelte";
    import { getGameState } from "../Game.state.svelte";

    let { gameId, guessMadeCallback } = $props();

    function guessChange(_event: Event): void {
      game.searchOpen = game.guessInput !== "";
    }

    async function handleGuess(event: Event | null): Promise<void> {
      event?.preventDefault();

      guessMadeCallback(game.guessInput);
      game.guessInput = "";
    }

    let game = getGameState(gameId);
    let dataTestIdPrefix = $derived(game.isCustomGame ? "customgame-" : "");

</script>

<div class="flex space-x-2 mb-4">
  <Input
    type="text"
    placeholder={`Guess... ${game.remainingGuesses} remaining`}
    bind:value={game.guessInput}
    onchange={guessChange}
    class="flex-1 text-base"
    disabled={game.done}
    data-testid={`${dataTestIdPrefix}guess-input`}
  />

  <Button
    type="submit"
    size="icon"
    onclick={handleGuess}
    class="relative -z-1000"
    disabled={game.done || game.guessInput.trim() === ""}
    data-testid={`${dataTestIdPrefix}submit-button`}
  >
    <Search />
  </Button>
</div>