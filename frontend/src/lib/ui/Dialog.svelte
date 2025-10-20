<script lang="ts">
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import type { DialogProps } from "$lib/props";

    let {
        children,
        open,
        title,
        id,
        confirmButton = "ok",
        cancelButton = "",
        confirmCallback = () => {},
        cancelCallback = () => {},
    }: DialogProps = $props();

    const confirmClose = () => {
        confirmCallback();
        open.set(false);
    }

    const cancelClose = () => {
        cancelCallback();
        open.set(false);
    }

</script>

<AlertDialog.Root bind:open={$open}>
    <AlertDialog.Content>
        <AlertDialog.Title data-testid="{id}-title-text">{title}</AlertDialog.Title>
        <AlertDialog.Description data-testid="{id}-body-text">
            {@render children?.()}
        </AlertDialog.Description>
        <AlertDialog.Footer>
            <AlertDialog.Action onclick={() => confirmClose()} data-testid="{id}-ok-button">
                {confirmButton}
            </AlertDialog.Action>
            {#if cancelButton !== ""}
                <AlertDialog.Action onclick={() => {cancelClose()}} data-testid="share-close-button">
                    {cancelButton}
                </AlertDialog.Action>
            {/if}
        </AlertDialog.Footer>
    </AlertDialog.Content>
</AlertDialog.Root>