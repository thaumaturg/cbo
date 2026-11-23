<script setup>
import { useAuthStore } from "@/stores/auth";
import { useNotify } from "@/utils/notify.js";
import { keepRedirect } from "@/utils/redirect.js";
import Button from "primevue/button";
import Menubar from "primevue/menubar";
import { computed } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();
const notify = useNotify();

const items = computed(() =>
  authStore.isAuthenticated
    ? [
        { label: "Tournaments", icon: "pi pi-trophy", route: { name: "tournaments" } },
        { label: "Topics", icon: "pi pi-book", route: { name: "topics" } },
        { label: "Friends", icon: "pi pi-users", route: { name: "friends" } },
      ]
    : [],
);

const isActiveSection = (item) => {
  const sectionPath = router.resolve(item.route).path;
  return route.path === sectionPath || route.path.startsWith(`${sectionPath}/`);
};

const handleLogout = () => {
  authStore.logout();
  notify.success("Logged out", "You have been logged out.");
  if (route.meta.requiresAuth) {
    router.push({ name: "home" });
  }
};
</script>

<template>
  <Menubar :model="items" class="header-menubar container mx-auto px-4 py-8">
    <template #start>
      <RouterLink :to="{ name: 'home' }" class="text-xl font-bold">Competitive Bracket Organizer</RouterLink>
    </template>
    <template #item="{ item, props }">
      <RouterLink :to="item.route" custom v-slot="{ href, navigate }">
        <a
          :href="href"
          v-bind="props.action"
          :class="{ 'active-section': isActiveSection(item) }"
          :aria-current="isActiveSection(item) ? 'page' : undefined"
          @click="navigate"
        >
          <span :class="item.icon" v-bind="props.icon" />
          <span v-bind="props.label">{{ item.label }}</span>
        </a>
      </RouterLink>
    </template>
    <template #end>
      <div v-if="!authStore.isAuthenticated" class="flex items-center gap-2">
        <RouterLink
          v-if="route.name !== 'login'"
          :to="{ name: 'login', query: keepRedirect(route.query) }"
          custom
          v-slot="{ href, navigate }"
        >
          <Button as="a" :href="href" label="Log in" @click="navigate" />
        </RouterLink>
        <RouterLink
          v-if="route.name !== 'register'"
          :to="{ name: 'register', query: keepRedirect(route.query) }"
          custom
          v-slot="{ href, navigate }"
        >
          <Button as="a" :href="href" label="Register" outlined @click="navigate" />
        </RouterLink>
      </div>
      <div v-else class="flex items-center gap-3">
        <RouterLink :to="{ name: 'profile' }" class="text-xl font-bold hover:underline" v-tooltip.bottom="'Profile'">
          {{ authStore.userName }}
        </RouterLink>
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

.active-section {
  color: var(--p-primary-color);
  font-weight: 600;
}

.active-section span {
  color: inherit;
}
</style>
