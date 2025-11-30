import { createRouter, createWebHistory } from "vue-router";
import HomeView from "@/views/HomeView.vue";
import TopicView from "@/views/TopicView.vue";

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
    },
    {
      path: "/topics/:id",
      name: "topic-edit",
      component: TopicView,
    },
    {
      path: "/catchAll.(.*)*",
      redirect: { name: "home" },
    }
  ],
});

export default router;
