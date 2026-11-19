import api from "./api-interceptors.js";
import { apiFailure } from "@/utils/error.js";

export const topicService = {
  /**
   * Get one page of the topics the current user owns
   * @param {Object} [params] - List params, named as in the API (see `useListQuery`)
   * @param {number} [params.page] - Page number, from 1
   * @param {number} [params.pageSize] - Rows per page, 1–100
   * @param {string} [params.sort] - `title`, `questionCount` or `createdAt`, with an optional leading `-` for descending
   * @param {string} [params.search] - Text the title contains
   * @param {boolean} [params.played] - Whether the topic has been played
   * @param {boolean} [params.isAuthor] - Whether the current user is an author of the topic
   * @param {Object} [options] - Request options
   * @param {AbortSignal} [options.signal] - Cancels the request
   * @returns {Promise} - API response with a page: `{ items, page, pageSize, totalCount }`, where each item is
   *   `{ id, title, questionCount, authorCount, isAuthor, isPlayed, createdAt }`
   */
  async getTopics(params = {}, { signal } = {}) {
    try {
      const response = await api.get("/Topics", { params, signal, paramsSerializer: { indexes: null } });
      return { success: true, data: response.data };
    } catch (error) {
      return apiFailure(error, "Failed to fetch topics. Please try again.");
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
      return apiFailure(error, "Failed to fetch topic details. Please try again.");
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
      return apiFailure(error, "Failed to create topic. Please try again.");
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
      return apiFailure(error, "Failed to update topic. Please try again.");
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
      return apiFailure(error, "Failed to delete topic. Please try again.");
    }
  },
};
