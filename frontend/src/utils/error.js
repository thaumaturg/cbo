const DEFAULT_FALLBACK = "An unexpected error occurred. Please try again.";

const isNonEmptyString = (value) => typeof value === "string" && value.trim() !== "";

/**
 * Read the field errors of an error thrown by an API call.
 * Keys are the camelCase JSON paths the API sends (for example `username` or `questions[0].text`).
 * @param {unknown} error - Error thrown by the axios instance
 * @returns {Record<string, string[]>} - Non-empty messages per field; empty when there are none
 */
const getApiFieldErrors = (error) => {
  const errors = error?.response?.data?.errors;
  if (errors === null || typeof errors !== "object" || Array.isArray(errors)) return {};

  const fieldErrors = {};
  for (const [field, messages] of Object.entries(errors)) {
    const valid = [messages].flat().filter(isNonEmptyString);
    if (valid.length > 0) fieldErrors[field] = valid;
  }
  return fieldErrors;
};

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

  const fieldMessages = Object.values(getApiFieldErrors(error)).flat();

  const parts = isNonEmptyString(problem.detail) ? [problem.detail, ...fieldMessages] : fieldMessages;
  if (parts.length > 0) return parts.join(" ");

  return isNonEmptyString(problem.title) ? problem.title : fallback;
};

/**
 * Build the failure envelope every service method returns.
 * `error` is the whole message, for toasts and form-level messages; `errors` lets forms
 * show each field's messages next to that field.
 * @param {unknown} error - Error thrown by the axios instance
 * @param {string} [fallback] - Message used when the response has nothing readable
 * @returns {{ success: false, error: string, errors: Record<string, string[]> }}
 */
export const apiFailure = (error, fallback = DEFAULT_FALLBACK) => ({
  success: false,
  error: getApiErrorMessage(error, fallback),
  errors: getApiFieldErrors(error),
});

/**
 * Pick the field errors a form shows next to its fields, keyed by the form's field names.
 * API fields the form doesn't name are left out; their messages are still in the envelope's `error`.
 * @param {Record<string, string[]>} errors - Field errors from the failure envelope
 * @param {string[] | Record<string, string>} fieldNames - API field names the form uses as they are,
 *   or a map from API field name to form field name
 * @returns {Record<string, string>} - One message per form field, ready for VeeValidate's `setErrors`
 */
export const toFormErrors = (errors, fieldNames) => {
  const pairs = Array.isArray(fieldNames) ? fieldNames.map((name) => [name, name]) : Object.entries(fieldNames);

  return Object.fromEntries(
    pairs
      .filter(([apiField]) => errors?.[apiField]?.length > 0)
      .map(([apiField, formField]) => [formField, errors[apiField].join(" ")]),
  );
};
