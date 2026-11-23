import api from "./api-interceptors.js";
import { apiFailure } from "@/utils/error.js";

export const friendsService = {
  /**
   * Get the current user's whole friend list, not a page (ADR-0009)
   * @returns {Promise} - API response with friends sorted by username, each `{ accountId, username, addedAt }`
   */
  async getFriends() {
    try {
      const response = await api.get("/Friends");
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to fetch your friends. Please try again.");
    }
  },

  /**
   * Add an account to the current user's friend list
   * @param {string} username - Exact username of the account, in any case
   * @returns {Promise} - API response with the new friend, `{ accountId, username, addedAt }`
   */
  async addFriend(username) {
    try {
      const response = await api.post("/Friends", { username });
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to add the friend. Please try again.");
    }
  },

  /**
   * Remove an account from the current user's friend list
   * @param {string} accountId - Account ID of the friend (GUID)
   * @returns {Promise} - API response
   */
  async removeFriend(accountId) {
    try {
      await api.delete(`/Friends/${accountId}`);
      return { success: true };
    } catch (error) {
      return apiFailure(error, "Failed to remove the friend. Please try again.");
    }
  },
};
