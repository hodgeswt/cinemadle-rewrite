<script lang="ts">
    import BuyMeAPizza from "$lib/ui/BuyMeAPizza.svelte";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
</script>

<PageWrapper>
    <Header />

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="page-title">
        known issues:
    </h2>
    <ul class="list-disc">
        <li data-testid="knownissue-1">
            if you log in, then log out, you must refresh to see your previous
            guesses
        </li>
    </ul>

    <BuyMeAPizza />
</PageWrapper>
