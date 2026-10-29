<script lang="ts">
  import Dialog from "$lib/ui/Dialog.svelte";
  import type { Snippet } from "svelte";
  import type { NullFunc, OpenDialogRequest } from "../GameTypes";
  import { writable, type Writable } from "svelte/store";
  import { getGameState } from "../Game.state.svelte";

  let dialogId: string = $state("");
  let titleText: string = $state("");
  let body: Snippet | undefined = $state();
  let confirmButtonText: string = $state("ok");
  let cancelButtonText: string | undefined = $state();
  let cancelCallback: NullFunc | undefined = $state();

  let dialogOpen: Writable<boolean> = writable(false);
  let { gameId } = $props();
  let game = getGameState(gameId);

  export const open = (request: OpenDialogRequest) => {
    dialogId = `${game.isCustomGame ? "customgame-" : ""}${request.dialogId}`;
    titleText = request.titleText;
    body = request.body;
    confirmButtonText = request.confirmButtonText ?? "ok";
    cancelButtonText = request.cancelButtonText;
    cancelCallback = request.cancelCallback;

    dialogOpen.set(true);
  };

  export const close = () => {
    dialogOpen.set(false);
  };
</script>

<Dialog
  open={dialogOpen}
  title={titleText}
  id={dialogId}
  confirmButton={confirmButtonText}
  cancelButton={cancelButtonText}
  {cancelCallback}
>
  {#if body}
    {@render body()}
  {/if}
</Dialog>
