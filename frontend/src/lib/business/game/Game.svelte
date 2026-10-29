<script lang="ts">
  import BuyMeAPizza from "$lib/ui/BuyMeAPizza.svelte";
  import HintsDisplay from "$lib/ui/HintsDisplay.svelte";
  import { flip } from "svelte/animate";
  import { validateAndRefreshToken } from "$lib/middleware";
  import { onMount } from "svelte";
  import { guessStore, userStore, hintsStore } from "$lib/stores";
  import { toast } from "svelte-sonner";
  import { type IGuessService } from "$lib/services";
  import Header from "$lib/ui/Header.svelte";
  import VisualClue from "$lib/ui/VisualClue.svelte";
  import { getGameState } from "./Game.state.svelte";
  import GameOverSubheader from "./subcomponents/GameOverSubheader.svelte";
  import GameDialogs from "./subcomponents/GameDialogs.svelte";
  import GameLoading from "./subcomponents/GameLoading.svelte";
  import VisualClueOption from "./subcomponents/VisualClueOption.svelte";
  import InputBox from "./subcomponents/InputBox.svelte";
  import SearchResults from "./subcomponents/SearchResults.svelte";
  import Guess from "./subcomponents/Guess.svelte";

  type PropsType = {
    gameId: string;
    guessService: IGuessService;
    isCustomGame: boolean;
  };

  let { gameId, guessService, isCustomGame }: PropsType = $props();

  const game = getGameState(gameId);

  const openError = (message: string) => {
    game.errorMessage = message;

    game.dialogs?.open({
      titleText: "uh-oh!",
      dialogId: "error",
      body: errorText,
    });
  };

  onMount(async () => {
    try {
      game.guessService = guessService;
      game.isCustomGame = isCustomGame;

      let promises = [];

      if ($userStore.loggedIn) {
        promises.push(validateAndRefreshToken($userStore.jwt, $userStore.refreshToken));
        hintsStore.fetchHints($userStore.jwt);
      } else {
        const anonUserId = localStorage.getItem("anonUserId");
        hintsStore.fetchHintsAnon(anonUserId ?? "");
      }

      promises.push(guessService.initialize());
      
      await Promise.allSettled(promises);

      const prev = await guessService.getPreviousGuesses();
      if (!prev.ok) {
        throw new Error(prev.error!);
      }

      guessStore.update((s) => ({ ...s, guesses: [...prev.data!] }));

      game.gameLoading = false;
    } catch (e) {
      openError("Unable to contact server.");
      game.gameLoading = false;
    }
  });

  async function showShareSheet(_event: Event): Promise<void> {
    const result = await guessService.getGameSummary();
    game.shareData = !result.ok ? [result.error!] : result.data!.summary;
    game.dialogs?.open({
      titleText: "your results",
      dialogId: "results",
      confirmButtonText: "close",
      cancelButtonText: "share",
      cancelCallback: () => deviceShare(),
      body: shareSheet,
    });
  }

  async function deviceShare(): Promise<void> {
    let shareData = game.shareData.join("\n");

    if (!navigator.share) {
      navigator.clipboard.writeText(shareData);
      toast("copied to clipboard");
      return;
    }

    const url = isCustomGame ? `https://cinemadle.com/customGame/${gameId}` : "https://cinemadle.com";

    navigator.share({
      title: "cinemadle",
      text: shareData,
      url: url,
    });
  }

  async function makeGuess(guess: string): Promise<void> {
    let result = await guessService.guess(guess);

    if (!result.ok) {
      openError(result.error!);
      return;
    }

    // Refresh hints after successful guess (non-blocking)
    hintsStore.invalidate();
    if ($userStore.loggedIn) {
      hintsStore.fetchHints($userStore.jwt);
    } else {
      const anonUserId = localStorage.getItem("anonUserId");
      hintsStore.fetchHintsAnon(anonUserId ?? "");
    }
  }

  function showVisualClue(_event: Event): void {
    game.dialogs?.open({
      titleText: "visual clue",
      dialogId: "visualclue",
      body: visualClue,
    });
  }
</script>

{#snippet errorText()}
  {game.errorMessage}
{/snippet}

{#snippet shareSheet()}
  {#each game.shareData as line}
    <div class="leading-none">{line}</div>
  {/each}
{/snippet}

{#snippet visualClue()}
  <VisualClue />
{/snippet}

<Header showDate={true} lose={game.lose} />

{#if !game.loading}
  {#if game.gameOver}
    <GameOverSubheader {showShareSheet} {gameId} />
  {/if}

  {#if game.remainingGuesses > 0 && !game.win}
    <InputBox {gameId} guessMadeCallback={makeGuess} />

    {#if game.filteredGuesses.length > 0}
      <SearchResults {gameId} guessMadeCallback={makeGuess} />
    {/if}
  {/if}

  {#if $userStore.loggedIn && game.visualClueEligible}
    <VisualClueOption onclick={showVisualClue} {gameId} />
  {/if}

  {#if game.anyGuesses}
    <HintsDisplay {gameId} />
  {/if}

  <GameDialogs bind:this={game.dialogs} gameId={gameId} />

  <div class="guesses z-10">
    {#each game.noWinGuesses as _, i (_)}
      <div animate:flip={{ duration: 1000 }}>
        <Guess {gameId} index={i} />
      </div>
    {/each}
  </div>
{/if}

{#if game.loading}
  <GameLoading />
{/if}

{#if !game.loading && $guessStore.guesses.length > 0}
  <BuyMeAPizza />
{/if}
