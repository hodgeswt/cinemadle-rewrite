<script lang="ts">
    import { FeatureFlags } from "$lib/domain";
    import { Container } from "$lib/services";
    import { userStore } from "$lib/stores";
    import { isDarkMode } from "$lib/stores/theme";
    import { isoDateNoTime } from "$lib/util";
    import { Menu, Sun, Moon } from "@lucide/svelte";
    import { onMount } from "svelte";

    const { showEmail = false, showDate = false } = $props();

    let paymentsEnabled = $state(false);
    let menuOpen = $state(false);

    onMount(async () => {
        paymentsEnabled = await Container.it().FeatureFlagService.getFeatureFlag(FeatureFlags.PaymentsEnabled);
    });

    function toggleMenu() {
        menuOpen = !menuOpen;
    }
</script>

<div class="w-full flex justify-between items-center mb-4">
    <h1 class="flex-1 text-4xl font-extrabold leading-none tracking-tight" data-testid="cinemadle-title">
        cinemadle
    </h1>

    <div class="flex-1 flex flex-col text-right justify-center">
        <div class="flex-1 flex justify-end items-center gap-2">
            <button
                class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
                onclick={() => isDarkMode.update(n => !n)}
                data-testid="theme-toggle"
            >
                {#if $isDarkMode}
                    <Sun class="h-5 w-5" />
                {:else}
                    <Moon class="h-5 w-5" />
                {/if}
            </button>
            <div class="relative">
                <button
                    class="block p-3 focus:outline-none"
                    onclick={toggleMenu}
                    data-testid="menu-button"
                >
                    <Menu />
                </button>

                <div
                    class={`absolute right-0 mt-2 w-48 ${$isDarkMode ? 'bg-gray-500' : 'bg-white'} shadow-lg rounded-lg transform transition-transform duration-300 ease-in-out ${menuOpen ? "z-[9999999] max-h-screen" : "max-h-0 overflow-hidden"}`}
                >
                    {#if !$userStore.loggedIn}
                        <a
                            href="/login"
                            class="block px-4 py-2 text-sm text-gray-700 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                            data-testid="login-link"
                            >log in</a
                        >
                        <a
                            href="/signup"
                            class="block px-4 py-2 text-sm text-gray-700 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                            data-testid="signup-link"
                            >sign up</a
                        >
                    {:else}
                        {#if paymentsEnabled}
                            <a
                                href="/purchase"
                                class="block px-4 py-2 text-sm text-gray-700 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                                data-testid="purchase-link"
                                >purchase</a
                            >
                        {/if}
                    {/if}
                    <a
                        href="/about"
                        class="block px-4 py-2 text-sm text-gray-700 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                        data-testid="about-link"
                        >about</a
                    >
                    <a
                        href="/devinfo"
                        class="block px-4 py-2 text-sm text-gray-700 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                        data-testid="devinfo-link"
                        >dev info</a
                    >
                    <a
                        href="/"
                        class="block px-4 py-2 text-sm text-gray-700 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                        data-testid="home-link"
                        >home</a
                    >
                </div>
            </div>
        </div>
    </div>
</div>
{#if showDate}
    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="cinemadle-date">
        {isoDateNoTime()}
    </h2>
{/if}
{#if $userStore.loggedIn && showEmail}
    <p class="block text-sm pb-4 {$isDarkMode ? 'text-white-500' : 'text-gray-700'}" data-testid="user-email">
        User: {$userStore.email}
    </p>
{/if}

