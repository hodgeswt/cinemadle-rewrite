<script lang="ts">
    import BuyMeAPizza from "$lib/BuyMeAPizza.svelte";
    import { Button } from "$lib/components/ui/button";
    import { userStore } from "$lib/stores";
</script>

<div class="p-4 flex justify-center min-h-screen">
    <div class="w-full lg:w-1/2 md:w-1/2 sm:w-full">
        <div class="w-full flex justify-between items-center mb-4">
            <h1
                class="flex-1 text-4xl font-extrabold leading-none tracking-tight"
            >
                cinemadle
            </h1>
            <div class="flex-1 flex flex-col text-right justify-center">
                {#if !$userStore.loggedIn}
                    <a href="/login" class="underline">log in</a>
                    <a href="/signup" class="underline">sign up</a>
                {/if}
                <a href="/about" class="underline">about</a>
                <a href="/" class="underline">home</a>
            </div>
        </div>
        
        <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
            known issues:
        </h2>
        <ul class="list-disc">
            <li>if you log in, then log out, you must refresh to see your previous guesses</li>
            <li>the box office box is busted</li>
        </ul>

        <BuyMeAPizza />
    </div>
</div>
