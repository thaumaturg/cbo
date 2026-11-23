/**
 * Whether two usernames name the same account. The server matches usernames ignoring case.
 * @param {string|null|undefined} a - Username
 * @param {string|null|undefined} b - Username
 * @returns {boolean}
 */
export const sameUsername = (a, b) => a?.toLowerCase() === b?.toLowerCase();

/**
 * Compare two rows by username, ignoring case, for `Array.prototype.sort`.
 * @param {{ username: string }} a - Row
 * @param {{ username: string }} b - Row
 * @returns {number}
 */
export const byUsername = (a, b) => a.username.localeCompare(b.username, undefined, { sensitivity: "base" });
