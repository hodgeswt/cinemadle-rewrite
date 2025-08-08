<script lang="ts">
    import BuyMeAPizza from "$lib/ui/BuyMeAPizza.svelte";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
</script>

<PageWrapper>
    <Header />

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
        known issues:
    </h2>
    <ul class="list-disc">
        <li>
            if you log in, then log out, you must refresh to see your previous
            guesses
        </li>
    </ul>

    <BuyMeAPizza />
</PageWrapper>
