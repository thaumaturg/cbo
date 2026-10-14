const DEFAULT_FALLBACK = "An unexpected error occurred. Please try again.";

const isNonEmptyString = (value) => typeof value === "string" && value.trim() !== "";

/**
 * Turn an error thrown by an API call into one readable message.
 * Every API error body is RFC 9457 problem details, so the message is built from,
 * in order: `detail` followed by every field-error message; else `title`; else the fallback.
 * Anything that isn't problem details (network failure, unexpected body) gives the fallback.
 * @param {unknown} error - Error thrown by the axios instance
 * @param {string} [fallback] - Message used when the response has nothing readable
 * @returns {string} - Display message
 */
export const getApiErrorMessage = (error, fallback = DEFAULT_FALLBACK) => {
  const problem = error?.response?.data;
  if (problem === null || typeof problem !== "object") return fallback;

  const fieldMessages =
    problem.errors && typeof problem.errors === "object"
      ? Object.values(problem.errors).flat().filter(isNonEmptyString)
      : [];

  const parts = isNonEmptyString(problem.detail) ? [problem.detail, ...fieldMessages] : fieldMessages;
  if (parts.length > 0) return parts.join(" ");

  return isNonEmptyString(problem.title) ? problem.title : fallback;
};

/**
 * Build the failure envelope every service method returns.
 * @param {unknown} error - Error thrown by the axios instance
 * @param {string} [fallback] - Message used when the response has nothing readable
 * @returns {{ success: false, error: string }}
 */
export const apiFailure = (error, fallback = DEFAULT_FALLBACK) => ({
  success: false,
  error: getApiErrorMessage(error, fallback),
});
