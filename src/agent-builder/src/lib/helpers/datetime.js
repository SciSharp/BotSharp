/**
 * @param {Date} datetime
 * @param {string} type - date or time
 */
export function format(datetime, type = 'date') {
    const date = new Date(datetime);
    if (type == 'date') {
        const options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
        return date.toLocaleString(undefined, options);
    } else if (type == 'time') {
        const options = { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' };
        return date.toLocaleString(undefined, options);
    } else if (type == 'short-time') {
        const options = { hour: '2-digit', minute: '2-digit' };
        return date.toLocaleString(undefined, options);
    }
};