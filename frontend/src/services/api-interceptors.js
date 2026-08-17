import axios from "axios";

const api = axios.create({
  baseURL: "/api",
  headers: {
    "Content-Type": "application/json",
  },
});

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("auth_token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  },
);

// Auth attempts must keep their own error handling: a 401 from these
// endpoints is a failed login, not an expired session.
const AUTH_ENDPOINTS = ["/Auth/Login", "/Auth/Register"];

function isAuthRequest(config) {
  const url = config?.url ?? "";
  return AUTH_ENDPOINTS.some((endpoint) => url.includes(endpoint));
}

async function handleSessionExpired() {
  const [{ useAuthStore }, { default: router }] = await Promise.all([
    import("@/stores/auth.js"),
    import("@/router/index.js"),
  ]);

  useAuthStore().logout();

  if (router.currentRoute.value.name !== "home") {
    await router.push({ name: "home", query: { authRequired: "true" } });
  }
}

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && !isAuthRequest(error.config)) {
      localStorage.removeItem("auth_token");
      handleSessionExpired();
    }
    return Promise.reject(error);
  },
);

export default api;
