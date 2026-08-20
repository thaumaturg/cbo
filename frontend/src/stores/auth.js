import { authService } from "@/services/auth-service.js";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

/**
 * Map JWT claims to the user shape used by the UI.
 * @param {Object} claims - Decoded JWT claims
 * @returns {Object} - User object
 */
function mapClaimsToUser(claims) {
  return {
    email: claims.email,
    username: claims.preferred_username,
    fullName: claims.name || "",
    emailVerified: claims.email_verified === true || claims.email_verified === "true",
  };
}

export const useAuthStore = defineStore("auth", () => {
  const user = ref(null);
  const isLoading = ref(false);
  const error = ref(null);

  const isAuthenticated = computed(() => !!user.value);
  const userEmail = computed(() => user.value?.email || null);
  const userName = computed(() => user.value?.username || null);
  const userFullName = computed(() => user.value?.fullName || null);
  const isEmailVerified = computed(() => user.value?.emailVerified || false);

  async function login(credentials) {
    isLoading.value = true;
    error.value = null;

    try {
      const result = await authService.login(credentials);

      if (!result.success) {
        error.value = result.error;
        return { success: false, error: result.error };
      }

      const claims = authService.getCurrentUser();
      if (!claims) {
        error.value = "Login failed: could not establish a session.";
        return { success: false, error: error.value };
      }

      user.value = mapClaimsToUser(claims);
      return { success: true };
    } catch (err) {
      console.error("Login error:", err);
      error.value = "An unexpected error occurred during login.";
      return { success: false, error: error.value };
    } finally {
      isLoading.value = false;
    }
  }

  async function register(userData) {
    isLoading.value = true;
    error.value = null;

    try {
      const result = await authService.register(userData);

      if (result.success) {
        return { success: true };
      } else {
        error.value = result.error;
        return { success: false, error: result.error };
      }
    } catch (err) {
      console.error("Registration error:", err);
      error.value = "An unexpected error occurred during registration.";
      return { success: false, error: error.value };
    } finally {
      isLoading.value = false;
    }
  }

  function logout() {
    authService.logout();
    user.value = null;
    error.value = null;
  }

  function clearError() {
    error.value = null;
  }

  function initializeAuth() {
    const claims = authService.getCurrentUser();
    if (claims) {
      user.value = mapClaimsToUser(claims);
    }
  }

  return {
    user,
    isLoading,
    error,

    isAuthenticated,
    userEmail,
    userName,
    userFullName,
    isEmailVerified,

    login,
    register,
    logout,
    clearError,
    initializeAuth,
  };
});
