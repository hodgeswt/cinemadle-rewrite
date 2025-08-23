import type { Writable } from "svelte/store"

export type DialogProps = {
    children?: any | undefined,
    open: Writable<boolean>,
    title: string,
    id: string,
    confirmButton: string,
    cancelButton?: string | undefined,
    confirmCallback?: Function | undefined,
    cancelCallback?: Function | undefined
}