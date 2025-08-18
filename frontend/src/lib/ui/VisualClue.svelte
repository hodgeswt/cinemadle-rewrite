<script lang="ts">
    import Skeleton from "$lib/components/ui/skeleton/skeleton.svelte";
    import Logger from "$lib/logger";
    import { PING_LIMIT } from "$lib/middleware";
    import type { IGuessService } from "$lib/services";
    import { Container } from "$lib/services";
    import { onMount } from "svelte";

    let loading = $state(true);
    let guessServicePing = $state(0);
    let serverDown = $state(false);
    let imageData = $state("");

    let guessService = (): IGuessService => Container.it().GuessService;

    onMount(async () => {
        while (!guessService().isInitialized()) {
            if (guessServicePing === PING_LIMIT) {
                serverDown = true;
                loading = false;
                return;
            }

            guessServicePing += 1;
            await new Promise((x) => setTimeout(x, 1000));
        }

        const result = await guessService().getVisualClue();

        if (!result.ok) {
            serverDown = true;
            loading = false;
            return;
        }

        Logger.log("Image data: {0}", result.data!);

        imageData = result.data!.imageData;
        loading = false;
        serverDown = false;
    })
</script>

{#if loading}
    <Skeleton class="h-[200px] w-full mb-4" data-testid="visualclue-skeleton" />
{:else if serverDown}
    <p data-testid="visualclue-loaderror-text">unable to load image</p>
{:else}
    <img src={imageData} alt="blurred movie banner" style='w-500' data-testid="visualclue-image"/>
{/if}

