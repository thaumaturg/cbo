import { friendsService } from "@/services/friends-service.js";
import { toFormFailure } from "@/utils/error.js";
import { useNotify } from "@/utils/notify.js";
import { ref, watch } from "vue";

/**
 * State and submit for a form that adds a friend by username.
 * The server does every check except the blank one (unknown, yourself, already a friend).
 * @returns {{
 *   username: import("vue").Ref<string>,
 *   fieldError: import("vue").Ref<string>,
 *   banner: import("vue").Ref<string>,
 *   isAdding: import("vue").Ref<boolean>,
 *   add: () => Promise<{ accountId: string, username: string, addedAt: string } | null>,
 *   reset: () => void,
 * }} - `add` resolves to the new friend, or null when it wasn't added
 */
export function useAddFriend() {
  const notify = useNotify();

  const username = ref("");
  const fieldError = ref("");
  const banner = ref("");
  const isAdding = ref(false);

  const reset = () => {
    username.value = "";
    fieldError.value = "";
    banner.value = "";
  };

  const add = async () => {
    const trimmed = username.value.trim();
    banner.value = "";
    fieldError.value = "";

    if (!trimmed) {
      fieldError.value = "Username is required.";
      return null;
    }

    isAdding.value = true;

    try {
      const result = await friendsService.addFriend(trimmed);

      if (result.success) {
        reset();
        notify.success("Friend added", `"${result.data.username}" was added to your friend list.`);
        return result.data;
      }

      const failure = toFormFailure(result, ["username"]);
      banner.value = failure.banner;
      fieldError.value = failure.fieldErrors.username ?? "";
    } catch (error) {
      banner.value = "An unexpected error occurred. Please try again.";
      console.error("Error adding friend:", error);
    } finally {
      isAdding.value = false;
    }

    return null;
  };

  watch(username, () => {
    fieldError.value = "";
  });

  return { username, fieldError, banner, isAdding, add, reset };
}
