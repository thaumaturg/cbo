import { createRouter, createWebHistory } from "vue-router";
import HomeView from "@/views/HomeView.vue";
import TopicView from "@/views/TopicView.vue";
import { useAuthStore } from "@/stores/auth.js";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      name: "home",
      component: HomeView,
    },
    {
      path: "/topics/new",
      name: "topic-new",
      component: TopicView,
      meta: { requiresAuth: true },
    },
    {
      path: "/topics/:id",
      name: "topic-edit",
      component: TopicView,
      meta: { requiresAuth: true },
    },
    {
      path: "/catchAll.(.*)*",
      redirect: { name: "home" },
    }
  ],
});

router.beforeEach((to, from, next) => {
  if (to.meta.requiresAuth) {
    const authStore = useAuthStore();
    if (!authStore.isAuthenticated) {
      next({ name: "home", query: { authRequired: "true" } });
      return;
    }
  }
  next();
});

export default router;
