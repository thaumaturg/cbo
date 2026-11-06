import { nextTick } from "vue";

/**
 * Move focus to the first field marked invalid, in document order. Call it after a failed submit.
 * PrimeVue inputs render `aria-invalid="true"` while their `invalid` prop is set.
 * @param {Element | null | undefined} root - Form or other element holding the fields
 * @returns {Promise<void>} - Resolves once the DOM shows the new errors and focus has moved
 */
export const focusFirstInvalid = async (root) => {
  await nextTick();
  root?.querySelector('[aria-invalid="true"]')?.focus();
};
