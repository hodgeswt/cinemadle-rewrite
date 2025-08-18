<script lang="ts">
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
</script>

<PageWrapper>
    <Header />

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="page-title">
        purchase failed
    </h2>

    <p data-testid="page-body">please try again later</p>
</PageWrapper>
