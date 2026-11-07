import { useConfirm } from "primevue/useconfirm";
import { onBeforeUnmount, onMounted, toValue } from "vue";
import { onBeforeRouteLeave, onBeforeRouteUpdate } from "vue-router";

/**
 * Ask before unsaved changes are lost: when leaving or updating the route, and when closing or
 * reloading the browser tab. Call it from the `setup` of a component rendered by a route.
 * Clear the dirty state before navigating on purpose (for example after a save that replaces the
 * route), or the user is asked anyway.
 * @param {import("vue").MaybeRefOrGetter<boolean>} isDirty - Whether there are unsaved changes
 * @returns {{ confirmDiscard: () => Promise<boolean> }} - `confirmDiscard` asks the same question
 *   when dirty and resolves `true` at once when clean, for in-page switches such as tabs
 */
export function useUnsavedChangesGuard(isDirty) {
  const confirm = useConfirm();

  // Settles the open prompt, if any. A newer prompt replaces it.
  let settleOpenPrompt = null;

  const confirmDiscard = () => {
    if (!toValue(isDirty)) {
      return Promise.resolve(true);
    }

    settleOpenPrompt?.(false);

    return new Promise((resolve) => {
      const settle = (discard) => {
        if (settleOpenPrompt !== settle) {
          return;
        }
        settleOpenPrompt = null;
        resolve(discard);
      };
      settleOpenPrompt = settle;

      confirm.require({
        header: "Unsaved Changes",
        message: "You have unsaved changes. Leave and discard them?",
        icon: "pi pi-exclamation-triangle",
        rejectProps: { label: "Keep Editing", severity: "secondary", outlined: true },
        acceptProps: { label: "Discard Changes", severity: "danger" },
        accept: () => settle(true),
        reject: () => settle(false),
        // Closing the dialog with Escape or its close button.
        onHide: () => settle(false),
      });
    });
  };

  onBeforeRouteLeave(() => confirmDiscard());
  onBeforeRouteUpdate(() => confirmDiscard());

  const onBeforeUnload = (event) => {
    if (toValue(isDirty)) {
      event.preventDefault();
    }
  };

  onMounted(() => window.addEventListener("beforeunload", onBeforeUnload));
  onBeforeUnmount(() => window.removeEventListener("beforeunload", onBeforeUnload));

  return { confirmDiscard };
}
