import axios from 'axios';
import { getUserStore } from '$lib/helpers/store.js';

/**
 * Set axios http headers globally
 */
export function setAuthorization() {
    let user = getUserStore();
    let headers = axios.defaults.headers;
    headers.common['Authorization'] = `Bearer ${user.token}`;
}