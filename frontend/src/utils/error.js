/**
 * Normalize an API error (plain string or ASP.NET ProblemDetails object)
 * into a human-readable message.
 * @param {string|Object|null|undefined} error - Error from a service result envelope
 * @param {string} [fallback] - Message used when no detail can be extracted
 * @returns {string} - Display message
 */
export const extractErrorMessage = (error, fallback = "An unexpected error occurred. Please try again.") => {
  if (typeof error === "string") return error;

  const validationMessages = error?.errors ? Object.values(error.errors).flat() : [];
  if (validationMessages.length > 0) return validationMessages.join(" ");

  return error?.title || fallback;
};
