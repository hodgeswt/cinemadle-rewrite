<script lang="ts">
    import { goto } from "$app/navigation";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    import { onMount } from "svelte";

    onMount(async () => {
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

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
        purchase successful, redirecting...
    </h2>

    <p>if a redirect does not happen, click <a href="/purchase" class="underline">here</a></p>
</PageWrapper>
