<script lang="ts">
    import { signUp } from "$lib/auth";
    import { Button } from "$lib/components/ui/button";
    import { Input } from "$lib/components/ui/input";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import { writable } from "svelte/store";
    import type { Result } from "$lib/result";
    import { type SignUpErrorDto } from "$lib/dto";
    import { goto } from "$app/navigation";
    import { userStore } from "$lib/stores";
    import Header from "$lib/ui/Header.svelte";
    import PageWrapper from "$lib/ui/PageWrapper.svelte";

    let openError = writable(false);
    let userEmail = $state("");
    let userPassword = $state("");
    let userConfirmPassword = $state("");
    let errorMessage = $state("");

    if ($userStore.loggedIn) {
        goto("/");
    }

    async function performSignUp(): Promise<void> {
        if (userPassword !== userConfirmPassword) {
            errorMessage = "Passwords do not match";
            openError.set(true);

            userPassword = "";
            userConfirmPassword = "";
            return;
        }

        let result: Result<SignUpErrorDto | null> = await signUp(
            userEmail,
            userPassword,
        );

        if (result.ok) {
            if (result.data === null) {
                goto("/login");
            } else {
                errorMessage = Object.values(result.data!.errors)
                    .flat()
                    .join("\n");
                openError.set(true);
            }
        }
    }

    function closeDialog() {
        openError.set(false);
        errorMessage = "";
    }
</script>

<PageWrapper>
    <Header />

    <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight" data-testid="page-title">
        sign up
    </h2>

    <p class="mb-4" data-testid="body1-text">
        email will be stored and may be provided to third parties. in the
        future, support will be added to remove your user account from our
        databases. this will be supported for all accounts created before this
        feature is added.
    </p>

    <div class="flex flex-col space-y-2">
        <Input
            type="email"
            class="text-base"
            placeholder="example@example.com"
            bind:value={userEmail}
            data-testid="email-input"
        />
        <Input
            type="password"
            placeholder="*****"
            class="text-base"
            bind:value={userPassword}
            data-testid="password-input"
        />
        <Input
            type="password"
            placeholder="*****"
            class="text-base"
            bind:value={userConfirmPassword}
            data-testid="passwordconfirm-input"
        />
        <Button
            type="submit"
            size="icon"
            onclick={performSignUp}
            class="w-full"
            data-testid="signup-button"
        >
            <p class="m-1">sign up</p>
        </Button>
    </div>

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
