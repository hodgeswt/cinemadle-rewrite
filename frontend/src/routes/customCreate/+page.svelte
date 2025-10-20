<script lang="ts">
	import { browser } from "$app/environment";
	import { goto } from "$app/navigation";
	import { Button } from "$lib/components/ui/button";
	import { Input } from "$lib/components/ui/input";
	import { Skeleton } from "$lib/components/ui/skeleton";
	import { createCustomGame, PING_LIMIT } from "$lib/middleware";
	import { Container, type IGuessService } from "$lib/services";
	import { userStore } from "$lib/stores";
	import { isDarkMode } from "$lib/stores/theme";
	import Header from "$lib/ui/Header.svelte";
	import PageWrapper from "$lib/ui/PageWrapper.svelte";
	import Logger from "$lib/logger";
	import { onMount } from "svelte";
	import * as AlertDialog from "$lib/components/ui/alert-dialog";
	import { writable } from "svelte/store";

	const openError = writable(false);

	let errorMessage = $state("");
	let successMessage = $state("");
	let movieSearch = $state("");
	let filteredMovies = $state([] as string[]);
	let possibleMovies = $state({} as { [key: string]: number });
	let selectedMovieTitle = $state("");
	let showSuggestions = $state(false);
	let loading = $state(true);
	let serverDown = $state(false);
	let creating = $state(false);
	let pingCount = 0;

	const suggestionsLimit = 10;
	const guessService = (): IGuessService => Container.it().GuessService;
	const topSuggestions = () =>
		Object.keys(possibleMovies)
			.sort((a, b) => a.localeCompare(b))
			.slice(0, suggestionsLimit);

	onMount(async () => {
		if (!$userStore.loggedIn) {
			if (browser) {
				goto("/");
			}
			return;
		}

		while (!guessService().isInitialized()) {
			if (pingCount === PING_LIMIT) {
				serverDown = true;
				loading = false;
				return;
			}

			pingCount += 1;
			await new Promise((resolve) => setTimeout(resolve, 1000));
		}

		possibleMovies = guessService().possibleGuesses();
		const titles = Object.keys(possibleMovies).sort((a, b) => a.localeCompare(b));
		filteredMovies = titles.slice(0, suggestionsLimit);
		loading = false;

		if (titles.length === 0) {
			serverDown = true;
		}
	});

	function handleInput(event: Event): void {
		const value = (event.target as HTMLInputElement).value;
		movieSearch = value;
		filterMovies(value);
	}

	function handleFocus(): void {
		if (movieSearch.trim() !== "") {
			showSuggestions = filteredMovies.length > 0;
		}
	}

	function handleBlur(): void {
		setTimeout(() => {
			showSuggestions = false;
		}, 150);
	}

	function filterMovies(value: string): void {
		if (!value.trim()) {
			filteredMovies = topSuggestions();
			showSuggestions = false;
			selectedMovieTitle = "";
			return;
		}

		const lower = value.toLowerCase().trim();
		const matches = Object.keys(possibleMovies)
			.filter((title) => title.toLowerCase().includes(lower))
			.sort((a, b) => a.localeCompare(b))
			.slice(0, suggestionsLimit);

		filteredMovies = matches;
		showSuggestions = matches.length > 0;
		if (!matches.includes(selectedMovieTitle)) {
			selectedMovieTitle = "";
		}
	}

	function selectMovie(title: string): void {
		selectedMovieTitle = title;
		movieSearch = title;
		showSuggestions = false;
	}

	function selectRandomMovie(): void {
		const titles = Object.keys(possibleMovies);
		if (titles.length === 0) {
			return;
		}

		const randomTitle = titles[Math.floor(Math.random() * titles.length)];
		selectMovie(randomTitle);
		filteredMovies = topSuggestions();
	}

	function resolveSelection(): { id: number | null; title: string | null } {
		if (selectedMovieTitle && possibleMovies[selectedMovieTitle] !== undefined) {
			return { id: possibleMovies[selectedMovieTitle], title: selectedMovieTitle };
		}

		const trimmed = movieSearch.trim();
		if (!trimmed) {
			return { id: null, title: null };
		}

		const exactMatch = Object.keys(possibleMovies).find(
			(title) => title.toLowerCase() === trimmed.toLowerCase(),
		);

		if (exactMatch && possibleMovies[exactMatch] !== undefined) {
			selectedMovieTitle = exactMatch;
			movieSearch = exactMatch;
			return { id: possibleMovies[exactMatch], title: exactMatch };
		}

		return { id: null, title: null };
	}

	function parseError(error: string | undefined | null): string {
		if (!error) {
			return "unable to create custom game. try again later.";
		}

		try {
			const parsed = JSON.parse(error);
			if (typeof parsed === "string") {
				return parsed;
			}

			if (parsed && typeof parsed.title === "string") {
				return parsed.title;
			}

			if (parsed && typeof parsed.detail === "string") {
				return parsed.detail;
			}
		} catch {
			return error;
		}

		return "unable to create custom game. try again later.";
	}

	async function submitCustomGame(event: SubmitEvent): Promise<void> {
		event.preventDefault();

		successMessage = "";
		const { id, title } = resolveSelection();

		if (id === null || title === null) {
			errorMessage = "select a movie from the list before continuing.";
			openError.set(true);
			return;
		}

		creating = true;

		const result = await createCustomGame(id, $userStore.jwt);

		creating = false;

		if (!result.ok) {
			Logger.log("customCreate.submitCustomGame: failed to create custom game {0}", result.error);
			errorMessage = parseError(result.error);
			openError.set(true);
			return;
		}

		successMessage = `${title} is ready as a custom game.`;
		selectedMovieTitle = "";
		movieSearch = "";
		filteredMovies = topSuggestions();
	}

	function closeDialog(): void {
		openError.set(false);
		errorMessage = "";
	}
</script>

<PageWrapper>
	<Header />

	<h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="page-title">
		create a custom game
	</h2>

	<p class="mb-4" data-testid="body1-text">
		pick a movie from the cinemadle catalog to generate a private custom game for your friends.
	</p>

	{#if serverDown}
		<div class="mt-4 p-4 border border-red-200 bg-red-50 text-red-600 rounded-lg" data-testid="customcreate-error">
			unable to load the movie list right now. please try again later.
		</div>
	{:else if loading}
		<div class="space-y-3 w-full">
			<Skeleton class="h-12 w-full" />
			<Skeleton class="h-10 w-32" />
		</div>
	{:else}
		<form class="space-y-6" on:submit|preventDefault={submitCustomGame}>
			<div class="relative">
				<label for="movie-search" class="block text-sm font-medium mb-1">
					movie title
				</label>
				<Input
					id="movie-search"
					type="text"
					placeholder="Search for a movie"
					bind:value={movieSearch}
					on:input={handleInput}
					on:focus={handleFocus}
					on:blur={handleBlur}
					autocomplete="off"
					class="text-base"
					data-testid="customcreate-search-input"
				/>

				{#if showSuggestions && filteredMovies.length > 0}
					<ul
						class="mt-1 max-h-60 overflow-y-auto border {$isDarkMode ? 'border-gray-700 bg-gray-900' : 'border-gray-300 bg-white'} rounded shadow-lg absolute w-full z-[9999999]"
					>
						{#each filteredMovies as title}
							<li class="p-2 text-sm">
								<button
									type="button"
									class="w-full text-left hover:{$isDarkMode ? 'bg-gray-800' : 'bg-gray-100'} rounded"
									on:click={() => selectMovie(title)}
									data-testid={`customcreate-suggestion-${title.replaceAll(" ", "-")}`}
								>
									{title}
								</button>
							</li>
						{/each}
					</ul>
				{/if}
			</div>

			{#if selectedMovieTitle !== ""}
				<div
					class="p-3 rounded border {$isDarkMode ? 'border-emerald-700 bg-emerald-900 text-emerald-100' : 'border-emerald-200 bg-emerald-50 text-emerald-700'}"
					data-testid="customcreate-selection"
				>
					selected movie: {selectedMovieTitle}
				</div>
			{/if}

            <Button
				type="button"
				variant="secondary"
				size="icon"
				class="w-full"
				on:click={selectRandomMovie}
				data-testid="customcreate-random"
			>
				<p class="m-1">pick a random movie</p>
			</Button>

			<Button
				type="submit"
				size="icon"
				class="w-full"
				disabled={creating}
				data-testid="customcreate-submit"
			>
				<p class="m-1">{creating ? "creating..." : "create custom game"}</p>
			</Button>
		</form>
	{/if}

	{#if successMessage !== ""}
		<div
			class="mt-6 p-4 border {$isDarkMode ? 'border-green-700 bg-green-900 text-green-100' : 'border-green-200 bg-green-50 text-green-700'} rounded-lg"
			data-testid="customcreate-success"
		>
			{successMessage}
		</div>
	{/if}

	<AlertDialog.Root bind:open={$openError}>
		<AlertDialog.Content>
			<AlertDialog.Title data-testid="error-title-text">uh-oh!</AlertDialog.Title>
			<AlertDialog.Description data-testid="error-body-text">
				{errorMessage}
			</AlertDialog.Description>
			<AlertDialog.Footer>
				<AlertDialog.Action on:click={closeDialog} data-testid="error-ok-button">
					ok
				</AlertDialog.Action>
			</AlertDialog.Footer>
		</AlertDialog.Content>
	</AlertDialog.Root>
</PageWrapper>
