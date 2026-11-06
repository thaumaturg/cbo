const DEFAULT_FALLBACK = "An unexpected error occurred. Please try again.";
const NETWORK_ERROR = "Can't reach the server. Check your connection and try again.";

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
 * Turn an error thrown by an API call into its form-level message, leaving out field errors.
 * Every API error body is RFC 9457 problem details, so the message is, in order:
 * `detail`; else `title`; else the fallback. A request that got no response at all gives the
 * network message; any other body that isn't problem details gives the fallback.
 * @param {unknown} error - Error thrown by the axios instance
 * @param {string} [fallback] - Message used when the response has nothing readable
 * @returns {string} - Display message
 */
export const getApiErrorMessage = (error, fallback = DEFAULT_FALLBACK) => {
  if (error?.isAxiosError && !error.response) return NETWORK_ERROR;

  const problem = error?.response?.data;
  if (problem === null || typeof problem !== "object") return fallback;

  if (isNonEmptyString(problem.detail)) return problem.detail;
  return isNonEmptyString(problem.title) ? problem.title : fallback;
};

/**
 * Build the failure envelope every service method returns.
 * - `error` is the form-level message only (see `getApiErrorMessage`); forms show it through
 *   `toFormFailure`, which decides whether a banner is needed at all.
 * - `message` is `error` followed by every field message, for callers that only have a toast,
 *   a load-error banner or the console and no fields of their own.
 * - `errors` lets forms show each field's messages next to that field.
 * - `status` is the HTTP status, or undefined when no response arrived.
 * @param {unknown} error - Error thrown by the axios instance
 * @param {string} [fallback] - Message used when the response has nothing readable
 * @returns {{ success: false, error: string, message: string, errors: Record<string, string[]>, status: number | undefined }}
 */
export const apiFailure = (error, fallback = DEFAULT_FALLBACK) => {
  const formMessage = getApiErrorMessage(error, fallback);
  const errors = getApiFieldErrors(error);

  return {
    success: false,
    error: formMessage,
    message: [formMessage, ...Object.values(errors).flat()].join(" "),
    errors,
    status: error?.response?.status,
  };
};

/**
 * Build a failure envelope for a failure the client detects itself, with no field errors.
 * @param {string} message - Display message
 * @returns {{ success: false, error: string, message: string, errors: {} }}
 */
export const clientFailure = (message) => ({ success: false, error: message, message, errors: {} });

const toFieldPairs = (fieldNames) =>
  Array.isArray(fieldNames) ? fieldNames.map((name) => [name, name]) : Object.entries(fieldNames);

/**
 * Pick the field errors a form shows next to its fields, keyed by the form's field names.
 * API fields the form doesn't name are left out; `toFormFailure` puts their messages in the banner.
 * @param {Record<string, string[]>} errors - Field errors from the failure envelope
 * @param {string[] | Record<string, string>} fieldNames - API field names the form uses as they are,
 *   or a map from API field name to form field name
 * @returns {Record<string, string>} - One message per form field, ready for VeeValidate's `setErrors`
 */
export const toFormErrors = (errors, fieldNames) =>
  Object.fromEntries(
    toFieldPairs(fieldNames)
      .filter(([apiField]) => errors?.[apiField]?.length > 0)
      .map(([apiField, formField]) => [formField, errors[apiField].join(" ")]),
  );

/**
 * Split a failure envelope into what a form shows under its fields and what it shows in its banner.
 * The banner never repeats a message shown under a field:
 * - without field errors, the banner is the form-level message (`error`);
 * - with field errors, `error` only summarises them, so the banner holds just the messages of
 *   API fields the form doesn't show, and is empty when every message is shown under a field.
 * @param {{ error: string, errors?: Record<string, string[]> }} failure - Failure envelope
 * @param {string[] | Record<string, string>} fieldNames - As for `toFormErrors`
 * @returns {{ fieldErrors: Record<string, string>, banner: string }} - `banner` is "" when
 *   the form should show none
 */
export const toFormFailure = (failure, fieldNames) => {
  const errors = failure?.errors ?? {};
  const shownFields = new Set(toFieldPairs(fieldNames).map(([apiField]) => apiField));
  const otherMessages = Object.entries(errors)
    .filter(([apiField]) => !shownFields.has(apiField))
    .flatMap(([, messages]) => messages);

  return {
    fieldErrors: toFormErrors(errors, fieldNames),
    banner: Object.keys(errors).length > 0 ? otherMessages.join(" ") : (failure?.error ?? ""),
  };
};
