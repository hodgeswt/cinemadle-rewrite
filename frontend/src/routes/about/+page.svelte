<script lang="ts">
    import BuyMeAPizza from "$lib/ui/BuyMeAPizza.svelte";
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
        cinemadle is free to play, but if you enjoy it, please consider donating to support the project. it costs ~$12 USD/month to run, and your support helps keep the game running and ad-free. thank you for playing!
    </p>

    <Divider />

    <p class="mb-4">
        privacy information: cinemadle previously collected information provided during the sign-up process (email, password). cinemadle has transitioned to a model that no longer requires sign-up. the identifying information provided (email) has been anonymized by passing it through a one-way SHA256 hash function.
    </p>

    <p class="mb-4">
        cinemadle does not sell your information to third parties. using cinemadle anonymously results in storage of a randomly generated user id on the servers. it is impossible for us to connect this to you. clearing your browser data will generate a new cinemadle "identity". information associated with the anonymous user id may be used to generate statistics about cinemadle, including but not limited to users per month, average win rate, most common first guess, median winning guess number, ando thers.
    </p>

    <p class="mb-4">
        cinemadle reserves the right to change this privacy policy at any time. by using this site you agree to these terms.
    </p>

    <Divider />

    <p class="mb-4">code: will hodges</p>

    <p class="mb-4">design: will hodges and myke yunis</p>

    <BuyMeAPizza />
</PageWrapper>
