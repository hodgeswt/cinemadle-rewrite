<script lang="ts">
    import BuyMeAPizza from "$lib/ui/BuyMeAPizza.svelte";
    import { Button } from "$lib/components/ui/button";
    import { userStore } from "$lib/stores";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    import Divider from "$lib/ui/Divider.svelte";
</script>

<PageWrapper>
    <Header showEmail={true} />

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="page-title">
        about
    </h2>

    <p class="mb-4">
        cinemadle is a guessing game. each day, a movie is randomly selected.
        your task is to figure out the movie based on the clues provided.
    </p>

    <Divider />
    
    <p class="mb-4">
        each movie gussed provides you information. the tiles within each guess
        are color-coded to indicate if the information is wrong (gray),
        partially correct (yellow), or correct (green). each category has
        different criteria to determine these colors.
    </p>

    <p class="mb-4">
        on the genre list, the cast list, and the creatives list, a star will
        indicate which item(s) from your guess are correct.
    </p>

    <p class="mb-4">
        one or two arrows may appear on the box office and year boxes to
        indicate how far off your guess was. two arrows means your guess was
        further off, one means less. a threshold is provided in the box as well.
        the arrows indicate which direction the number must go (i.e. up arrows
        mean the number must be bigger).
    </p>

    <div class="mb-4">
        <ul class="list-disc pl-5">
            <li>
                rating: yellow if your guess is adjacent to the MPAA rating of
                the target
            </li>
            <li>
                year: yellow if your guess was released within ten years of the
                target
            </li>
            <li>
                cast: yellow if any of the actors in your guessed movie are in
                the target movie
            </li>
            <li>
                genre: yellow if any of the genres of your guessed movie match
                the target movie
            </li>
            <li>
                box office: yellow if your guess earned within $100M of the
                target movie's box office gross
            </li>
            <li>
                creatives: yellow if any of your guessed creatives are creatives
                on the target movie
            </li>
        </ul>
    </div>

    <Divider />

    <p class="mb-4">
        cinemadle is free to play, but if you enjoy it, please consider donating to support the project. it costs ~$12USD/month to run, and your support helps keep the game running and ad-free. thank you for playing!
    </p>

    <Divider />

    <p class="mb-4">
        privacy information: cinemadle collects information provided during the sign-up process. cinemadle is transitioning to a model that will not require a model, and will remove the only required identifying information (email address) from the system and replace it with a sha256 hash of the email address.
    </p>

    <p class="mb-4">
        cinemadle may collect information about your device and browser for analytics purposes. this information is used for curiosity and may be made public in aggregate form, but will never be used to identify you personally. cinemadle does not sell or share your information with third parties.
    </p>

    <p class="mb-4">
        your game statistics (win rate, average guesses to completion, etc.) may be made available to you in a future update. accessing this information is limited to your specific browser information; your anonymous user id is randomly generated and stored in cookies. if you clear your cookies, you will lose access to this information, though it will persist on the server. there is no way to connect the server data to your specific browser; from our end, it is entirely anonymous. game information associated with your anonymous user id will, in a future update, be deleted after 30 days of inactivity.
    </p>

    <p class="mb-4">
        cinemadle reserves the right to change this privacy policy at any time. any and all user data is subject to deletion at any time. it is possible that your data may be deleted without notice, and cinemadle is not responsible for any loss of data. this may be the result of a bug, a server crash, the shutdown of the service, or any other reason. by using this site you agree to these terms.
    </p>

    <Divider />

    <p class="mb-4">code: will hodges</p>

    <p class="mb-4">design: will hodges and myke yunis</p>

    {#if $userStore.loggedIn}
        <div class="flex justify-center items-center mb-4">
            <Button
                onclick={() => {
                    userStore.setLoggedOut();
                }}
                data-testid="logout-button"
            >
                <p>log out</p>
            </Button>
        </div>
    {/if}
    <BuyMeAPizza />
</PageWrapper>
