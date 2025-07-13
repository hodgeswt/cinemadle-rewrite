import { writable } from 'svelte/store';

const createUserStore = () => {
    const { subscribe, set, update } = writable({
        email: '',
        jwt: '',
        loggedIn: false,
    });

    return {
        subscribe,
        setLoggedIn: (email: string, jwt: string) => update(user => ({
            ...user,
            email,
            jwt,
            loggedIn: true
        })),
        setLoggedOut: () => update(_ => ({
            email: '',
            jwt: '',
            loggedIn: false
        }))
    }
}

export const userStore = createUserStore();
