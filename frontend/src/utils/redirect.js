/**
 * Pick where to go after logging in or registering. Only internal paths are followed, so a
 * crafted `?redirect=//evil.example` link can't send the user off-site.
 * @param {unknown} redirect - The `redirect` query value
 * @returns {string} - The redirect path, or "/" when it isn't a safe internal path
 */
export const safeRedirect = (redirect) => {
  const isInternalPath =
    typeof redirect === "string" &&
    redirect.startsWith("/") &&
    !redirect.startsWith("//") &&
    !redirect.startsWith("/\\");

  return isInternalPath ? redirect : "/";
};

/**
 * Keep the `redirect` query when moving between the login and register pages.
 * @param {Record<string, unknown>} query - The current route's query
 * @returns {Record<string, string>} - A query with just `redirect`, or an empty one
 */
export const keepRedirect = (query) => (typeof query.redirect === "string" ? { redirect: query.redirect } : {});
