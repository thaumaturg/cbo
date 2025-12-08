<script setup>
import { watch } from "vue";
import { RouterView, useRouter, useRoute } from "vue-router";
import AppHeader from "@/components/AppHeader.vue";
import AuthDialog from "./components/AuthDialog.vue";
import { useAuthStore } from "@/stores/auth.js";

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

// Redirect to home when user signs out while on a protected route
watch(
  () => authStore.isAuthenticated,
  (isAuthenticated) => {
    if (!isAuthenticated && route.meta.requiresAuth) {
      router.push({ name: "home" });
    }
  }
);
</script>

<template>
  <AppHeader></AppHeader>
  <RouterView />
  <AuthDialog></AuthDialog>
</template>
