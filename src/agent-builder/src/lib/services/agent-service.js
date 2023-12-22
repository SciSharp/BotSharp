import { getUserStore } from '$lib/helpers/store.js';
import { endpoints } from '$lib/services/api-endpoints.js';

export async function getAgents() {
    let user = getUserStore();
    const headers = {
        Authorization: `Bearer ${user.token}`,
    };

    const response = await fetch(endpoints.agentListUrl, {
        headers: headers
    }).then(response => {
        if (response.ok) {
            return response.json();
        } else {
            alert(response.statusText);
        }
    });
    return response;
}

/**
 * @param {string} id
 */
export async function getAgent(id) {
    let user = getUserStore();
    const headers = {
        Authorization: `Bearer ${user.token}`,
    };

    const response = await fetch(endpoints.agentDetailUrl.replace("{id}", id), {
        headers: headers
    }).then(response => {
        if (response.ok) {
            return response.json();
        } else {
            alert(response.statusText);
        }
    });
    return response;
}
