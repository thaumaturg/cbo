import { useAuthStore } from "@/stores/auth.js";
import HomeView from "@/views/HomeView.vue";
import LoginView from "@/views/LoginView.vue";
import MatchView from "@/views/MatchView.vue";
import ProfileView from "@/views/ProfileView.vue";
import RegisterView from "@/views/RegisterView.vue";
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
      path: "/login",
      name: "login",
      component: LoginView,
      meta: { guestOnly: true },
    },
    {
      path: "/register",
      name: "register",
      component: RegisterView,
      meta: { guestOnly: true },
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

router.beforeEach((to) => {
  const authStore = useAuthStore();

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return { name: "login", query: { redirect: to.fullPath } };
  }
  if (to.meta.guestOnly && authStore.isAuthenticated) {
    return { name: "home" };
  }
});

export default router;
