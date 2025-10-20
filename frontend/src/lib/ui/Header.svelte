<script lang="ts">
    import { FeatureFlags } from "$lib/domain";
    import { Container } from "$lib/services";
    import { userStore } from "$lib/stores";
    import { isDarkMode } from "$lib/stores/theme";
    import { isoDateNoTime } from "$lib/util";
    import { Menu, Sun, Moon } from "@lucide/svelte";
    import { onMount } from "svelte";

    const { showEmail = false, showDate = false, customGameId = null } = $props<{
        showEmail?: boolean;
        showDate?: boolean;
        customGameId?: string | null;
    }>();

    let paymentsEnabled = $state(false);
    let menuOpen = $state(false);

    onMount(async () => {
        paymentsEnabled = await Container.it().FeatureFlagService.getFeatureFlag(FeatureFlags.PaymentsEnabled);
    });

    function toggleMenu() {
        menuOpen = !menuOpen;
    }
</script>

<div class="w-full flex justify-between items-center">
    <div class="flex-1 flex items-center gap-3">
        <h1 class="text-4xl font-extrabold leading-none tracking-tight {$isDarkMode ? 'bg-gradient-to-r from-[#00ff88] to-[#00ffcc]' : 'bg-gradient-to-r from-green-500 to-green-600'} bg-clip-text text-transparent" data-testid="cinemadle-title">
            cinemadle
        </h1>
       
    </div>
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
                    class={`absolute right-0 mt-2 w-48 ${$isDarkMode ? 'bg-[rgb(18,21,31)]' : 'bg-white'} shadow-lg rounded-lg transform transition-transform duration-300 ease-in-out ${menuOpen ? "z-[9999999] max-h-screen" : "max-h-0 overflow-hidden"}`}
                >
                    {#if !$userStore.loggedIn}
                        <a
                            href="/login"
                            class="block px-4 py-2 text-sm text-gray-500 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                            data-testid="login-link"
                            >log in</a
                        >
                        <a
                            href="/signup"
                            class="block px-4 py-2 text-sm text-gray-500 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                            data-testid="signup-link"
                            >sign up</a
                        >
                    {:else}
                        {#if paymentsEnabled}
                            <a
                                href="/purchase"
                                class="block px-4 py-2 text-sm text-gray-500 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                                data-testid="purchase-link"
                                >purchase</a
                            >
                        {/if}
                        <a
                            href="/customCreate"
                            class="block px-4 py-2 text-sm text-gray-500 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                            data-testid="customcreate-link"
                            >create custom game</a
                        >
                    {/if}
                    <a
                        href="/about"
                        class="block px-4 py-2 text-sm text-gray-500 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                        data-testid="about-link"
                        >about</a
                    >
                    <a
                        href="/devinfo"
                        class="block px-4 py-2 text-sm text-gray-500 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                        data-testid="devinfo-link"
                        >dev info</a
                    >
                    <a
                        href="/"
                        class="block px-4 py-2 text-sm text-gray-500 hover:{$isDarkMode ? 'bg-gray-300' : 'bg-gray-100'}"
                        data-testid="home-link"
                        >home</a
                    >
                </div>
            </div>
        </div>
    </div>
</div>

 {#if showDate}
    <span 
        class="inline-block rounded-full px-3 py-1 text-base font-semibold
            {$isDarkMode ? 'bg-gradient-to-r from-[#00ff88] to-[#00ffcc] text-gray-500' : 'bg-gradient-to-r from-green-500 via-green-400 to-green-600'} border-gray-200 dark:border-gray-700 mb-2"
        data-testid="cinemadle-date"
    >
        {customGameId ? `Custom Game: ${customGameId}` : isoDateNoTime()}
    </span>
{/if}

{#if $userStore.loggedIn && showEmail}
    <p class="block text-sm pb-4 {$isDarkMode ? 'text-white-500' : 'text-gray-700'}" data-testid="user-email">
        User: {$userStore.email}
    </p>
{/if}

