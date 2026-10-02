const DEFAULT_MESSAGE = "An unexpected error occurred. Please try again.";

/**
 * Failure envelope returned by every service method.
 * `error` is always a display-ready string; the other fields are for branching.
 * @typedef {Object} ServiceFailure
 * @property {false} success
 * @property {string} error - Human-readable message, safe to render as-is
 * @property {number|null} status - HTTP status, or null when no response was received
 * @property {Record<string, string[]>} fieldErrors - Messages keyed by camelCase request member
 * @property {string[]} errorCodes - Stable machine-readable codes (e.g. "round.alreadyExists")
 */

const isPlainObject = (value) => value !== null && typeof value === "object" && !Array.isArray(value);

const isHtmlResponse = (response) => String(response?.headers?.["content-type"] ?? "").includes("text/html");

/**
 * Turn an API error body (plain string or RFC 9457 problem details) into a display message.
 * Problem `title` is deliberately ignored: it is the generic reason phrase ("Not Found"),
 * which is less helpful than the caller's contextual fallback.
 * @param {unknown} body - Response body
 * @param {string} fallback - Message used when the body carries no usable detail
 * @returns {string}
 */
const extractErrorMessage = (body, fallback) => {
  if (typeof body === "string") return body.trim() || fallback;
  if (!isPlainObject(body)) return fallback;

  const fieldMessages = isPlainObject(body.errors) ? Object.values(body.errors).flat() : [];
  const messages = [body.detail, ...fieldMessages].filter((m) => typeof m === "string" && m.length > 0);

  return messages.length > 0 ? messages.join(" ") : fallback;
};

/**
 * Normalize a failed request (usually an axios error) into a {@link ServiceFailure}.
 * This is the single place where backend error shapes are interpreted.
 * @param {unknown} error - The caught error
 * @param {string} [fallback] - Contextual message used when the response has no usable detail
 * @returns {ServiceFailure}
 */
export const toFailure = (error, fallback = DEFAULT_MESSAGE) => {
  const response = error?.response;
  // Proxy/gateway error pages (e.g. Caddy 502) are HTML and must never reach the UI.
  const body = isHtmlResponse(response) ? null : response?.data;
  const problem = isPlainObject(body) ? body : {};

  return {
    success: false,
    error: extractErrorMessage(body, fallback),
    status: response?.status ?? null,
    fieldErrors: isPlainObject(problem.errors) ? problem.errors : {},
    errorCodes: Array.isArray(problem.errorCodes) ? problem.errorCodes : [],
  };
};
