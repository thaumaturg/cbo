<script setup>
import { useAuthStore } from "@/stores/auth";
import { useAuthDialogStore } from "@/stores/auth-dialog";
import Button from "primevue/button";
import Menubar from "primevue/menubar";

const authDialogStore = useAuthDialogStore();
const authStore = useAuthStore();

const items = [
  // We can add more navigation items here later
];

const toggleAuthModal = () => {
  authDialogStore.toggle();
};

const handleLogout = () => {
  authStore.logout();
};
</script>

<template>
  <Menubar :model="items" class="header-menubar container mx-auto px-4 py-8">
    <template #start>
      <RouterLink :to="{ name: 'home' }" class="text-xl font-bold">Competitive Bracket Organizer</RouterLink>
    </template>
    <template #end>
      <div v-if="!authStore.isAuthenticated">
        <Button label="Login/Register" severity="primary" outlined @click="toggleAuthModal" />
      </div>
      <div v-else class="flex items-center gap-3">
        <span class="text-xl font-bold">{{ authStore.userName }}</span>
        <Button label="Logout" severity="secondary" outlined rounded @click="handleLogout" class="p-2" />
      </div>
    </template>
  </Menubar>
</template>

<style scoped>
.header-menubar {
  border: none;
  background: transparent;
}
</style>
