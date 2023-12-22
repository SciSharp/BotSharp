import { writable } from 'svelte/store';

function isBrowser() {
    return typeof window !== 'undefined';
}

const initValue = { init: true, loggedIn: false, token: '' };
export const userStore = writable(initValue);

export function getUserStore () {
    if (isBrowser()) {
        // Access localStorage only if in the browser context
        let json = localStorage.getItem('user');
        return JSON.parse(json) || initValue;
    } else {
        // Return a default value for SSR
        return initValue;
    }
};

userStore.subscribe(value => {
    if (isBrowser()) {
        if(!value.init) {
            localStorage.setItem('user', JSON.stringify(value));
        }
    }
});