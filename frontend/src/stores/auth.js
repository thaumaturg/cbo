import { authService } from "@/services/auth-service.js";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useAuthStore = defineStore("auth", () => {
  const user = ref(null);
  const isLoading = ref(false);
  const error = ref(null);

  const isAuthenticated = computed(() => !!user.value && authService.isAuthenticated());
  const userEmail = computed(() => user.value?.email || null);
  const userName = computed(() => user.value?.username || null);
  const userFullName = computed(() => user.value?.fullName || null);
  const isEmailVerified = computed(() => user.value?.emailVerified || false);

  async function login(credentials) {
    isLoading.value = true;
    error.value = null;

    try {
      const result = await authService.login(credentials);

      if (result.success) {
        const userData = authService.getCurrentUser();
        if (userData) {
          user.value = {
            email: userData.email,
            username: userData.preferred_username,
            fullName: userData.name || "",
            emailVerified: userData.email_verified === "true",
          };
        }
        return { success: true };
      } else {
        error.value = result.error;
        return { success: false, error: result.error };
      }
    } catch (error) {
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
    if (authService.isAuthenticated()) {
      const userData = authService.getCurrentUser();
      if (userData) {
        user.value = {
          email: userData.email,
          username: userData.preferred_username,
          fullName: userData.name || "",
          emailVerified: userData.email_verified === "true",
        };
      }
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
