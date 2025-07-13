<script lang="ts">
    import { logIn, signUp } from "$lib/auth";
    import { Button } from "$lib/components/ui/button";
    import { Input } from "$lib/components/ui/input";
    import * as AlertDialog from "$lib/components/ui/alert-dialog";
    import { writable } from "svelte/store";
    import { userStore } from "$lib/stores";
    import type { Result } from "$lib/result";
    import { type LoginDto, type SignUpErrorDto } from "$lib/dto";
    import { goto } from "$app/navigation";
    let openError = writable(false);
    let userEmail = $state("");
    let userPassword = $state("");
    let userConfirmPassword = $state("");
    let errorMessage = $state("");

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
    <div class="w-full lg:w-1/2 md:w-1/2 sm:w-full p-4">
        <div class="w-full flex justify-between items-center">
            <h1
                class="flex-1 m-4 text-4xl font-extrabold leading-none tracking-tight"
            >
                cinemadle
            </h1>
            <div
                class="w-full flex-1 flex flex-col m-4 text-right justify-center"
            >
                <a href="/login" class="underline">Login</a>
                <a href="/about" class="underline">About</a>
                <a href="/" class="underline">Home</a>
            </div>
        </div>

        <h2 class="m-4 text-2xl font-semibold leading-non tracking-tight">
            Sign Up
        </h2>

        <p class="m-4">
            Email will be stored and may be provided to third parties. In the
            future, support will be added to remove your user account from our
            databases. This will be supported for all accounts created before
            this feature is added.
        </p>

        <div class="flex justify-center items-center">
            <hr
                class="my-12 h-0.5 w-1/2 border-t-0 bg-gray-300 dark:bg-white/10"
            />
        </div>

        <div class="flex flex-col">
            <Input
                type="email"
                placeholder="example@example.com"
                bind:value={userEmail}
                class="m-1"
            />
            <Input
                type="password"
                placeholder="*****"
                bind:value={userPassword}
                class="m-1"
            />
            <Input
                type="password"
                placeholder="*****"
                bind:value={userConfirmPassword}
                class="m-1"
            />
            <Button
                type="submit"
                size="icon"
                onclick={performSignUp}
                class="m-1 flex-grow w-full"
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
