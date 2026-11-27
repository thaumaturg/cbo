import api from "./api-interceptors.js";
import { apiFailure } from "@/utils/error.js";

export const tournamentService = {
  /**
   * Get one page of the tournaments the current user takes part in
   * @param {Object} [params] - List params, named as in the API (see `useListQuery`)
   * @param {number} [params.page] - Page number, from `PAGING.firstPage`
   * @param {number} [params.pageSize] - Rows per page, from 1 to `PAGING.maxPageSize`
   * @param {string} [params.sort] - `title`, `stage` or `createdAt`, with an optional leading `-` for descending
   * @param {string} [params.search] - Text the title contains
   * @param {"Preparations" | "Qualifications" | "Playoff" | "Ended"} [params.stage] - Stage filter
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
   * @param {string} [tournamentData.format] - Tournament format, one of `LIMITS.tournaments.selectableFormats` (optional)
   * @param {number} [tournamentData.topicsPerParticipantMax] - Maximum topics per participant (optional)
   * @param {number} [tournamentData.topicsPerParticipantMin] - Minimum topics per participant (optional)
   * @returns {Promise} - API response with created tournament
   */
  async createTournament(tournamentData) {
    try {
      const response = await api.post("/Tournaments", {
        title: tournamentData.title,
        description: tournamentData.description,
        format: tournamentData.format,
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
   * @param {string} [tournamentData.format] - Tournament format, one of `LIMITS.tournaments.selectableFormats` (optional)
   * @param {number} [tournamentData.topicsPerParticipantMax] - Maximum topics per participant (optional)
   * @param {number} [tournamentData.topicsPerParticipantMin] - Minimum topics per participant (optional)
   * @returns {Promise} - API response with updated tournament
   */
  async updateTournament(tournamentId, tournamentData) {
    try {
      const response = await api.put(`/Tournaments/${tournamentId}`, {
        title: tournamentData.title,
        description: tournamentData.description,
        format: tournamentData.format,
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
   * Start Qualifications: move a tournament from Preparations to Qualifications
   * @param {string} tournamentId - Tournament ID (GUID)
   * @returns {Promise} - API response with updated tournament
   */
  async startQualifications(tournamentId) {
    try {
      const response = await api.patch(`/Tournaments/${tournamentId}`, {
        stage: "Qualifications",
      });
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to start Qualifications. Please try again.");
    }
  },

  /**
   * Start Playoff: move a tournament from Qualifications to the Playoff, fixing the qualification places
   * @param {string} tournamentId - Tournament ID (GUID)
   * @returns {Promise} - API response with updated tournament
   */
  async startPlayoff(tournamentId) {
    try {
      const response = await api.patch(`/Tournaments/${tournamentId}`, { stage: "Playoff" });
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to start the Playoff. Please try again.");
    }
  },

  /**
   * End Tournament: move a tournament from the Playoff to Ended, once its Final is decided
   * @param {string} tournamentId - Tournament ID (GUID)
   * @returns {Promise} - API response with updated tournament
   */
  async endTournament(tournamentId) {
    try {
      const response = await api.patch(`/Tournaments/${tournamentId}`, { stage: "Ended" });
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to end the tournament. Please try again.");
    }
  },

  /**
   * Roll back: move a tournament to an earlier stage, wiping everything played after it
   * @param {string} tournamentId - Tournament ID (GUID)
   * @param {string} stage - The earlier stage to go back to
   * @returns {Promise} - API response with updated tournament
   */
  async rollBack(tournamentId, stage) {
    try {
      const response = await api.patch(`/Tournaments/${tournamentId}`, { stage });
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to roll the tournament back. Please try again.");
    }
  },
};
