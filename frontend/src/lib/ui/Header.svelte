<script lang="ts">
    import { FeatureFlags } from "$lib/domain";
    import { Container } from "$lib/services";
    import { userStore } from "$lib/stores";
    import { isoDateNoTime } from "$lib/util";
    import { Menu } from "@lucide/svelte";

    export let showEmail = false;
    export let showDate = false;

    const paymentsEnabled = Container.it().FeatureFlagService.getFeatureFlag(FeatureFlags.PaymentsEnabled);

    let menuOpen = false;

    function toggleMenu() {
        menuOpen = !menuOpen;
    }
</script>

<div class="w-full flex justify-between items-center mb-4">
    <h1 class="flex-1 text-4xl font-extrabold leading-none tracking-tight" data-testid="cinemadle-title">
        cinemadle
    </h1>

    <div class="flex-1 flex flex-col text-right justify-center">
        <div class="flex-1 flex justify-end">
            <div class="relative">
                <button
                    class="block p-3 focus:outline-none"
                    on:click={toggleMenu}
                    data-testid="menu-button"
                >
                    <Menu />
                </button>

                <div
                    class={`absolute right-0 mt-2 w-48 bg-white shadow-lg rounded-lg transform transition-transform duration-300 ease-in-out ${menuOpen ? "z-[9999999] max-h-screen" : "max-h-0 overflow-hidden"}`}
                >
                    {#if !$userStore.loggedIn}
                        <a
                            href="/login"
                            class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                            data-testid="login-link"
                            >log in</a
                        >
                        <a
                            href="/signup"
                            class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                            data-testid="signup-link"
                            >sign up</a
                        >
                    {:else}
                        {#if paymentsEnabled}
                            <a
                                href="/purchase"
                                class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                                data-testid="purchase-link"
                                >purchase</a
                            >
                        {/if}
                    {/if}
                    <a
                        href="/about"
                        class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                        data-testid="about-link"
                        >about</a
                    >
                    <a
                        href="/devinfo"
                        class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                        data-testid="devinfo-link"
                        >dev info</a
                    >
                    <a
                        href="/"
                        class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
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
    <p class="block text-sm pb-4 text-gray-700" data-testid="user-email">
        User: {$userStore.email}
    </p>
{/if}

