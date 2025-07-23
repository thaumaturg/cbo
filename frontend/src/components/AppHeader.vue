<script setup>
import Menubar from "primevue/menubar";
import Button from "primevue/button";
import { useRouter } from "vue-router";
import useAuthModalStore from "@/stores/auth-modal";

const router = useRouter();
const authModalStore = useAuthModalStore();

const items = [
  // We can add more navigation items here later
];

const navigateHome = () => {
  router.push("/");
};

const toggleAuthModal = () => {
  authModalStore.toggle();
};
</script>

<template>
  <Menubar :model="items">
    <template #start>
      <div class="cursor-pointer" @click="navigateHome">
        <span>CBO</span>
      </div>
    </template>
    <template #end>
      <Button label="Login/Register" severity="primary" outlined @click="toggleAuthModal" />
    </template>
  </Menubar>
</template>

<style scoped>
.cursor-pointer {
  cursor: pointer;
}
</style>
