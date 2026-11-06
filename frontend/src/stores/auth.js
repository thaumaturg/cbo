import { authService } from "@/services/auth-service.js";
import { apiFailure, clientFailure } from "@/utils/error.js";
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

  const isAuthenticated = computed(() => !!user.value);
  const userEmail = computed(() => user.value?.email || null);
  const userName = computed(() => user.value?.username || null);
  const userFullName = computed(() => user.value?.fullName || null);
  const isEmailVerified = computed(() => user.value?.emailVerified || false);

  function setUserFromToken() {
    const claims = authService.getCurrentUser();
    if (!claims) return false;

    user.value = mapClaimsToUser(claims);
    return true;
  }

  async function login(credentials) {
    isLoading.value = true;

    try {
      const result = await authService.login(credentials);
      if (!result.success) return result;

      if (!setUserFromToken()) {
        return clientFailure("You couldn't be logged in. Please try again.");
      }
      return { success: true };
    } catch (err) {
      console.error("Login error:", err);
      return apiFailure(err, "An unexpected error occurred during login.");
    } finally {
      isLoading.value = false;
    }
  }

  async function register(userData) {
    isLoading.value = true;

    try {
      const result = await authService.register(userData);
      if (!result.success) return result;

      if (!setUserFromToken()) {
        return clientFailure("Your account was created, but you couldn't be logged in. Log in to continue.");
      }
      return { success: true };
    } catch (err) {
      console.error("Registration error:", err);
      return apiFailure(err, "An unexpected error occurred during registration.");
    } finally {
      isLoading.value = false;
    }
  }

  function logout() {
    authService.logout();
    user.value = null;
  }

  function initializeAuth() {
    setUserFromToken();
  }

  return {
    user,
    isLoading,

    isAuthenticated,
    userEmail,
    userName,
    userFullName,
    isEmailVerified,

    login,
    register,
    logout,
    initializeAuth,
  };
});
