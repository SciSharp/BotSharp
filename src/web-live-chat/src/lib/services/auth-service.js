import { userStore, getUserStore } from '$lib/helpers/store.js';
import { tokenUrl,  usrCreationUrl } from './api-endpoints.js';

/**
 * This callback type is called `requestCallback` and is displayed as a global symbol.
 *
 * @callback loginSucceed
 * @param {string} token
 */

/**
 * @param {string} email
 * @param {string} password
 * @param {loginSucceed} onSucceed
 */
export async function getToken(email, password, onSucceed) {
    const credentials = btoa(`${email}:${password}`);
    const headers = {
        Authorization: `Basic ${credentials}`,
    };

    await fetch(tokenUrl, {
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
        onSucceed(result.access_token);
    })
    .catch(error => alert(error.message));
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

    await fetch(usrCreationUrl, {
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
