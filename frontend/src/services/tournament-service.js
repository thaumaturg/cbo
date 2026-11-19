import api from "./api-interceptors.js";
import { apiFailure } from "@/utils/error.js";

export const tournamentService = {
  /**
   * Get one page of the tournaments the current user takes part in
   * @param {Object} [params] - List params, named as in the API (see `useListQuery`)
   * @param {number} [params.page] - Page number, from 1
   * @param {number} [params.pageSize] - Rows per page, 1–100
   * @param {string} [params.sort] - `title`, `stage` or `createdAt`, with an optional leading `-` for descending
   * @param {string} [params.search] - Text the title contains
   * @param {"Preparations" | "Started"} [params.stage] - Stage filter
   * @param {Array<"Creator" | "Organizer" | "Player">} [params.role] - The current user's role is one of these
   * @param {Object} [options] - Request options
   * @param {AbortSignal} [options.signal] - Cancels the request
   * @returns {Promise} - API response with a page: `{ items, page, pageSize, totalCount }`
   */
  async getTournaments(params = {}, { signal } = {}) {
    try {
      // indexes: null sends arrays as repeated keys (role=Creator&role=Player), which ASP.NET binds
      const response = await api.get("/Tournaments", { params, signal, paramsSerializer: { indexes: null } });
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to fetch tournaments. Please try again.");
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
      return apiFailure(error, "Failed to fetch tournament details. Please try again.");
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
      return apiFailure(error, "Failed to create tournament. Please try again.");
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
      return apiFailure(error, "Failed to update tournament. Please try again.");
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
      return apiFailure(error, "Failed to delete tournament. Please try again.");
    }
  },

  /**
   * Start a tournament: move it from Preparations to Qualifications
   * @param {string} tournamentId - Tournament ID (GUID)
   * @returns {Promise} - API response with updated tournament
   */
  async startTournament(tournamentId) {
    try {
      const response = await api.patch(`/Tournaments/${tournamentId}`, {
        stage: "Qualifications",
      });
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to start the tournament. Please try again.");
    }
  },
};
