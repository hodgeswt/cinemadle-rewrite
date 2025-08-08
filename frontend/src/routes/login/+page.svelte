<script lang="ts">
    import { logIn } from "$lib/auth";
    import { Button } from "$lib/components/ui/button";
    import { Input } from "$lib/components/ui/input";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import { writable } from "svelte/store";
    import { userStore } from "$lib/stores";
    import type { Result } from "$lib/result";
    import { type LoginDto } from "$lib/dto";
    import { goto } from "$app/navigation";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";
    let openError = writable(false);
    let userEmail = $state("");
    let userPassword = $state("");
    let errorMessage = $state("");

    if ($userStore.loggedIn) {
        goto("/");
    }

    async function performLogIn(): Promise<void> {
        let result: Result<LoginDto> = await logIn(userEmail, userPassword);

        if (result.ok) {
            userStore.setLoggedIn(
                userEmail,
                `Bearer ${result.data!.accessToken}`,
                result.data!.refreshToken,
            );
            goto("/");
        } else {
            errorMessage = "Unable to log in";
            openError.set(true);
        }
    }

    function closeDialog() {
        openError.set(false);
        errorMessage = "";
    }
</script>

<PageWrapper>
    <Header />

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
        Log In
    </h2>

    <div class="flex flex-col space-y-2">
        <Input
            type="email"
            placeholder="example@example.com"
            bind:value={userEmail}
            class="text-base"
        />
        <Input
            type="password"
            placeholder="*****"
            class="text-base"
            bind:value={userPassword}
        />
        <Button type="submit" size="icon" onclick={performLogIn} class="w-full">
            <p class="m-1">Log In</p>
        </Button>
    </div>

    <AlertDialog.Root bind:open={$openError}>
        <AlertDialog.Content>
            <AlertDialog.Title>Uh-oh!</AlertDialog.Title>
            <AlertDialog.Description>
                {errorMessage}
            </AlertDialog.Description>
            <AlertDialog.Footer>
                <AlertDialog.Action on:click={closeDialog}>
                    Ok
                </AlertDialog.Action>
            </AlertDialog.Footer>
        </AlertDialog.Content>
    </AlertDialog.Root>
</PageWrapper>
