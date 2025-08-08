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

<div class="p-4 flex justify-center min-h-screen">
    <div class="w-full lg:w-1/2 md:w-1/2 sm:w-full">
        <Header />

        <h2 class="mb-4 text-2xl font-semibold leading-none tracking-tight">
            Sign Up
        </h2>

        <p class="mb-4">
            Email will be stored and may be provided to third parties. In the
            future, support will be added to remove your user account from our
            databases. This will be supported for all accounts created before
            this feature is added.
        </p>

        <div class="flex flex-col space-y-2">
            <Input
                type="email"
                class="text-base"
                placeholder="example@example.com"
                bind:value={userEmail}
            />
            <Input
                type="password"
                placeholder="*****"
                class="text-base"
                bind:value={userPassword}
            />
            <Input
                type="password"
                placeholder="*****"
                class="text-base"
                bind:value={userConfirmPassword}
            />
            <Button
                type="submit"
                size="icon"
                onclick={performSignUp}
                class="w-full"
            >
                <p class="m-1">Sign Up</p>
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
    </div>
</div>
