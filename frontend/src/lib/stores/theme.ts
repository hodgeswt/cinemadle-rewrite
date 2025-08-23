import { writable } from 'svelte/store';

const isDarkMode = writable(typeof window !== 'undefined' ? localStorage.getItem('theme') === 'dark' : false);

if (typeof window !== 'undefined') {
    isDarkMode.subscribe(value => {
        localStorage.setItem('theme', value ? 'dark' : 'light');
        if (value) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    });
}

export { isDarkMode };