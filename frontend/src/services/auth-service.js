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
        if (!this.validateJWTStructure(response.data.jwtToken)) {
          return {
            success: false,
            error: "Received invalid token format from server.",
          };
        }

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
      if (!this.validateJWTStructure(token)) {
        console.warn("Invalid JWT structure detected");
        this.logout();
        return false;
      }

      const payload = this.decodeToken(token);
      if (!payload) {
        console.warn("Failed to decode JWT payload");
        this.logout();
        return false;
      }

      if (!this.validateRequiredClaims(payload)) {
        console.warn("JWT missing required claims");
        this.logout();
        return false;
      }

      const isExpired = payload.exp * 1000 < Date.now();
      if (isExpired) {
        console.info("JWT token has expired");
        this.logout();
        return false;
      }

      if (payload.iat && payload.iat * 1000 > Date.now()) {
        console.warn("JWT token issued in the future");
        this.logout();
        return false;
      }

      return true;
    } catch (error) {
      console.error("JWT validation error:", error);
      this.logout();
      return false;
    }
  },

  /**
   * Validate JWT structure (header.payload.signature)
   * @param {string} token - JWT token
   * @returns {boolean} - True if structure is valid
   */
  validateJWTStructure(token) {
    if (!token || typeof token !== "string") {
      return false;
    }

    const parts = token.split(".");
    if (parts.length !== 3) {
      return false;
    }

    // Check that each part is base64url encoded (basic check)
    const base64urlPattern = /^[A-Za-z0-9_-]+$/;
    return parts.every((part) => part.length > 0 && base64urlPattern.test(part));
  },

  /**
   * Validate that JWT contains required claims
   * @param {Object} payload - Decoded JWT payload
   * @returns {boolean} - True if all required claims are present
   */
  validateRequiredClaims(payload) {
    const requiredClaims = [
      "sub", // Subject (user ID)
      "jti", // JWT ID
      "iat", // Issued at
      "exp", // Expiration
      "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
      "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
    ];

    return requiredClaims.every((claim) => {
      const hasValue = Object.hasOwn(payload, claim) && payload[claim] !== null && payload[claim] !== undefined;
      if (!hasValue) {
        console.warn(`Missing required JWT claim: ${claim}`);
      }
      return hasValue;
    });
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
      if (!this.validateJWTStructure(token)) {
        throw new Error("Invalid JWT structure");
      }

      const base64Url = token.split(".")[1];
      const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");

      // Add padding if necessary
      const paddedBase64 = base64 + "=".repeat((4 - (base64.length % 4)) % 4);

      const jsonPayload = decodeURIComponent(
        atob(paddedBase64)
          .split("")
          .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
          .join(""),
      );

      const payload = JSON.parse(jsonPayload);

      if (typeof payload !== "object" || payload === null) {
        throw new Error("Invalid JWT payload format");
      }

      return payload;
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
