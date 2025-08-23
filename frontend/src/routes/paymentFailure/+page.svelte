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
    });
    
</script>

<PageWrapper>
    <Header />

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="page-title">
        purchase failed
    </h2>

    <p data-testid="page-body">please try again later</p>
</PageWrapper>
