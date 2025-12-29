import api from "./api-interceptors.js";

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
      return {
        success: false,
        error: error.response?.data || "Failed to fetch matches. Please try again.",
      };
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
      return {
        success: false,
        error: error.response?.data || "Failed to fetch match details. Please try again.",
      };
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
      return {
        success: false,
        error: error.response?.data || "Failed to fetch available topics. Please try again.",
      };
    }
  },
};
