<script lang="ts">
    import { goto } from "$app/navigation";
    import { FeatureFlags } from "$lib/domain";
    import { Container } from "$lib/services";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    import { onMount } from "svelte";

    const paymentsEnabled = Container.it().FeatureFlagService.getFeatureFlag(FeatureFlags.PaymentsEnabled);
   
    onMount(async () => {
        if (!await paymentsEnabled) {
            goto("/");
        }
        let count = 0;
        while (count < 3) {
            count += 1;
            await new Promise((x) => setTimeout(x, 1000));
        }

        goto("/");
    })
</script>

<PageWrapper>
    <Header />

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="page-title">
        purchase successful, redirecting...
    </h2>

    <p data-testid="redirect-text">if a redirect does not happen, click <a href="/purchase" class="underline" data-testid="redirect-link">here</a></p>
</PageWrapper>
