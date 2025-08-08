<script lang="ts">
    import BuyMeAPizza from "$lib/ui/BuyMeAPizza.svelte";
    import { Button } from "$lib/components/ui/button";
    import { userStore } from "$lib/stores";
    import Header from "$lib/ui/Header.svelte";
</script>

<div class="p-4 flex justify-center min-h-screen">
    <div class="w-full lg:w-1/2 md:w-1/2 sm:w-full">
        <Header />
        
        <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
            known issues:
        </h2>
        <ul class="list-disc">
            <li>if you log in, then log out, you must refresh to see your previous guesses</li>
        </ul>

        <BuyMeAPizza />
    </div>
</div>
