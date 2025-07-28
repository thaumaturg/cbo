import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { authService } from "@/services/auth-service.js";

export const useAuthStore = defineStore("auth", () => {
  const user = ref(null);
  const isLoading = ref(false);
  const error = ref(null);

  const isAuthenticated = computed(() => !!user.value && authService.isAuthenticated());
  const userEmail = computed(() => user.value?.email || null);
  const userName = computed(() => user.value?.username || null);
  const userFullName = computed(() => user.value?.fullName || null);

  async function login(credentials) {
    isLoading.value = true;
    error.value = null;

    try {
      const result = await authService.login(credentials);

      if (result.success) {
        const userData = authService.getCurrentUser();
        if (userData) {
          user.value = {
            email: userData["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"],
            username: userData["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
            roles: userData["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || [],
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
          email: userData["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"],
          username: userData["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
          roles: userData["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || [],
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

    login,
    register,
    logout,
    clearError,
    initializeAuth,
  };
});
