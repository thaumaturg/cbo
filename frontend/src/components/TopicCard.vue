<script setup>
import Button from "primevue/button";
import Card from "primevue/card";

const props = defineProps({
  topic: {
    type: Object,
    required: true,
    default: () => ({
      id: null,
      name: "Topic Name",
      authors: [],
      description: "",
      isPlayed: false,
    }),
  },
});

const emit = defineEmits(["view", "authors", "delete"]);

const handleView = () => {
  emit("settings", props.topic);
  console.log("View clicked for topic:", props.topic.name);
};

const handleAuthors = () => {
  emit("participants", props.topic);
  console.log("Authors clicked for authors:", props.topic.name);
};

const handleDelete = () => {
  emit("delete", props.topic);
  console.log("Delete clicked for topic:", props.topic.name);
};
</script>

<template>
  <Card class="w-full shadow-md hover:shadow-lg transition-shadow duration-200">
    <template #content>
      <div class="p-4">
        <div class="mb-4">
          <div class="flex items-start justify-between gap-2 mb-2">
            <h3 class="text-xl font-semibold text-gray-900 dark:text-gray-100">
              {{ topic.name }}
            </h3>
            <div class="flex gap-2">
              <span
                v-if="topic.isPlayed"
                class="px-2 py-1 text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 rounded"
              >
                Played
              </span>
            </div>
          </div>
          <p v-if="topic.description" class="text-sm text-gray-600 dark:text-gray-400 line-clamp-2 mb-2">
            {{ topic.description }}
          </p>
          <div
            v-if="topic.authors && topic.authors.length > 0"
            class="flex items-center gap-1 text-sm text-gray-500 dark:text-gray-400"
          >
            <i class="pi pi-users text-xs"></i>
            <span>{{ topic.authors.join(", ") }}</span>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <Button
            icon="pi pi-cog"
            severity="secondary"
            outlined
            rounded
            size="small"
            @click="handleView"
            v-tooltip.bottom="'View'"
            class="p-2"
            aria-label="Topic View"
          />

          <Button
            icon="pi pi-users"
            severity="info"
            outlined
            rounded
            size="small"
            @click="handleAuthors"
            v-tooltip.bottom="'Authors'"
            class="p-2"
            aria-label="Topic Authors"
          />

          <Button
            icon="pi pi-trash"
            severity="danger"
            outlined
            rounded
            size="small"
            @click="handleDelete"
            v-tooltip.bottom="'Delete Topic'"
            class="p-2"
            aria-label="Delete Topic"
          />
        </div>
      </div>
    </template>
  </Card>
</template>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
