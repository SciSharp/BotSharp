import axios from 'axios';

/**
 * Set axios http headers globally
 * @param {string} token 
 */
export function setAuthorization(token) {
    let headers = axios.defaults.headers;
    headers.common['Authorization'] = `Bearer ${token}`;
}