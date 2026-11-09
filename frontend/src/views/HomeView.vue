<script setup>
import { useAuthStore } from "@/stores/auth.js";
import { keepRedirect } from "@/utils/redirect.js";
import Button from "primevue/button";
import { RouterLink, useRoute } from "vue-router";

const authStore = useAuthStore();
const route = useRoute();
</script>

<template>
  <main class="container mx-auto px-4 py-16">
    <section class="flex flex-col items-center gap-4 text-center">
      <h1 class="text-3xl font-bold">Welcome to Competitive Bracket Organizer</h1>
      <p class="text-lg text-gray-500 dark:text-gray-400">
        Organize tournaments in which participants bring topics and players compete in matches of rounds.
      </p>

      <p v-if="authStore.isAuthenticated" class="text-xl">Welcome back, {{ authStore.userName }}</p>
      <template v-else>
        <p class="text-xl">Log in or register to get started</p>
        <div class="flex items-center gap-2">
          <RouterLink :to="{ name: 'login', query: keepRedirect(route.query) }" custom v-slot="{ href, navigate }">
            <Button as="a" :href="href" label="Log in" @click="navigate" />
          </RouterLink>
          <RouterLink :to="{ name: 'register', query: keepRedirect(route.query) }" custom v-slot="{ href, navigate }">
            <Button as="a" :href="href" label="Register" outlined @click="navigate" />
          </RouterLink>
        </div>
      </template>
    </section>
  </main>
</template>
