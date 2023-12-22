import { getUserStore } from '$lib/helpers/store.js';
import { pluginListUrl } from '$lib/services/api-endpoints.js';

/**
 * Get plugin list
 * @returns {Promise<import('$types').PluginDefModel[]>}
 */
export async function GetPlugins() {
    let user = getUserStore();
    const headers = {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${user.token}`,
    };

    const response = await fetch(pluginListUrl, {
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
