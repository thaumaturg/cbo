<script setup>
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import Tag from "primevue/tag";
import { RouterLink, useRouter } from "vue-router";

defineProps({
  topics: {
    type: Array,
    required: true,
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const router = useRouter();

const topicRoute = (topic) => ({ name: "topic-edit", params: { id: topic.id } });

const openTopic = ({ data }) => {
  router.push(topicRoute(data));
};
</script>

<template>
  <DataTable
    :value="topics"
    :loading="loading"
    dataKey="id"
    rowHover
    :rowClass="() => 'cursor-pointer'"
    @row-click="openTopic"
  >
    <Column header="Title">
      <template #body="{ data }">
        <RouterLink :to="topicRoute(data)" class="font-semibold hover:underline" @click.stop>
          {{ data.title }}
        </RouterLink>
      </template>
    </Column>
    <Column header="Questions">
      <template #body="{ data }">{{ data.questions.length }}</template>
    </Column>
    <Column header="Authors">
      <template #body="{ data }">{{ data.authors.length }}</template>
    </Column>
    <Column header="I'm an author">
      <template #body="{ data }">
        <i v-if="data.isAuthor" class="pi pi-check" role="img" aria-label="Yes"></i>
      </template>
    </Column>
    <Column header="Played">
      <template #body="{ data }">
        <Tag v-if="data.isPlayed" value="Played" severity="success" />
      </template>
    </Column>
    <template #empty>No topics yet</template>
  </DataTable>
</template>
