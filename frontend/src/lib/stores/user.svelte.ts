import { hasValue } from '$lib/util';
import { writable } from 'svelte/store';
import { browser } from '$app/environment';
import { get as sget } from 'svelte/store';
import { guessStore } from './guessStore.svelte';

type UserStoreData = {
    email: string;
    jwt: string;
    refreshToken: string;
    loggedIn: boolean;
}

function isUserStoreData(obj: any): obj is UserStoreData {
    if (!hasValue(obj)) {
        return false;
    }

    return typeof obj.email === 'string'
        && typeof obj.jwt === 'string'
        && typeof obj.loggedIn === 'boolean'
        && typeof obj.refreshToken === 'string'
}

const createUserStore = () => {
    const localStorageKey = "cinemadleUserStore";
    const rawCached = browser && localStorage.getItem(localStorageKey) || null;
    const getCached = (): UserStoreData | null => {
        try {
            const j = JSON.parse(rawCached === null ? "" : rawCached);
            if (!isUserStoreData(j)) {
                return null;
            }

            return j as UserStoreData
        } catch {
            return null
        }
    }

    const userStoreCached = getCached();

    const defaultEmail = userStoreCached === null ? '' : userStoreCached.email;
    const defaultJwt = userStoreCached === null ? '' : userStoreCached.jwt;
    const defaultRefreshToken = userStoreCached === null ? '' : userStoreCached.jwt;
    const defaultLoggedIn = userStoreCached === null ? '' : userStoreCached.loggedIn;
    const { subscribe, set, update } = writable({
        email: defaultEmail,
        jwt: defaultJwt,
        loggedIn: defaultLoggedIn,
        refreshToken: defaultRefreshToken,
    });

    subscribe(user => {
        browser && localStorage.setItem("cinemadleUserStore", JSON.stringify(user));
    });

    return {
        subscribe,
        setLoggedIn: (email: string, jwt: string, refreshToken: string) => {
            guessStore.update(s => ({ ...s, guesses: [] }));
            return update(user => ({
                ...user,
                email,
                jwt,
                loggedIn: true,
                refreshToken,
            }));
        },
        setLoggedOut: () => {
            guessStore.update(s => ({ ...s, guesses: [] }));
            return update(_ => ({
                email: '',
                jwt: '',
                loggedIn: false,
                refreshToken: ''
            }));
        }
    }
}

export const userStore = createUserStore();
