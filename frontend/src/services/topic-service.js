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
   * @param {number} topicId - Topic ID
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
   * @param {Object} topicData - Topic data
   * @param {string} topicData.title - Topic title
   * @param {string} topicData.description - Topic description (optional)
   * @param {boolean} topicData.isPlayed - Whether topic has been played
   * @param {boolean} topicData.isAuthor - Whether current user is the author
   * @param {Array} topicData.questions - Array of questions
   * @returns {Promise} - API response
   */
  async createTopic(topicData) {
    try {
      const response = await api.post("/Topics", {
        title: topicData.title,
        description: topicData.description || null,
        isPlayed: topicData.isPlayed,
        isAuthor: topicData.isAuthor,
        questions: topicData.questions.map((q) => ({
          text: q.text,
          answer: q.answer,
          comment: q.comment || null,
        })),
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
   * Update topic with questions
   * @param {number} topicId - Topic ID
   * @param {Object} topicData - Updated topic data
   * @param {string} topicData.title - Topic title
   * @param {string} topicData.description - Topic description (optional)
   * @param {boolean} topicData.isPlayed - Whether topic has been played
   * @param {Array} topicData.questions - Array of questions
   * @returns {Promise} - API response
   */
  async updateTopic(topicId, topicData) {
    try {
      const response = await api.put(`/Topics/${topicId}`, {
        title: topicData.title,
        description: topicData.description || null,
        isPlayed: topicData.isPlayed,
        questions: topicData.questions.map((q) => ({
          text: q.text,
          answer: q.answer,
          comment: q.comment || null,
        })),
      });
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
      const response = await api.delete(`/Topics/${topicId}`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Failed to delete topic. Please try again.",
      };
    }
  },
};
