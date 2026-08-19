import { jwtDecode } from "jwt-decode";
import api from "./api-interceptors.js";

const TOKEN_KEY = "auth_token";

// Tolerance for client clocks running slightly behind the server. A wrong
// "still valid" verdict is harmless: the backend rejects the request with a
// 401 and the response interceptor ends the session.
const CLOCK_SKEW_MS = 60 * 1000;

export const authService = {
  /**
   * Register a new user
   * @param {Object} userData - User registration data
   * @param {string} userData.email - User email
   * @param {string} userData.username - Username
   * @param {string} userData.password - Password
   * @param {string} userData.fullName - Full name (optional)
   * @returns {Promise} - Registration response
   */
  async register(userData) {
    try {
      const response = await api.post("/Auth/Register", {
        email: userData.email,
        password: userData.password,
        username: userData.username,
        fullName: userData.fullName,
      });
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Registration failed. Please try again.",
      };
    }
  },

  /**
   * Login user and store the received JWT.
   * @param {Object} credentials - Login credentials
   * @param {string} credentials.email - User email
   * @param {string} credentials.password - User password
   * @returns {Promise} - Login response with token
   */
  async login(credentials) {
    try {
      const response = await api.post("/Auth/Login", {
        email: credentials.email,
        password: credentials.password,
      });

      const token = response.data?.jwtToken;
      if (!token || !this.decodeToken(token)) {
        return {
          success: false,
          error: "Received an invalid token from the server. Please try again.",
        };
      }

      localStorage.setItem(TOKEN_KEY, token);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Login failed. Please check your credentials.",
      };
    }
  },

  /**
   * Change the current user's password
   * @param {Object} passwordData - Password change data
   * @param {string} passwordData.currentPassword - Current password
   * @param {string} passwordData.newPassword - New password
   * @returns {Promise} - Change password response
   */
  async changePassword(passwordData) {
    try {
      await api.post("/Auth/ChangePassword", {
        currentPassword: passwordData.currentPassword,
        newPassword: passwordData.newPassword,
      });
      return { success: true };
    } catch (error) {
      const problemErrors = error.response?.data?.errors;
      const messages = problemErrors ? Object.values(problemErrors).flat() : [];
      return {
        success: false,
        error: messages.length > 0 ? messages.join(" ") : "Password change failed. Please try again.",
      };
    }
  },

  logout() {
    localStorage.removeItem(TOKEN_KEY);
  },

  /**
   * Derive the current user's claims from the stored token.
   * The token is the single source of truth.
   * @returns {Object|null} - Decoded JWT claims or null
   */
  getCurrentUser() {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return null;

    const claims = this.decodeToken(token);
    if (!claims) {
      this.logout();
      return null;
    }

    if (claims.exp && claims.exp * 1000 + CLOCK_SKEW_MS < Date.now()) {
      console.info("JWT token has expired");
      this.logout();
      return null;
    }

    return claims;
  },

  /**
   * Decode a JWT payload without verifying the signature.
   * @param {string} token - JWT token
   * @returns {Object|null} - Decoded token payload
   */
  decodeToken(token) {
    try {
      return jwtDecode(token);
    } catch (error) {
      console.error("Error decoding token:", error);
      return null;
    }
  },
};
