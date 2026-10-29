import type { Snippet } from 'svelte';

export type NullFunc = () => void;

export type OpenDialogRequest = {
    dialogId: string;
    titleText: string;
    body: Snippet;
    confirmButtonText?: string;
    cancelButtonText?: string;
    cancelCallback?: NullFunc;
};