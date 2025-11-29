<script setup>
import { RouterView } from "vue-router";
import AppHeader from "@/components/AppHeader.vue";
import AuthDialog from "./components/AuthDialog.vue";
</script>

<template>
  <AppHeader></AppHeader>
  <RouterView />
  <AuthDialog></AuthDialog>
</template>
