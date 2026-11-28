import api from "./api-interceptors.js";
import { apiFailure } from "@/utils/error.js";

export const tournamentMatchesService = {
  /**
   * Get all matches for a tournament
   * @param {string} tournamentId - Tournament ID (GUID)
   * @returns {Promise} - API response with matches list
   */
  async getMatches(tournamentId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}/matches`);
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to fetch matches. Please try again.");
    }
  },

  /**
   * Get match with full round details
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} matchId - Match ID (GUID)
   * @returns {Promise} - API response with match details including rounds
   */
  async getMatchWithRounds(tournamentId, matchId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}/matches/${matchId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to fetch match details. Please try again.");
    }
  },

  /**
   * Get available topics for a match (filtered by author conflicts and already played)
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} matchId - Match ID (GUID)
   * @returns {Promise} - API response with available topics list
   */
  async getAvailableTopics(tournamentId, matchId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}/matches/${matchId}/available-topics`);
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to fetch available topics. Please try again.");
    }
  },

  /**
   * Confirm a finished playoff match, shootout or Final: lock it and create the shootout or next matches its result calls for
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} matchId - Match ID (GUID)
   * @returns {Promise} - API response with the confirmed match, including rounds
   */
  async confirmMatch(tournamentId, matchId) {
    try {
      const response = await api.post(`/Tournaments/${tournamentId}/matches/${matchId}/confirm`);
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to confirm the match. Please try again.");
    }
  },

  /**
   * List the matches reopening a match would delete, in bracket order, with their round counts
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} matchId - Match ID (GUID)
   * @returns {Promise} - API response with the matches to delete
   */
  async getReopenDeletions(tournamentId, matchId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}/matches/${matchId}/reopen-deletions`);
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to fetch what reopening deletes. Please try again.");
    }
  },

  /**
   * Reopen a confirmed playoff match, shootout or Final: delete what was created from its result, then unlock it
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} matchId - Match ID (GUID)
   * @returns {Promise} - API response with the reopened match, including rounds
   */
  async reopenMatch(tournamentId, matchId) {
    try {
      const response = await api.post(`/Tournaments/${tournamentId}/matches/${matchId}/reopen`);
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to reopen the match. Please try again.");
    }
  },
};
