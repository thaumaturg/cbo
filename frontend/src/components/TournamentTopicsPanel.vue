<script setup>
import TopicsTable from "@/components/TopicsTable.vue";
import { topicService } from "@/services/topic-service.js";
import { tournamentTopicsService } from "@/services/tournament-topics-service.js";
import { useNotify } from "@/utils/notify.js";
import AutoComplete from "primevue/autocomplete";
import Button from "primevue/button";
import Message from "primevue/message";
import { computed, ref, watch } from "vue";

const props = defineProps({
  tournament: {
    type: Object,
    required: true,
  },
  readOnly: {
    type: Boolean,
    default: false,
  },
});

const notify = useNotify();

// The tournament topics in priority order, with empty slots taking positions between them
const list = ref([]);
const ownedTopics = ref([]);
const searchQuery = ref("");
const filteredTopics = ref([]);
const isLoading = ref(false);
const isSaving = ref(false);
const loadError = ref(null);

// What the server holds, so changes that only touch trailing empty slots skip the request
let savedPayload = "[]";

const maxTopics = computed(() => props.tournament.topicsPerParticipantMax);
const minTopics = computed(() => props.tournament.topicsPerParticipantMin);
const isFull = computed(() => list.value.length >= maxTopics.value);
const isBusy = computed(() => isLoading.value || isSaving.value);
const canEdit = computed(() => !props.readOnly && !loadError.value);

const availableTopics = computed(() => {
  const listedIds = new Set(list.value.map((row) => row.topicId));
  return ownedTopics.value.filter((topic) => !listedIds.has(topic.id));
});

let emptySlotCounter = 0;
const createEmptySlot = () => ({ key: `empty-slot-${emptySlotCounter++}`, isEmptySlot: true });

const toTopicRow = (topic) => ({ ...topic, key: topic.id, topicId: topic.id });

const buildList = (dtos) => {
  const ownedById = new Map(ownedTopics.value.map((topic) => [topic.id, topic]));
  const sorted = [...dtos].sort((a, b) => a.priorityIndex - b.priorityIndex);
  const rows = [];
  for (const dto of sorted) {
    while (rows.length < dto.priorityIndex) {
      rows.push(createEmptySlot());
    }
    rows.push(toTopicRow(ownedById.get(dto.topicId) ?? { id: dto.topicId, title: dto.topicTitle }));
  }
  return rows;
};

const toPayload = (rows) =>
  rows
    .map((row, index) => ({ row, index }))
    .filter(({ row }) => !row.isEmptySlot)
    .map(({ row, index }) => ({ topicId: row.topicId, priorityIndex: index }));

// Without the server's list, any save would overwrite it, so a failed load also blocks editing
const load = async () => {
  isLoading.value = true;
  loadError.value = null;

  try {
    const [myResult, ownedResult] = await Promise.all([
      tournamentTopicsService.getMyTopics(props.tournament.id),
      topicService.getAllTopics(),
    ]);
    const failed = [myResult, ownedResult].find((result) => !result.success);
    if (failed) {
      list.value = [];
      loadError.value = failed.message;
      return;
    }

    ownedTopics.value = ownedResult.data;
    list.value = buildList(myResult.data);
    savedPayload = JSON.stringify(toPayload(list.value));
  } catch (error) {
    list.value = [];
    loadError.value = "Failed to load your topics. Please try again.";
    console.error("Error fetching tournament topics:", error);
  } finally {
    isLoading.value = false;
  }
};

// Keeps the local list on success, so a trailing empty slot stays on screen
const save = async () => {
  const payload = toPayload(list.value);
  const serialized = JSON.stringify(payload);
  if (serialized === savedPayload) return;

  isSaving.value = true;
  let failed = false;

  try {
    const result = await tournamentTopicsService.setMyTopics(props.tournament.id, payload);
    if (result.success) {
      savedPayload = serialized;
    } else {
      failed = true;
      notify.error("Save Failed", result.message);
    }
  } catch (error) {
    failed = true;
    notify.error("Save Failed", "An unexpected error occurred. Please try again.");
    console.error("Error saving tournament topics:", error);
  } finally {
    isSaving.value = false;
  }

  if (failed) await load();
};

const searchTopics = (event) => {
  const query = event.query.toLowerCase();
  filteredTopics.value = availableTopics.value.filter((topic) => topic.title.toLowerCase().includes(query));
};

const addTopic = (event) => {
  const topic = event.value;
  searchQuery.value = "";
  if (!topic || isFull.value || isBusy.value) return;

  list.value = [...list.value, toTopicRow(topic)];
  save();
};

const addEmptySlot = () => {
  if (isFull.value || isBusy.value) return;

  list.value = [...list.value, createEmptySlot()];
  save();
};

const removeRow = (index) => {
  if (isBusy.value) return;

  list.value = list.value.filter((_, i) => i !== index);
  save();
};

const reorder = (rows) => {
  if (isBusy.value) return;

  list.value = rows;
  save();
};

// Starting the tournament makes the list read-only, so reload with the stage too
watch(() => [props.tournament.id, props.tournament.currentStage], load, { immediate: true });
</script>

<template>
  <div class="flex flex-col gap-4">
    <Message v-if="loadError" severity="error">{{ loadError }}</Message>

    <Message :severity="isFull ? 'warn' : 'info'">
      {{ list.length }} / {{ maxTopics }} positions used.
      <span class="opacity-75">Suggested minimum: {{ minTopics }} (not required).</span>
    </Message>

    <!-- Add Topic -->
    <div v-if="canEdit" class="flex flex-col gap-2">
      <div class="flex gap-2">
        <AutoComplete
          v-model="searchQuery"
          :suggestions="filteredTopics"
          optionLabel="title"
          placeholder="Search your topics…"
          aria-label="Search your topics"
          class="flex-1"
          :disabled="isFull || isBusy"
          dropdown
          forceSelection
          @complete="searchTopics"
          @option-select="addTopic"
        >
          <template #empty>
            <div class="p-2 text-gray-500 dark:text-gray-400">
              {{
                availableTopics.length === 0
                  ? "All your topics are already in this tournament"
                  : "No matching topics found"
              }}
            </div>
          </template>
        </AutoComplete>
        <Button
          label="Add empty slot"
          icon="pi pi-minus-circle"
          severity="secondary"
          outlined
          class="shrink-0"
          :disabled="isFull || isBusy"
          v-tooltip.bottom="'Reserve a priority position without a topic'"
          @click="addEmptySlot"
        />
      </div>
      <Message v-if="isFull" severity="warn" variant="simple" size="small">Maximum reached ({{ maxTopics }})</Message>
      <span v-if="list.length > 1" class="text-xs text-gray-500 dark:text-gray-400">
        Drag rows by the handle to change the priority of the topics you bring.
      </span>
    </div>

    <TopicsTable
      :topics="list"
      :loading="isLoading"
      :disabled="isSaving"
      :reorderable="canEdit"
      :removable="canEdit"
      showPosition
      dataKey="key"
      emptyMessage="You don't bring any topics to this tournament yet"
      @reorder="reorder"
      @remove="removeRow"
    />
  </div>
</template>
