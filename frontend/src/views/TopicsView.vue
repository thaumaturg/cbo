<script setup>
import TopicsTable from "@/components/TopicsTable.vue";
import { topicService } from "@/services/topic-service.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import { onMounted, ref } from "vue";
import { RouterLink } from "vue-router";

const notify = useNotify();

const topics = ref([]);
const isLoading = ref(false);

const fetchTopics = async () => {
  isLoading.value = true;
  try {
    const result = await topicService.getAllTopics();
    if (result.success) {
      topics.value = result.data;
    } else {
      notify.error("Load Failed", result.message);
    }
  } finally {
    isLoading.value = false;
  }
};

onMounted(fetchTopics);
</script>

<template>
  <main class="container mx-auto px-4 py-8">
    <div class="mb-6 flex items-center gap-4">
      <h1 class="text-3xl">Topics</h1>
      <RouterLink :to="{ name: 'topic-new' }" custom v-slot="{ href, navigate }">
        <Button as="a" :href="href" icon="pi pi-plus" label="New" severity="secondary" outlined @click="navigate" />
      </RouterLink>
    </div>

    <TopicsTable :topics="topics" :loading="isLoading" />
  </main>
</template>
