<script setup>
import Button from "primevue/button";
import Card from "primevue/card";

const props = defineProps({
  tournament: {
    type: Object,
    required: true,
    default: () => ({
      id: null,
      name: "Tournament Name",
      description: "",
      startDate: null,
      endDate: null,
    }),
  },
});

const emit = defineEmits(["settings", "participants", "start", "delete"]);

const handleSettings = () => {
  emit("settings", props.tournament);
  console.log("Settings clicked for tournament:", props.tournament.name);
};

const handleParticipants = () => {
  emit("participants", props.tournament);
  console.log("Participants clicked for tournament:", props.tournament.name);
};

const handleStart = () => {
  emit("start", props.tournament);
  console.log("Start clicked for tournament:", props.tournament.name);
};

const handleDelete = () => {
  emit("delete", props.tournament);
  console.log("Delete clicked for tournament:", props.tournament.name);
};
</script>

<template>
  <Card class="w-full shadow-md hover:shadow-lg transition-shadow duration-200">
    <template #content>
      <div class="p-4">
        <div class="mb-4">
          <h3 class="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-1">
            {{ tournament.name }}
          </h3>
          <p v-if="tournament.description" class="text-sm text-gray-600 dark:text-gray-400 line-clamp-2">
            {{ tournament.description }}
          </p>
        </div>

        <div class="flex items-center gap-3">
          <Button
            icon="pi pi-cog"
            severity="secondary"
            outlined
            rounded
            size="small"
            @click="handleSettings"
            v-tooltip.bottom="'Settings'"
            class="p-2"
            aria-label="Tournament Settings"
          />

          <Button
            icon="pi pi-users"
            severity="info"
            outlined
            rounded
            size="small"
            @click="handleParticipants"
            v-tooltip.bottom="'Participants'"
            class="p-2"
            aria-label="Tournament Participants"
          />

          <Button
            icon="pi pi-play"
            severity="success"
            outlined
            rounded
            size="small"
            @click="handleStart"
            v-tooltip.bottom="'Start Tournament'"
            class="p-2"
            aria-label="Start Tournament"
          />

          <Button
            icon="pi pi-trash"
            severity="danger"
            outlined
            rounded
            size="small"
            @click="handleDelete"
            v-tooltip.bottom="'Delete Tournament'"
            class="p-2"
            aria-label="Delete Tournament"
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
