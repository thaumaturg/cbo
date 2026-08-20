import "./assets/main.css";

import { createApp } from "vue";
import { createPinia } from "pinia";

import App from "./App.vue";
import router from "./router";
import VeeValidatePlugin from "./includes/validation";
import PrimeVue from "primevue/config";
import Aura from "@primeuix/themes/aura";
import Tooltip from "primevue/tooltip";
import ToastService from "primevue/toastservice";
import ConfirmationService from "primevue/confirmationservice";

import { useAuthStore } from "@/stores/auth";

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);
app.use(VeeValidatePlugin);
app.use(PrimeVue, {
  license: import.meta.env.VITE_PRIMEUI_LICENSE_KEY,
  theme: {
    preset: Aura,
    options: {
      prefix: "p",
      darkModeSelector: "system",
      cssLayer: false,
    },
  },
});

app.directive("tooltip", Tooltip);
app.use(ToastService);
app.use(ConfirmationService);

const authStore = useAuthStore();
authStore.initializeAuth();

app.mount("#app");
