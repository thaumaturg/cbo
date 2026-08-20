<script setup>
import { topicService } from "@/services/topic-service.js";
import { tournamentTopicsService } from "@/services/tournament-topics-service.js";
import { extractErrorMessage } from "@/utils/error.js";
import AutoComplete from "primevue/autocomplete";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import Message from "primevue/message";
import OrderList from "primevue/orderlist";
import { useConfirm } from "primevue/useconfirm";
import { computed, ref, watch } from "vue";

const props = defineProps({
  visible: {
    type: Boolean,
    required: true,
  },
  tournament: {
    type: Object,
    required: false,
    default: null,
  },
});

const emit = defineEmits(["update:visible"]);

const confirm = useConfirm();

const assignedTopics = ref([]);
const ownedTopics = ref([]);
const searchQuery = ref("");
const filteredTopics = ref([]);
const isLoadingTopics = ref(false);
const isLoadingOwned = ref(false);
const isSaving = ref(false);
const error = ref(null);
const hasChanges = ref(false);

const isPlayerRole = computed(() => props.tournament?.currentUserRole === "Player");
const minTopics = computed(() => props.tournament?.topicsPerParticipantMin ?? 6);
const maxTopics = computed(() => props.tournament?.topicsPerParticipantMax ?? 10);
const canAddMore = computed(() => assignedTopics.value.length < maxTopics.value);
const realTopicsCount = computed(() => assignedTopics.value.filter((t) => !t.isPlaceholder).length);
const maxPriorityNumber = computed(() => assignedTopics.value.findLastIndex((t) => !t.isPlaceholder) + 1);

let placeholderCounter = 0;
const createPlaceholder = () => ({
  key: `placeholder-${placeholderCounter++}`,
  isPlaceholder: true,
});

const toListItem = (dto) => ({
  key: dto.topicId,
  id: dto.id,
  topicId: dto.topicId,
  title: dto.topicTitle,
});

const buildListFromDtos = (dtos) => {
  const sorted = [...dtos].sort((a, b) => a.priorityIndex - b.priorityIndex);
  const items = [];
  for (const dto of sorted) {
    while (items.length < dto.priorityIndex) {
      items.push(createPlaceholder());
    }
    items.push(toListItem(dto));
  }
  return items;
};

const topicsStatus = computed(() => {
  const count = maxPriorityNumber.value;
  if (isPlayerRole.value && count < minTopics.value) {
    return { severity: "warn", message: `${count}/${minTopics.value} topics (need ${minTopics.value - count} more)` };
  }
  if (count > maxTopics.value) {
    return { severity: "error", message: `${count}/${maxTopics.value} topics (remove ${count - maxTopics.value})` };
  }
  if (isPlayerRole.value) {
    return { severity: "success", message: `${count}/${maxTopics.value} topics` };
  }
  return {
    severity: "success",
    message: `${count}/${maxTopics.value} topics (min ${minTopics.value} optional for your role)`,
  };
});

const canSave = computed(() => {
  const count = realTopicsCount.value;
  const meetsMinRequirement = isPlayerRole.value ? count >= minTopics.value : true;
  return hasChanges.value && meetsMinRequirement && count <= maxTopics.value;
});

// Available topics (owned but not assigned)
const availableTopics = computed(() => {
  const assignedIds = new Set(assignedTopics.value.map((t) => t.topicId));
  return ownedTopics.value.filter((t) => !assignedIds.has(t.id));
});

const closeDialog = () => {
  if (hasChanges.value) {
    confirm.require({
      message: "You have unsaved changes. Are you sure you want to close?",
      header: "Unsaved Changes",
      icon: "pi pi-exclamation-triangle",
      rejectProps: { label: "Keep Editing", severity: "secondary", outlined: true },
      acceptProps: { label: "Discard Changes", severity: "danger" },
      accept: () => {
        emit("update:visible", false);
        resetState();
      },
    });
    return;
  }
  emit("update:visible", false);
  resetState();
};

const resetState = () => {
  assignedTopics.value = [];
  ownedTopics.value = [];
  searchQuery.value = "";
  filteredTopics.value = [];
  error.value = null;
  hasChanges.value = false;
};

const fetchAssignedTopics = async () => {
  if (!props.tournament) return;

  isLoadingTopics.value = true;
  error.value = null;

  try {
    const result = await tournamentTopicsService.getMyTopics(props.tournament.id);
    if (result.success) {
      assignedTopics.value = buildListFromDtos(result.data);
    } else {
      error.value = extractErrorMessage(result.error, "Failed to load assigned topics.");
    }
  } catch (err) {
    error.value = "Failed to load assigned topics. Please try again.";
    console.error("Error fetching assigned topics:", err);
  } finally {
    isLoadingTopics.value = false;
  }
};

const fetchOwnedTopics = async () => {
  isLoadingOwned.value = true;

  try {
    const result = await topicService.getAllTopics();
    if (result.success) {
      ownedTopics.value = result.data;
    } else {
      console.error("Failed to load owned topics:", result.error);
    }
  } catch (err) {
    console.error("Error fetching owned topics:", err);
  } finally {
    isLoadingOwned.value = false;
  }
};

const searchTopics = (event) => {
  const query = event.query.toLowerCase();
  filteredTopics.value = availableTopics.value.filter((topic) => topic.title.toLowerCase().includes(query));
};

const addTopic = (event) => {
  const topic = event.value;
  if (!topic || !canAddMore.value) return;

  assignedTopics.value.push({
    key: topic.id,
    id: null,
    topicId: topic.id,
    title: topic.title,
  });

  searchQuery.value = "";
  hasChanges.value = true;
};

const addPlaceholder = () => {
  if (!canAddMore.value) return;

  assignedTopics.value.push(createPlaceholder());
  hasChanges.value = true;
};

const removeTopic = (index) => {
  assignedTopics.value.splice(index, 1);
  hasChanges.value = true;
};

const onReorder = () => {
  hasChanges.value = true;
};

const saveTopics = async () => {
  if (!props.tournament) return;

  isSaving.value = true;
  error.value = null;

  try {
    const topicsToSave = assignedTopics.value
      .map((t, index) => ({ ...t, priorityIndex: index }))
      .filter((t) => !t.isPlaceholder)
      .map((t) => ({
        topicId: t.topicId,
        priorityIndex: t.priorityIndex,
      }));

    const result = await tournamentTopicsService.setMyTopics(props.tournament.id, topicsToSave);

    if (result.success) {
      assignedTopics.value = buildListFromDtos(result.data);
      hasChanges.value = false;
    } else {
      error.value = extractErrorMessage(result.error, "Failed to save topics. Please try again.");
    }
  } catch (err) {
    error.value = "An unexpected error occurred. Please try again.";
    console.error("Error saving topics:", err);
  } finally {
    isSaving.value = false;
  }
};

watch(
  () => props.visible,
  (newValue) => {
    if (newValue && props.tournament) {
      fetchAssignedTopics();
      fetchOwnedTopics();
    } else {
      resetState();
    }
  },
);
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="closeDialog"
    modal
    :header="`Topics - ${tournament?.title || 'Tournament'}`"
    :draggable="false"
    :style="{ width: '50rem' }"
    :closable="!isSaving"
  >
    <!-- Error Message -->
    <div v-if="error" class="mb-4">
      <Message severity="error" :closable="false">{{ error }}</Message>
    </div>

    <!-- Topics Status -->
    <div class="mb-4">
      <Message :severity="topicsStatus.severity" :closable="false">
        {{ topicsStatus.message }}
      </Message>
    </div>

    <!-- Add Topic Section -->
    <div class="mb-6 p-4 border border-gray-200 dark:border-gray-700 rounded-lg">
      <h3 class="text-lg font-semibold mb-4">Add Topic</h3>

      <div class="flex gap-2">
        <AutoComplete
          v-model="searchQuery"
          :suggestions="filteredTopics"
          @complete="searchTopics"
          @option-select="addTopic"
          optionLabel="title"
          placeholder="Search your topics..."
          class="flex-1"
          :disabled="!canAddMore || isLoadingOwned || isSaving"
          :loading="isLoadingOwned"
          dropdown
          forceSelection
        >
          <template #empty>
            <div class="p-2 text-gray-500">
              {{ availableTopics.length === 0 ? "All your topics are already assigned" : "No matching topics found" }}
            </div>
          </template>
        </AutoComplete>
        <Button
          v-if="!isPlayerRole"
          label="Add empty slot"
          icon="pi pi-minus-circle"
          severity="secondary"
          outlined
          @click="addPlaceholder"
          :disabled="!canAddMore || isSaving"
          v-tooltip.bottom="'Reserve a priority slot without a topic'"
          class="shrink-0"
        />
      </div>

      <p v-if="!canAddMore" class="text-sm text-amber-600 dark:text-amber-400 mt-2">
        <i class="pi pi-exclamation-triangle mr-1"></i>
        Maximum topics reached ({{ maxTopics }})
      </p>
    </div>

    <!-- Assigned Topics List (Reorderable) -->
    <div>
      <h3 class="text-lg font-semibold mb-4">Assigned Topics</h3>

      <!-- Loading State -->
      <div v-if="isLoadingTopics" class="text-center py-8">
        <i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i>
        <p class="text-gray-500 dark:text-gray-400 mt-2">Loading topics...</p>
      </div>

      <!-- Topics OrderList -->
      <OrderList
        v-else
        v-model="assignedTopics"
        dataKey="key"
        @reorder="onReorder"
        :metaKeySelection="true"
        :autoOptionFocus="false"
      >
        <template #option="{ item, index }">
          <div class="flex items-center justify-between w-full py-1 px-2">
            <div class="flex items-center gap-2 flex-1 min-w-0">
              <span class="text-gray-400 font-mono text-xs w-5">{{ index + 1 }}.</span>
              <span
                v-if="item.isPlaceholder"
                class="text-sm text-gray-400 dark:text-gray-500 italic border border-dashed border-gray-300 dark:border-gray-600 rounded px-2 py-0.5"
              >
                Empty slot (skipped)
              </span>
              <span v-else class="text-sm text-gray-900 dark:text-gray-100 truncate">{{ item.title }}</span>
            </div>
            <Button
              icon="pi pi-times"
              severity="danger"
              text
              rounded
              size="small"
              @click.stop="removeTopic(index)"
              :disabled="isSaving"
              v-tooltip.left="'Remove'"
              class="shrink-0"
            />
          </div>
        </template>
      </OrderList>
    </div>

    <!-- Footer -->
    <template #footer>
      <div class="flex w-full justify-between items-center">
        <div>
          <span v-if="hasChanges" class="text-sm text-amber-600 dark:text-amber-400">
            <i class="pi pi-exclamation-circle mr-1"></i>
            Unsaved changes
          </span>
        </div>
        <div class="flex gap-2">
          <Button label="Cancel" severity="secondary" @click="closeDialog" :disabled="isSaving" />
          <Button
            label="Save"
            severity="success"
            @click="saveTopics"
            :loading="isSaving"
            :disabled="!canSave || isSaving"
          />
        </div>
      </div>
    </template>
  </Dialog>
</template>

<style scoped></style>
