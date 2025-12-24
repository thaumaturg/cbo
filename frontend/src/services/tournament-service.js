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
   * @param {number} tournamentId - Tournament ID
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

  /**
   * Get current user's topics for a tournament (for players)
   * @param {number} tournamentId - Tournament ID
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
   * @param {number} tournamentId - Tournament ID
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
   * @param {number} tournamentId - Tournament ID
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
   * Advance tournament to the next stage
   * @param {number} tournamentId - Tournament ID
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

  /**
   * Get all matches for a tournament
   * @param {number} tournamentId - Tournament ID
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
   * @param {number} tournamentId - Tournament ID
   * @param {number} matchId - Match ID
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
   * @param {number} tournamentId - Tournament ID
   * @param {number} matchId - Match ID
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

  /**
   * Create a new round with answers
   * @param {number} tournamentId - Tournament ID
   * @param {number} matchId - Match ID
   * @param {Object} roundData - Round data
   * @param {number} roundData.numberInMatch - Round number (1-4)
   * @param {number} roundData.topicId - Topic ID
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
   * @param {number} tournamentId - Tournament ID
   * @param {number} matchId - Match ID
   * @param {number} roundNumber - Round number (1-4)
   * @param {Object} roundData - Round data
   * @param {number} roundData.numberInMatch - Round number (must match roundNumber)
   * @param {number} roundData.topicId - Topic ID
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
   * Get topic with questions for a tournament (allows access to topics you don't own)
   * @param {number} tournamentId - Tournament ID
   * @param {number} topicId - Topic ID
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

  /**
   * Delete a round
   * @param {number} tournamentId - Tournament ID
   * @param {number} matchId - Match ID
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
