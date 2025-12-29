import api from "./api-interceptors.js";

export const topicService = {
  /**
   * Get all topics owned by the current user
   * @returns {Promise} - API response with user's topics
   */
  async getAllTopics() {
    try {
      const response = await api.get("/Topics");
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch topics. Please try again.",
      };
    }
  },

  /**
   * Get topic by ID (includes questions)
   * @param {string} topicId - Topic ID (GUID)
   * @returns {Promise} - API response with topic details
   */
  async getTopicById(topicId) {
    try {
      const response = await api.get(`/Topics/${topicId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch topic details. Please try again.",
      };
    }
  },

  /**
   * Create a new topic with questions
   * @param {Object} topicData - Pre-formatted topic data from view
   * @returns {Promise} - API response
   */
  async createTopic(topicData) {
    try {
      const response = await api.post("/Topics", topicData);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to create topic. Please try again.",
      };
    }
  },

  /**
   * Update topic with questions
   * @param {string} topicId - Topic ID (GUID)
   * @param {Object} topicData - Pre-formatted topic data from view (questions must include IDs)
   * @returns {Promise} - API response
   */
  async updateTopic(topicId, topicData) {
    try {
      const response = await api.put(`/Topics/${topicId}`, topicData);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to update topic. Please try again.",
      };
    }
  },

  /**
   * Delete topic
   * @param {string} topicId - Topic ID (GUID)
   * @returns {Promise} - API response
   */
  async deleteTopic(topicId) {
    try {
      await api.delete(`/Topics/${topicId}`);
      return { success: true };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to delete topic. Please try again.",
      };
    }
  },

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
