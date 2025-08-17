<script lang="ts">
    import { Button } from "$lib/components/ui/button";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import { writable } from "svelte/store";
    import { userStore } from "$lib/stores";
    import { goto } from "$app/navigation";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    import { get, post } from "$lib/middleware";
    import { isPurchaseResponseDto, isQuantitiesDto, type PurchaseDetailsDto, type PurchaseResponseDto } from "$lib/dto";
    import { err, ok, type Result } from "$lib/result";
    import { loadStripe } from '@stripe/stripe-js';
    import Logger from "$lib/logger";
    import { onMount } from "svelte";
    import { isoDateNoTime } from "$lib/util";

    let openError = writable(false);
    let errorMessage = $state("");
    let purchaseVisualClueDisabled = $state(false);
    let purchaseCategoryRevealDisabled = $state(false);

    let quantities = $state({} as { [key: string]: number });

    if (!$userStore.loggedIn) {
        goto("/");
    }

    const visualClueProductId = "price_1Rwv2IBpMK1eGQsnnKpUaxCb";
    const categoryRevealProductId = "price_1Rww6UBpMK1eGQsngue55u43";

    const stripeFrontendKey = import.meta.env.VITE_STRIPE_FRONTEND_KEY;

    const nameMap = {
        'VisualClue': 'visual clue',
        'CategoryReveal': 'category reveal'
    } as { [key: string]: string }

    onMount(async () => {
        let result = await get(
            '/api/payments/quantities',
            { },
            { Authorization: $userStore.jwt },
            false,
            true,
        )

        if (result.ok) {
            let data = JSON.parse(result.data!);
            if (isQuantitiesDto(data)) {
                Logger.log("Got quantities: {0}", data.quantities)
                quantities = data.quantities;
            } else {
                Logger.log("got bad data")
            }
        } else {
            Logger.log("unable to load quantities")
        }
    });

    async function purchase(productId: string): Promise<Result<PurchaseResponseDto>> {
        Logger.log("Making purchase request to server");
        let result = await post(
            '/api/payments/purchase',
            true,
            JSON.stringify({ productId: productId, quantity: 1 } as PurchaseDetailsDto),
            { Authorization: $userStore.jwt, "Content-Type": "application/json"}
        );

        if (!result.ok) {
            return err("unable to make purchase request");
        }

        const data = JSON.parse(result.data!);
        if (!isPurchaseResponseDto(data)) {
            return err("unable to make purchase request");
        }

        return ok(data);
    }

    async function stripeRedirect(sessionId: string) {
        Logger.log("Stripe frontend key: {0}", stripeFrontendKey);
        const stripe = await loadStripe(stripeFrontendKey);
        if (stripe === null || stripe === undefined) {
            errorMessage = "unable to load checkout";
            openError.set(true);
            return;
        }

        await stripe?.redirectToCheckout({sessionId});
    }

    async function purchaseVisualClue() {
        purchaseVisualClueDisabled = true;
        Logger.log("Making purchase request for visual clue");
        let result = await purchase(visualClueProductId);

        if (!result.ok) {
            errorMessage = result.error!;
            openError.set(true);
            return;
        }

        await stripeRedirect(result.data!.sessionId);
    }

    async function purchaseCategoryReveal() {
        purchaseCategoryRevealDisabled = true;
        let result = await purchase(categoryRevealProductId);

        if (!result.ok) {
            errorMessage = result.error!;
            openError.set(true);
            return;
        }

        await stripeRedirect(result.data!.sessionId);
    }

    function closeDialog() {
        openError.set(false);
        purchaseVisualClueDisabled = false;
        purchaseCategoryRevealDisabled = false;
        errorMessage = "";
    }
</script>

<PageWrapper>
    <Header />

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
        purchase
    </h2>

    {#if Object.keys(quantities).length > 0}
        <div class="mt-6 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg mb-4">
            <h3 class="text-lg font-semibold mb-3">your current items</h3>
            <ul class="space-y-2">
                {#each Object.entries(quantities) as [productId, quantity]}
                    <li class="flex justify-between items-center p-3 bg-white dark:bg-gray-700 rounded-md shadow-sm">
                        <span class="font-medium text-gray-700 dark:text-gray-200">
                            {nameMap[productId]}
                        </span>
                        <span class="text-lg font-semibold text-blue-600 dark:text-blue-400">
                            {quantity}
                        </span>
                    </li>
                {/each}
            </ul>
        </div>
    {:else}
        <div class="mt-6 p-4 mb-4 bg-gray-50 dark:bg-gray-800 rounded-lg text-center text-gray-500 dark:text-gray-400">
            <p>No items yet. Purchase some to get started!</p>
        </div>
    {/if}

    <p class="mb-4">Stripe payments will indicate recipient Will Hodges</p>

    <Button type="submit" size="icon" onclick={purchaseVisualClue} class="w-full mb-4" disabled={purchaseVisualClueDisabled}>
        <p class="m-1">Buy 10 Visual Clues</p>
    </Button>

    <Button type="submit" size="icon" onclick={purchaseCategoryReveal} class="w-full mb-4" disabled={purchaseCategoryRevealDisabled}>
        <p class="m-1">Buy 10 Category Reveals</p>
    </Button>

    <AlertDialog.Root bind:open={$openError}>
        <AlertDialog.Content>
            <AlertDialog.Title>Uh-oh!</AlertDialog.Title>
            <AlertDialog.Description>
                {errorMessage}
            </AlertDialog.Description>
            <AlertDialog.Footer>
                <AlertDialog.Action on:click={closeDialog}>
                    ok
                </AlertDialog.Action>
            </AlertDialog.Footer>
        </AlertDialog.Content>
    </AlertDialog.Root>
</PageWrapper>
