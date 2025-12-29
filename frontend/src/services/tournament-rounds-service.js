import api from "./api-interceptors.js";

export const tournamentRoundsService = {
  /**
   * Create a new round with answers
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} matchId - Match ID (GUID)
   * @param {Object} roundData - Round data
   * @param {number} roundData.numberInMatch - Round number (1-4)
   * @param {string} roundData.topicId - Topic ID (GUID)
   * @param {Array} roundData.answers - Array of answer objects
   * @returns {Promise} - API response with created round
   */
  async createRound(tournamentId, matchId, roundData) {
    try {
      const response = await api.post(`/Tournaments/${tournamentId}/matches/${matchId}/rounds`, roundData);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to create round. Please try again.",
      };
    }
  },

  /**
   * Update an existing round with answers
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} matchId - Match ID (GUID)
   * @param {number} roundNumber - Round number (1-4)
   * @param {Object} roundData - Round data
   * @param {number} roundData.numberInMatch - Round number (must match roundNumber)
   * @param {string} roundData.topicId - Topic ID (GUID)
   * @param {Array} roundData.answers - Array of answer objects
   * @returns {Promise} - API response with updated round
   */
  async updateRound(tournamentId, matchId, roundNumber, roundData) {
    try {
      const response = await api.put(
        `/Tournaments/${tournamentId}/matches/${matchId}/rounds/${roundNumber}`,
        roundData,
      );
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to update round. Please try again.",
      };
    }
  },

  /**
   * Delete a round
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} matchId - Match ID (GUID)
   * @param {number} roundNumber - Round number (1-4)
   * @returns {Promise} - API response
   */
  async deleteRound(tournamentId, matchId, roundNumber) {
    try {
      await api.delete(`/Tournaments/${tournamentId}/matches/${matchId}/rounds/${roundNumber}`);
      return { success: true };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to delete round. Please try again.",
      };
    }
  },
};
