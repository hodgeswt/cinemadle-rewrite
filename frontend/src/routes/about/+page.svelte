<script lang="ts">
    import { Button } from "$lib/components/ui/button";
    import { userStore } from "$lib/stores";
</script>

<div class="p-4 flex justify-center min-h-screen">
    <div class="w-full lg:w-1/2 md:w-1/2 sm:w-full p-4">
        <div class="w-full flex justify-between items-center">
            <h1
                class="flex-1 m-4 text-4xl font-extrabold leading-none tracking-tight"
            >
                cinemadle
            </h1>
            <div
                class="w-full flex-1 flex flex-col m-4 p-4 text-right justify-center"
            >
                {#if !$userStore.loggedIn}
                    <a href="/login" class="underline">Log In</a>
                    <a href="/signup" class="underline">Sign Up</a>
                {:else}
                    <p>{$userStore.email}</p>
                {/if}
                <a href="/" class="underline">Home</a>
            </div>
        </div>

        <h2 class="m-4 text-2xl font-semibold leading-non tracking-tight">
            About
        </h2>

        <p class="m-4">
            cinemadle is a guessing game. each day, a movie is randomly
            selected. your task is to figure out the movie based on the clues
            provided.
        </p>

        <div class="flex justify-center items-center">
            <hr
                class="my-12 h-0.5 w-1/2 border-t-0 bg-gray-300 dark:bg-white/10"
            />
        </div>

        <p class="m-4">
            each movie gussed provides you information. the tiles within each
            guess are color-coded to indicate if the information is wrong
            (gray), partially correct (yellow), or correct (green). each
            category has different criteria to determine these colors.
        </p>

        <p class="m-4">
            on the genre list and the cast list, a star will indicate which
            item(s) from your guess are correct.
        </p>

        <div class="m-4">
            <ul class="list-disc">
                <li>
                    rating: yellow if your guess is adjacent to the MPAA rating
                    of the target
                </li>
                <li>
                    year: yellow if your guess was released within ten years of
                    the target
                </li>
                <li>
                    cast: yellow if any of the actors in your guessed movie are
                    in the target movie
                </li>
                <li>
                    genre: yellow if any of the genres of your guessed movie
                    match the target movie
                </li>
            </ul>
        </div>

        <div class="flex justify-center items-center">
            <hr
                class="my-12 h-0.5 w-1/2 border-t-0 bg-gray-300 dark:bg-white/10"
            />
        </div>

        <p class="m-4">code: will hodges</p>

        <p class="m-4">design: will hodges and myke yunis</p>

        {#if $userStore.loggedIn}
            <div class="flex justify-center items-center">
                <Button
                    onclick={() => {
                        userStore.setLoggedOut();
                    }}
                >
                    <p>Log out</p>
                </Button>
            </div>
        {/if}
    </div>
</div>
