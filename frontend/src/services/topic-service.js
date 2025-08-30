import api from "./api-interceptors.js";

export const topicService = {
  /**
   * Get topics owned by the current user
   * @returns {Promise} - API response with user's topics
   */
  async getUserTopics() {
    try {
      const response = await api.get("/Topic/user");
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch topics. Please try again.",
      };
    }
  },

  /**
   * Get all available topics
   * @returns {Promise} - API response with all topics
   */
  async getAllTopics() {
    try {
      const response = await api.get("/Topic");
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch topics. Please try again.",
      };
    }
  },

  /**
   * Get topic by ID
   * @param {number} topicId - Topic ID
   * @returns {Promise} - API response with topic details
   */
  async getTopicById(topicId) {
    try {
      const response = await api.get(`/Topic/${topicId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch topic details. Please try again.",
      };
    }
  },

  /**
   * Create a new topic
   * @param {Object} topicData - Topic data
   * @param {string} topicData.title - Topic title
   * @param {string} topicData.description - Topic description
   * @param {string} topicData.category - Topic category (optional)
   * @returns {Promise} - API response
   */
  async createTopic(topicData) {
    try {
      const response = await api.post("/Topic", {
        title: topicData.title,
        description: topicData.description,
        category: topicData.category || null,
      });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to create topic. Please try again.",
      };
    }
  },

  /**
   * Update topic
   * @param {number} topicId - Topic ID
   * @param {Object} topicData - Updated topic data
   * @returns {Promise} - API response
   */
  async updateTopic(topicId, topicData) {
    try {
      const response = await api.put(`/Topic/${topicId}`, topicData);
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
   * @param {number} topicId - Topic ID
   * @returns {Promise} - API response
   */
  async deleteTopic(topicId) {
    try {
      const response = await api.delete(`/Topic/${topicId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to delete topic. Please try again.",
      };
    }
  },

  /**
   * Get comments for a topic
   * @param {number} topicId - Topic ID
   * @returns {Promise} - API response with topic comments
   */
  async getTopicComments(topicId) {
    try {
      const response = await api.get(`/Topic/${topicId}/comments`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to fetch comments. Please try again.",
      };
    }
  },

  /**
   * Add comment to a topic
   * @param {number} topicId - Topic ID
   * @param {string} content - Comment content
   * @returns {Promise} - API response
   */
  async addComment(topicId, content) {
    try {
      const response = await api.post(`/Topic/${topicId}/comments`, {
        content: content,
      });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to add comment. Please try again.",
      };
    }
  },
};
