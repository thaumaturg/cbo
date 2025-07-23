<script setup>
import { RouterLink, RouterView } from "vue-router";
import AppHeader from "@/components/AppHeader.vue";
import AppAuth from "./components/AppAuth.vue";
</script>

<template>
  <AppHeader></AppHeader>
  <AppAuth></AppAuth>
  <RouterLink to="/">Home</RouterLink>
  <RouterView />
</template>
