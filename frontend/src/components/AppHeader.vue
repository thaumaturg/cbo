<script setup>
import Menubar from "primevue/menubar";
import Button from "primevue/button";
import { useRouter } from "vue-router";
import { useAuthDialogStore } from "@/stores/auth-dialog";
import { useAuthStore } from "@/stores/auth";

const router = useRouter();
const authDialogStore = useAuthDialogStore();
const authStore = useAuthStore();

const items = [
  // We can add more navigation items here later
];

const navigateHome = () => {
  router.push("/");
};

const toggleAuthModal = () => {
  authDialogStore.toggle();
};

const handleLogout = () => {
  authStore.logout();
};
</script>

<template>
  <Menubar :model="items">
    <template #start>
      <div class="cursor-pointer" @click="navigateHome">
        <span>cbo</span>
      </div>
    </template>
    <template #end>
      <div v-if="!authStore.isAuthenticated">
        <Button label="Login/Register" severity="primary" outlined @click="toggleAuthModal" />
      </div>
      <div v-else class="flex items-center gap-3">
        <span class="text-sm text-gray-600">Welcome, {{ authStore.userName }}</span>
        <Button label="Logout" severity="secondary" outlined rounded @click="handleLogout" class="p-2" />
      </div>
    </template>
  </Menubar>
</template>

<style scoped>
.cursor-pointer {
  cursor: pointer;
}
</style>
