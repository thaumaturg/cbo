import api from "./api-interceptors.js";

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
   * Login user
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

      if (response.data?.jwtToken) {
        localStorage.setItem("auth_token", response.data.jwtToken);
        const userData = this.decodeToken(response.data.jwtToken);

        if (userData) {
          localStorage.setItem("user_data", JSON.stringify(userData));
        }
      }

      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data || "Login failed. Please check your credentials.",
      };
    }
  },

  logout() {
    localStorage.removeItem("auth_token");
    localStorage.removeItem("user_data");
  },

  /**
   * Check if user is authenticated
   * @returns {boolean} - Authentication status
   */
  isAuthenticated() {
    const token = localStorage.getItem("auth_token");
    if (!token) return false;

    try {
      const payload = this.decodeToken(token);
      if (!payload || !payload.exp) return false;

      const isExpired = payload.exp * 1000 < Date.now();
      if (isExpired) {
        this.logout();
        return false;
      }

      return true;
    } catch {
      this.logout();
      return false;
    }
  },

  /**
   * Get current user data from stored token
   * @returns {Object|null} - User data or null
   */
  getCurrentUser() {
    const userData = localStorage.getItem("user_data");
    return userData ? JSON.parse(userData) : null;
  },

  /**
   * Decode JWT token to extract user data
   * @param {string} token - JWT token
   * @returns {Object|null} - Decoded token payload
   */
  decodeToken(token) {
    try {
      const base64Url = token.split(".")[1];
      const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split("")
          .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
          .join(""),
      );
      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error("Error decoding token:", error);
      return null;
    }
  },

  /**
   * Get auth token
   * @returns {string|null} - JWT token or null
   */
  getToken() {
    return localStorage.getItem("auth_token");
  },
};
