import api from "./api-interceptors.js";

export const tournamentTopicsService = {
  /**
   * Get current user's topics for a tournament (for players)
   * @param {string} tournamentId - Tournament ID (GUID)
   * @returns {Promise} - API response with user's topics list
   */
  async getMyTopics(tournamentId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}/topics`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch topics. Please try again.",
      };
    }
  },

  /**
   * Get all topics for a tournament (for organizers/creators)
   * @param {string} tournamentId - Tournament ID (GUID)
   * @returns {Promise} - API response with all topics list
   */
  async getAllTopics(tournamentId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}/topics/all`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch all topics. Please try again.",
      };
    }
  },

  /**
   * Set/replace current user's topics for a tournament
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {Array} topics - Array of topic objects with topicId and priorityIndex
   * @returns {Promise} - API response with updated topics list
   */
  async setMyTopics(tournamentId, topics) {
    try {
      const response = await api.put(`/Tournaments/${tournamentId}/topics`, topics);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to update topics. Please try again.",
      };
    }
  },

  /**
   * Get topic with questions for a tournament (allows access to topics you don't own)
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} topicId - Topic ID (GUID)
   * @returns {Promise} - API response with topic details including questions
   */
  async getTournamentTopic(tournamentId, topicId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}/topics/${topicId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch topic. Please try again.",
      };
    }
  },
};
