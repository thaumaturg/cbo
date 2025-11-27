import api from "./api-interceptors.js";

export const tournamentService = {
  // ==================== Tournament Management ====================

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
   * @param {number} tournamentId - Tournament ID
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
   * @param {number} [tournamentData.participantsPerTournament] - Maximum participants in tournament (optional)
   * @param {number} [tournamentData.questionsCostMax] - Maximum question cost (optional)
   * @param {number} [tournamentData.questionsCostMin] - Minimum question cost (optional)
   * @param {number} [tournamentData.questionsPerTopicMax] - Maximum questions per topic (optional)
   * @param {number} [tournamentData.questionsPerTopicMin] - Minimum questions per topic (optional)
   * @param {number} [tournamentData.topicsAuthorsMax] - Maximum topic authors (optional)
   * @param {number} [tournamentData.topicsPerParticipantMax] - Maximum topics per participant (optional)
   * @param {number} [tournamentData.topicsPerParticipantMin] - Minimum topics per participant (optional)
   * @param {number} [tournamentData.topicsPerMatch] - Topics per match (optional)
   * @returns {Promise} - API response with created tournament
   */
  async createTournament(tournamentData) {
    try {
      const response = await api.post("/Tournaments", {
        title: tournamentData.title,
        description: tournamentData.description,
        participantsPerTournament: tournamentData.participantsPerTournament,
        questionsCostMax: tournamentData.questionsCostMax,
        questionsCostMin: tournamentData.questionsCostMin,
        questionsPerTopicMax: tournamentData.questionsPerTopicMax,
        questionsPerTopicMin: tournamentData.questionsPerTopicMin,
        topicsAuthorsMax: tournamentData.topicsAuthorsMax,
        topicsPerParticipantMax: tournamentData.topicsPerParticipantMax,
        topicsPerParticipantMin: tournamentData.topicsPerParticipantMin,
        topicsPerMatch: tournamentData.topicsPerMatch,
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
   * @param {string} tournamentData.title - Tournament title
   * @param {string} [tournamentData.description] - Tournament description (optional)
   * @param {number} [tournamentData.participantsPerTournament] - Maximum participants in tournament (optional)
   * @param {number} [tournamentData.questionsCostMax] - Maximum question cost (optional)
   * @param {number} [tournamentData.questionsCostMin] - Minimum question cost (optional)
   * @param {number} [tournamentData.questionsPerTopicMax] - Maximum questions per topic (optional)
   * @param {number} [tournamentData.questionsPerTopicMin] - Minimum questions per topic (optional)
   * @param {number} [tournamentData.topicsAuthorsMax] - Maximum topic authors (optional)
   * @param {number} [tournamentData.topicsPerParticipantMax] - Maximum topics per participant (optional)
   * @param {number} [tournamentData.topicsPerParticipantMin] - Minimum topics per participant (optional)
   * @param {number} [tournamentData.topicsPerMatch] - Topics per match (optional)
   * @returns {Promise} - API response with updated tournament
   */
  async updateTournament(tournamentId, tournamentData) {
    try {
      const response = await api.put(`/Tournaments/${tournamentId}`, {
        title: tournamentData.title,
        description: tournamentData.description,
        participantsPerTournament: tournamentData.participantsPerTournament,
        questionsCostMax: tournamentData.questionsCostMax,
        questionsCostMin: tournamentData.questionsCostMin,
        questionsPerTopicMax: tournamentData.questionsPerTopicMax,
        questionsPerTopicMin: tournamentData.questionsPerTopicMin,
        topicsAuthorsMax: tournamentData.topicsAuthorsMax,
        topicsPerParticipantMax: tournamentData.topicsPerParticipantMax,
        topicsPerParticipantMin: tournamentData.topicsPerParticipantMin,
        topicsPerMatch: tournamentData.topicsPerMatch,
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
   * @param {number} tournamentId - Tournament ID
   * @returns {Promise} - API response
   */
  async deleteTournament(tournamentId) {
    try {
      const response = await api.delete(`/Tournaments/${tournamentId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to delete tournament. Please try again.",
      };
    }
  },

  // ==================== Tournament Participants Management ====================

  /**
   * Get all participants for a tournament
   * @param {number} tournamentId - Tournament ID
   * @returns {Promise} - API response with participants list
   */
  async getAllParticipants(tournamentId) {
    try {
      const response = await api.get(`/Tournaments/${tournamentId}/participants`);
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
   * @param {number} tournamentId - Tournament ID
   * @param {number} participantId - Participant ID
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
   * @param {number} tournamentId - Tournament ID
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
   * @param {number} tournamentId - Tournament ID
   * @param {number} participantId - Participant ID
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
   * @param {number} tournamentId - Tournament ID
   * @param {number} participantId - Participant ID
   * @returns {Promise} - API response
   */
  async deleteParticipant(tournamentId, participantId) {
    try {
      const response = await api.delete(`/Tournaments/${tournamentId}/participants/${participantId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to remove participant. Please try again.",
      };
    }
  },
};
