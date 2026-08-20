import { useAuthStore } from "@/stores/auth.js";
import HomeView from "@/views/HomeView.vue";
import MatchView from "@/views/MatchView.vue";
import ProfileView from "@/views/ProfileView.vue";
import TopicView from "@/views/TopicView.vue";
import TournamentView from "@/views/TournamentView.vue";
import { createRouter, createWebHistory } from "vue-router";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      name: "home",
      component: HomeView,
    },
    {
      path: "/profile",
      name: "profile",
      component: ProfileView,
      meta: { requiresAuth: true },
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
      path: "/tournaments/:tournamentId",
      name: "tournament-view",
      component: TournamentView,
      meta: { requiresAuth: true },
    },
    {
      path: "/tournaments/:tournamentId/matches/:matchId",
      name: "match-view",
      component: MatchView,
      meta: { requiresAuth: true },
    },
    {
      path: "/:pathMatch(.*)*",
      redirect: { name: "home" },
    },
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
