import { userStore, getUserStore } from '$lib/helpers/store.js';
import { setAuthorization } from '$lib/helpers/http';
import { endpoints } from './api-endpoints.js';
import axios from 'axios';

/**
 * @param {string} email
 * @param {string} password
 * @param {function} onSucceed()
 */
export async function getToken(email, password, onSucceed) {
    const credentials = btoa(`${email}:${password}`);
    const headers = {
        Authorization: `Basic ${credentials}`,
    };

    await fetch(endpoints.tokenUrl, {
        method: 'POST',
        headers: headers,
    }).then(response => {
        if (response.ok) {
            return response.json();
        } else {
            alert(response.statusText);
        }
    }).then(result => {
        let user = getUserStore();
        userStore.set({ ...user, init: false, loggedIn: true, email, token: result.access_token });
        onSucceed();
    })
    .catch(error => alert(error.message));
}

/**
 * Set token from exteranl
 * @param {string} token
 */
export function setToken(token) {
    let user = getUserStore();
    userStore.set({ ...user, init: false, loggedIn: true, token: token });
}

/**
 * @returns {Promise<import('$types').UserModel>}
 */
export async function myInfo() {
    setAuthorization();
    const response = await axios.get(endpoints.myInfoUrl);
    return response.data;
}

/**
 * @param {string} firstName
 * @param {string} lastName
 * @param {string} email
 * @param {string} password
 * @param {function} onSucceed()
 */
export async function register(firstName, lastName, email, password, onSucceed) {
    let data = JSON.stringify({
        firstName,
        lastName,
        email,
        password
    });

    await fetch(endpoints.usrCreationUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: data
    })
    .then(result => {
        if (result.ok) {
            onSucceed();
        } else {
            alert(result.statusText);
        }
    })
    .catch(error => alert(error.message));
}