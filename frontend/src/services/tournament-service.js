import api from "./api-interceptors.js";

export const tournamentService = {
  /**
   * Get all tournaments for the current user
   * @returns {Promise} - API response with user's tournaments
   */
  async getAllTournaments() {
    try {
      const response = await api.get("/Tournaments");
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch tournaments. Please try again.",
      };
    }
  },

  /**
   * Get tournament by ID
   * @param {string} tournamentId - Tournament ID (GUID)
   * @returns {Promise} - API response with tournament details
   */
  async getTournamentById(tournamentId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch tournament details. Please try again.",
      };
    }
  },

  /**
   * Create a new tournament
   * @param {Object} tournamentData - Tournament data
   * @param {string} tournamentData.title - Tournament title
   * @param {string} [tournamentData.description] - Tournament description (optional)
   * @param {number} [tournamentData.playersPerTournament] - Maximum players in tournament (optional)
   * @param {number} [tournamentData.topicsPerParticipantMax] - Maximum topics per participant (optional)
   * @param {number} [tournamentData.topicsPerParticipantMin] - Minimum topics per participant (optional)
   * @returns {Promise} - API response with created tournament
   */
  async createTournament(tournamentData) {
    try {
      const response = await api.post("/Tournaments", {
        title: tournamentData.title,
        description: tournamentData.description,
        playersPerTournament: tournamentData.playersPerTournament,
        topicsPerParticipantMax: tournamentData.topicsPerParticipantMax,
        topicsPerParticipantMin: tournamentData.topicsPerParticipantMin,
      });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to create tournament. Please try again.",
      };
    }
  },

  /**
   * Update tournament
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {Object} tournamentData - Updated tournament data
   * @param {string} tournamentData.title - Tournament title
   * @param {string} [tournamentData.description] - Tournament description (optional)
   * @param {number} [tournamentData.playersPerTournament] - Maximum players in tournament (optional)
   * @param {number} [tournamentData.topicsPerParticipantMax] - Maximum topics per participant (optional)
   * @param {number} [tournamentData.topicsPerParticipantMin] - Minimum topics per participant (optional)
   * @returns {Promise} - API response with updated tournament
   */
  async updateTournament(tournamentId, tournamentData) {
    try {
      const response = await api.put(`/Tournaments/${tournamentId}`, {
        title: tournamentData.title,
        description: tournamentData.description,
        playersPerTournament: tournamentData.playersPerTournament,
        topicsPerParticipantMax: tournamentData.topicsPerParticipantMax,
        topicsPerParticipantMin: tournamentData.topicsPerParticipantMin,
      });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to update tournament. Please try again.",
      };
    }
  },

  /**
   * Delete tournament
   * @param {string} tournamentId - Tournament ID (GUID)
   * @returns {Promise} - API response
   */
  async deleteTournament(tournamentId) {
    try {
      await api.delete(`/Tournaments/${tournamentId}`);
      return { success: true };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to delete tournament. Please try again.",
      };
    }
  },

  /**
   * Advance tournament to the next stage
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} stage - Target stage
   * @returns {Promise} - API response with updated tournament
   */
  async advanceStage(tournamentId, stage) {
    try {
      const response = await api.patch(`/Tournaments/${tournamentId}`, {
        stage: stage,
      });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to advance tournament stage. Please try again.",
      };
    }
  },
};
