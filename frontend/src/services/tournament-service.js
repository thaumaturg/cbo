import api from "./api-interceptors.js";

export const tournamentService = {
  /**
   * Get tournaments where the current user is registered
   * @returns {Promise} - API response with user's tournaments
   */
  async getUserTournaments() {
    try {
      const response = await api.get("/Tournament/user");
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch tournaments. Please try again.",
      };
    }
  },

  /**
   * Get all available tournaments
   * @returns {Promise} - API response with all tournaments
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
   * @param {number} tournamentId - Tournament ID
   * @returns {Promise} - API response with tournament details
   */
  async getTournamentById(tournamentId) {
    try {
      const response = await api.get(`/Tournament/${tournamentId}`);
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
   * @param {string} tournamentData.name - Tournament name
   * @param {string} tournamentData.description - Tournament description
   * @param {string} tournamentData.startDate - Tournament start date
   * @param {string} tournamentData.endDate - Tournament end date
   * @returns {Promise} - API response
   */
  async createTournament(tournamentData) {
    try {
      const response = await api.post("/Tournament", {
        name: tournamentData.name,
        description: tournamentData.description,
        startDate: tournamentData.startDate,
        endDate: tournamentData.endDate,
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
   * @param {number} tournamentId - Tournament ID
   * @param {Object} tournamentData - Updated tournament data
   * @returns {Promise} - API response
   */
  async updateTournament(tournamentId, tournamentData) {
    try {
      const response = await api.put(`/Tournament/${tournamentId}`, tournamentData);
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
   * @param {number} tournamentId - Tournament ID
   * @returns {Promise} - API response
   */
  async deleteTournament(tournamentId) {
    try {
      const response = await api.delete(`/Tournament/${tournamentId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to delete tournament. Please try again.",
      };
    }
  },

  /**
   * Register user for a tournament
   * @param {number} tournamentId - Tournament ID
   * @returns {Promise} - API response
   */
  async registerForTournament(tournamentId) {
    try {
      const response = await api.post(`/Tournament/${tournamentId}/register`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to register for tournament. Please try again.",
      };
    }
  },

  /**
   * Unregister user from a tournament
   * @param {number} tournamentId - Tournament ID
   * @returns {Promise} - API response
   */
  async unregisterFromTournament(tournamentId) {
    try {
      const response = await api.delete(`/Tournament/${tournamentId}/register`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to unregister from tournament. Please try again.",
      };
    }
  },
};
