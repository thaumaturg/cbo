<script setup>
import { RouterLink, RouterView } from "vue-router";
import AppHeader from "@/components/AppHeader.vue";
import AuthDialog from "./components/AuthDialog.vue";
</script>

<template>
  <AppHeader></AppHeader>
  <AuthDialog></AuthDialog>
  <RouterLink to="/">Home</RouterLink>
  <RouterView />
</template>
