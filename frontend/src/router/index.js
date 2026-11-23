import { useAuthStore } from "@/stores/auth.js";
import FriendsView from "@/views/FriendsView.vue";
import HomeView from "@/views/HomeView.vue";
import LoginView from "@/views/LoginView.vue";
import MatchView from "@/views/MatchView.vue";
import ProfileView from "@/views/ProfileView.vue";
import RegisterView from "@/views/RegisterView.vue";
import TopicView from "@/views/TopicView.vue";
import TopicsView from "@/views/TopicsView.vue";
import TournamentView from "@/views/TournamentView.vue";
import TournamentsView from "@/views/TournamentsView.vue";
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
      path: "/topics",
      name: "topics",
      component: TopicsView,
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
      path: "/tournaments",
      name: "tournaments",
      component: TournamentsView,
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
      path: "/friends",
      name: "friends",
      component: FriendsView,
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
