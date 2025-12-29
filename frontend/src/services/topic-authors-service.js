import api from "./api-interceptors.js";

export const topicAuthorsService = {
  /**
   * Get all authors for a topic
   * @param {string} topicId - Topic ID (GUID)
   * @returns {Promise} - API response with authors list
   */
  async getAllAuthors(topicId) {
    try {
      const response = await api.get(`/Topics/${topicId}/authors`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch authors. Please try again.",
      };
    }
  },

  /**
   * Get author by ID
   * @param {string} topicId - Topic ID (GUID)
   * @param {string} authorId - Author ID (GUID)
   * @returns {Promise} - API response with author details
   */
  async getAuthorById(topicId, authorId) {
    try {
      const response = await api.get(`/Topics/${topicId}/authors/${authorId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch author details. Please try again.",
      };
    }
  },

  /**
   * Add an author to a topic
   * @param {string} topicId - Topic ID (GUID)
   * @param {Object} authorData - Author data
   * @param {string} authorData.username - Username of the author
   * @returns {Promise} - API response with created author
   */
  async createAuthor(topicId, authorData) {
    try {
      const response = await api.post(`/Topics/${topicId}/authors`, {
        username: authorData.username,
      });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to add author. Please try again.",
      };
    }
  },

  /**
   * Remove an author from a topic
   * @param {string} topicId - Topic ID (GUID)
   * @param {string} authorId - Author ID (GUID)
   * @returns {Promise} - API response
   */
  async deleteAuthor(topicId, authorId) {
    try {
      await api.delete(`/Topics/${topicId}/authors/${authorId}`);
      return { success: true };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to remove author. Please try again.",
      };
    }
  },
};
