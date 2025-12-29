import api from "./api-interceptors.js";

export const tournamentParticipantsService = {
  /**
   * Get all participants for a tournament
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} [role] - Optional role filter
   * @returns {Promise} - API response with participants list
   */
  async getAllParticipants(tournamentId, role = null) {
    try {
      const params = role ? { role } : {};
      const response = await api.get(`/Tournaments/${tournamentId}/participants`, { params });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch participants. Please try again.",
      };
    }
  },

  /**
   * Get participant by ID
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} participantId - Participant ID (GUID)
   * @returns {Promise} - API response with participant details
   */
  async getParticipantById(tournamentId, participantId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}/participants/${participantId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch participant details. Please try again.",
      };
    }
  },

  /**
   * Add a participant to a tournament
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {Object} participantData - Participant data
   * @param {string} participantData.username - Username of the participant
   * @param {string} participantData.role - Participant role
   * @returns {Promise} - API response with created participant
   */
  async createParticipant(tournamentId, participantData) {
    try {
      const response = await api.post(`/Tournaments/${tournamentId}/participants`, {
        username: participantData.username,
        role: participantData.role,
      });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to add participant. Please try again.",
      };
    }
  },

  /**
   * Update participant role
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} participantId - Participant ID (GUID)
   * @param {Object} participantData - Updated participant data
   * @param {string} participantData.role - Participant role
   * @returns {Promise} - API response with updated participant
   */
  async updateParticipant(tournamentId, participantId, participantData) {
    try {
      const response = await api.put(`/Tournaments/${tournamentId}/participants/${participantId}`, {
        role: participantData.role,
      });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to update participant. Please try again.",
      };
    }
  },

  /**
   * Remove a participant from a tournament
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} participantId - Participant ID (GUID)
   * @returns {Promise} - API response
   */
  async deleteParticipant(tournamentId, participantId) {
    try {
      await api.delete(`/Tournaments/${tournamentId}/participants/${participantId}`);
      return { success: true };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to remove participant. Please try again.",
      };
    }
  },
};
