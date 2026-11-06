<script setup>
import { RouterView } from "vue-router";
import AppHeader from "@/components/AppHeader.vue";
import ConfirmDialog from "primevue/confirmdialog";
import Toast from "primevue/toast";
</script>

<template>
  <AppHeader></AppHeader>
  <RouterView />
  <ConfirmDialog />
  <Toast />
</template>
