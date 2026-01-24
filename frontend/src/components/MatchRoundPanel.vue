<script setup>
import { tournamentTopicsService } from "@/services/tournament-topics-service.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import InputNumber from "primevue/inputnumber";
import RadioButton from "primevue/radiobutton";
import Select from "primevue/select";
import Skeleton from "primevue/skeleton";
import ToggleSwitch from "primevue/toggleswitch";
import { computed, onMounted, ref, watch } from "vue";

const props = defineProps({
  roundState: {
    type: Object,
    required: true,
  },
  roundIndex: {
    type: Number,
    required: true,
  },
  participants: {
    type: Array,
    required: true,
  },
  availableTopics: {
    type: Array,
    required: true,
  },
  allRoundStates: {
    type: Array,
    required: true,
  },
  matchRounds: {
    type: Array,
    default: () => [],
  },
  tournamentId: {
    type: String,
    required: true,
  },
  canEdit: {
    type: Boolean,
    default: false,
  },
  isProcessing: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["submit", "delete", "update:roundState"]);

const notify = useNotify();
const localQuestions = ref([]);
const localAnswers = ref({});
const localOverrideCosts = ref({});
const localIsOverrideMode = ref(false);
const topicAuthors = ref([]);
const topicDescription = ref("");
const topicDetailsLoadedForId = ref(null);
// Start in loading state if there's already a topic selected (so everything loads together)
const isLoadingTopicDetails = ref(!!props.roundState?.selectedTopicId);

watch(
  () => props.roundState,
  (newState) => {
    // Sync answers and mode from parent (these are managed by parent)
    localAnswers.value = { ...newState.answers };
    localOverrideCosts.value = { ...newState.overrideCosts };
    localIsOverrideMode.value = newState.isOverrideMode || false;

    if (!newState.selectedTopicId) {
      localQuestions.value = [];
      topicAuthors.value = [];
      topicDescription.value = "";
      topicDetailsLoadedForId.value = null;
    }
  },
  { immediate: true, deep: true },
);

onMounted(async () => {
  if (props.roundState.selectedTopicId && !topicDetailsLoadedForId.value) {
    await fetchTopicDetails(props.roundState.selectedTopicId);
  }
});

// Compute available topics for this specific round (exclude topics used in other rounds)
const filteredAvailableTopics = computed(() => {
  const usedTopicIds = props.allRoundStates
    .filter((_, idx) => idx !== props.roundIndex)
    .map((r) => r.selectedTopicId)
    .filter((id) => id !== null);

  return props.availableTopics.filter((t) => !usedTopicIds.includes(t.topicId));
});

const getRoundByTopicId = (topicId) => {
  return props.matchRounds?.find((r) => r.topicId === topicId) || null;
};

const getTopicById = (topicId) => {
  return props.availableTopics.find((t) => t.topicId === topicId) || null;
};

const actualAuthors = computed(() => {
  return topicAuthors.value.filter((a) => a.isAuthor);
});

/**
 * Fetches topic details (questions, authors, description) from the API.
 * @param {string} topicId - The topic ID to fetch
 * @param {Object} options - Options for the fetch behavior
 * @param {boolean} options.emitChanges - Whether to emit changes to parent (for new topic selection)
 * @param {boolean} options.showErrors - Whether to show error notifications
 */
const fetchTopicDetails = async (topicId, { emitChanges = false, showErrors = false } = {}) => {
  isLoadingTopicDetails.value = true;
  try {
    const result = await tournamentTopicsService.getTournamentTopic(props.tournamentId, topicId);
    if (result.success) {
      localQuestions.value = result.data.questions || [];
      topicAuthors.value = result.data.authors || [];
      topicDescription.value = result.data.description || "";
      topicDetailsLoadedForId.value = topicId;

      if (emitChanges) {
        emitUpdate({ questions: result.data.questions || [] });
      }
    } else {
      localQuestions.value = [];
      topicAuthors.value = [];
      topicDescription.value = "";
      topicDetailsLoadedForId.value = null;

      if (showErrors) {
        notify.error("Topic Load Failed", result.error || "Failed to load questions");
      }
      if (emitChanges) {
        emitUpdate({ questions: [] });
      }
    }
  } catch {
    localQuestions.value = [];
    topicAuthors.value = [];
    topicDescription.value = "";
    topicDetailsLoadedForId.value = null;

    if (showErrors) {
      notify.error("Topic Load Failed", "Failed to load questions");
    }
    if (emitChanges) {
      emitUpdate({ questions: [] });
    }
  } finally {
    isLoadingTopicDetails.value = false;
  }
};

const getAnswerValue = (questionId, participantId) => {
  const key = `${questionId}::${participantId}`;
  return localAnswers.value[key] ?? null;
};

const getOverrideCostValue = (questionId, participantId) => {
  const key = `${questionId}::${participantId}`;
  return localOverrideCosts.value[key] ?? null;
};

const setAnswerValue = (questionId, participantId, value) => {
  if (!props.canEdit) return;

  const key = `${questionId}::${participantId}`;
  const currentValue = localAnswers.value[key];

  if (currentValue === value) {
    localAnswers.value[key] = null;
    emitUpdate({ hasChanges: true, answers: { ...localAnswers.value } });
    return;
  }

  if (value === true) {
    const existingCorrectKey = Object.entries(localAnswers.value).find(
      ([k, v]) => k.startsWith(`${questionId}::`) && k !== key && v === true,
    );

    if (existingCorrectKey) {
      notify.warn("Invalid Selection", "Only one correct answer per question");
      return;
    }
  }

  localAnswers.value[key] = value;
  emitUpdate({ hasChanges: true, answers: { ...localAnswers.value } });
};

const setOverrideCostValue = (questionId, participantId, value) => {
  if (!props.canEdit) return;

  const key = `${questionId}::${participantId}`;
  localOverrideCosts.value[key] = value;
  emitUpdate({ hasChanges: true, overrideCosts: { ...localOverrideCosts.value } });
};

const toggleOverrideMode = (newValue) => {
  if (!props.canEdit) return;

  localIsOverrideMode.value = newValue;
  // Clear all answers/costs when switching modes
  localAnswers.value = {};
  localOverrideCosts.value = {};
  emitUpdate({
    isOverrideMode: newValue,
    answers: {},
    overrideCosts: {},
    hasChanges: true,
  });
};

const onTopicChange = async (topicId) => {
  if (!props.canEdit) return;

  emitUpdate({
    selectedTopicId: topicId,
    answers: {},
    overrideCosts: {},
    hasChanges: true,
  });

  localAnswers.value = {};
  localOverrideCosts.value = {};

  if (topicId) {
    await fetchTopicDetails(topicId, { emitChanges: true, showErrors: true });
  } else {
    // No topic selected - emit empty questions (watcher handles clearing local state)
    emitUpdate({ questions: [] });
  }
};

const emitUpdate = (updates) => {
  emit("update:roundState", {
    ...props.roundState,
    ...updates,
  });
};

const handleSubmit = () => {
  emit("submit", props.roundIndex);
};

const handleDelete = () => {
  emit("delete", props.roundIndex);
};
</script>

<template>
  <div class="p-4 space-y-6">
    <!-- Topic Selection (Creator) or Topic Display (read-only) -->
    <div class="flex flex-col gap-2">
      <!-- Topic Selector (only for Creator) -->
      <div v-if="canEdit" class="flex items-center gap-3">
        <Select
          :modelValue="roundState.selectedTopicId"
          @update:modelValue="onTopicChange"
          :options="filteredAvailableTopics"
          optionLabel="topicTitle"
          optionValue="topicId"
          placeholder="Choose a topic..."
          class="w-full md:w-1/2"
          :disabled="!!roundState.existingRoundId"
        >
          <template #option="{ option }">
            <div class="flex items-center gap-3">
              <span class="font-mono text-sm bg-gray-200 dark:bg-gray-600 px-2 py-0.5 rounded">
                #{{ option.priorityIndex }}
              </span>
              <span class="font-medium">{{ option.topicTitle }}</span>
              <span class="text-gray-500 dark:text-gray-400 text-sm">by {{ option.ownerUsername }}</span>
            </div>
          </template>
          <template #value="{ value }">
            <template v-if="value">
              <!-- Find topic in available topics first -->
              <span v-if="getTopicById(value)" class="flex items-center gap-2">
                <span class="font-mono text-sm">#{{ getTopicById(value).priorityIndex }}</span>
                <span>{{ getTopicById(value).topicTitle }}</span>
                <span class="text-gray-500 text-sm">by {{ getTopicById(value).ownerUsername }}</span>
              </span>
              <!-- Handle case where topic is from existing round but not in available list -->
              <span v-else-if="getRoundByTopicId(value)" class="flex items-center gap-2">
                <span class="font-mono text-sm">#{{ getRoundByTopicId(value).topicPriorityIndex }}</span>
                <span>{{ getRoundByTopicId(value).topicTitle }}</span>
                <span class="text-gray-500 text-sm">by {{ getRoundByTopicId(value).topicOwnerUsername }}</span>
              </span>
            </template>
            <span v-else class="text-gray-400">Choose a topic...</span>
          </template>
        </Select>
      </div>

      <!-- Topic Display (read-only for non-creators) -->
      <div v-else-if="roundState.selectedTopicId">
        <label class="font-semibold text-gray-700 dark:text-gray-300">Topic</label>
        <p class="text-gray-800 dark:text-gray-200">
          {{ getRoundByTopicId(roundState.selectedTopicId)?.topicTitle || `Topic ID: ${roundState.selectedTopicId}` }}
        </p>
      </div>

      <small v-if="canEdit && roundState.existingRoundId" class="text-gray-500 dark:text-gray-400">
        Topic is locked. Delete the round to select a different topic.
      </small>
    </div>

    <!-- Topic Details Section (shared for both modes) -->
    <template v-if="roundState.selectedTopicId">
      <!-- Loading skeleton for topic details and table -->
      <template v-if="isLoadingTopicDetails">
        <!-- Authors/Description skeleton -->
        <div class="p-4 bg-white dark:bg-gray-800 rounded-xl shadow-md">
          <div class="flex items-center gap-2 mb-3">
            <Skeleton width="1.5rem" height="1.5rem" shape="circle" />
            <Skeleton width="6rem" height="1.5rem" />
          </div>
          <div class="flex flex-wrap gap-4">
            <Skeleton width="5rem" height="2.25rem" borderRadius="0.5rem" />
            <Skeleton width="6rem" height="2.25rem" borderRadius="0.5rem" />
          </div>
        </div>

        <!-- Override toggle skeleton -->
        <div class="flex items-center gap-3 p-3 bg-gray-100 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
          <Skeleton width="2.5rem" height="1.5rem" borderRadius="1rem" />
          <Skeleton width="10rem" height="1rem" />
        </div>

        <!-- Table skeleton -->
        <div class="bg-white dark:bg-gray-800 rounded-xl shadow-md overflow-hidden">
          <!-- Table header skeleton -->
          <div class="flex gap-4 p-3 bg-gray-50 dark:bg-gray-700 border-b border-gray-200 dark:border-gray-600">
            <Skeleton width="4rem" height="1rem" />
            <Skeleton width="4rem" height="1rem" />
            <Skeleton width="12rem" height="1rem" />
            <Skeleton width="8rem" height="1rem" />
            <Skeleton width="6rem" height="1rem" />
          </div>
          <!-- Table rows skeleton -->
          <div v-for="i in 5" :key="i" class="flex gap-4 p-3 border-b border-gray-100 dark:border-gray-700">
            <Skeleton width="3rem" height="1.25rem" />
            <Skeleton width="3rem" height="1.25rem" />
            <Skeleton width="100%" height="1.25rem" />
            <Skeleton width="6rem" height="1.25rem" />
            <Skeleton width="4rem" height="1.25rem" />
          </div>
        </div>
      </template>

      <!-- Authors display -->
      <transition name="fade">
        <div
          v-if="!isLoadingTopicDetails && actualAuthors.length > 0"
          class="p-4 bg-white dark:bg-gray-800 rounded-xl shadow-md"
        >
          <h3 class="text-lg font-semibold mb-3 text-gray-900 dark:text-gray-100">
            <i class="pi pi-users mr-2 text-blue-500"></i>Authors
          </h3>
          <div class="flex flex-wrap gap-4">
            <div
              v-for="author in actualAuthors"
              :key="author.id"
              class="px-4 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg"
            >
              <span class="font-medium text-gray-800 dark:text-gray-200">{{ author.username }}</span>
            </div>
          </div>
        </div>
      </transition>

      <!-- Topic Description -->
      <transition name="fade">
        <div
          v-if="!isLoadingTopicDetails && topicDescription"
          class="p-4 bg-white dark:bg-gray-800 rounded-xl shadow-md"
        >
          <h3 class="text-lg font-semibold mb-3 text-gray-900 dark:text-gray-100">
            <i class="pi pi-align-left mr-2 text-purple-500"></i>Description
          </h3>
          <p class="text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{{ topicDescription }}</p>
        </div>
      </transition>

      <!-- Override Mode Toggle -->
      <div
        v-if="!isLoadingTopicDetails"
        class="flex items-center gap-3 p-3 bg-gray-100 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700"
      >
        <ToggleSwitch
          :modelValue="localIsOverrideMode"
          @update:modelValue="toggleOverrideMode"
          :disabled="!canEdit || !!roundState.existingRoundId"
        />
        <span class="font-medium text-gray-700 dark:text-gray-300">Override Costs Mode</span>
      </div>
    </template>

    <!-- Questions Table (shown when topic is selected and details are loaded) -->
    <div v-if="roundState.selectedTopicId && !isLoadingTopicDetails && localQuestions.length > 0">
      <DataTable :value="localQuestions" dataKey="id" responsiveLayout="scroll" stripedRows size="small">
        <Column field="costPositive" header="Cost +" style="width: 80px" class="text-center">
          <template #body="{ data }">
            <span class="text-green-600 dark:text-green-400 font-medium">+{{ data.costPositive }}</span>
          </template>
        </Column>

        <Column field="costNegative" header="Cost -" style="width: 80px" class="text-center">
          <template #body="{ data }">
            <span class="text-red-600 dark:text-red-400 font-medium">-{{ data.costNegative }}</span>
          </template>
        </Column>

        <Column field="text" header="Question" style="min-width: 250px">
          <template #body="{ data }">
            <div class="text-gray-800 dark:text-gray-200">{{ data.text }}</div>
          </template>
        </Column>

        <Column field="answer" header="Answer" style="min-width: 150px">
          <template #body="{ data }">
            <div class="text-gray-600 dark:text-gray-400 italic">{{ data.answer }}</div>
          </template>
        </Column>

        <Column field="comment" header="Comment" style="min-width: 120px">
          <template #body="{ data }">
            <div v-if="data.comment" class="text-gray-500 dark:text-gray-500 text-sm">{{ data.comment }}</div>
            <div v-else class="text-gray-400">-</div>
          </template>
        </Column>

        <!-- Dynamic columns for each participant -->
        <Column v-for="participant in participants" :key="participant.id" style="min-width: 140px">
          <template #header>
            <div class="w-full text-center">{{ participant.username }}</div>
          </template>
          <template #body="{ data }">
            <!-- Override Mode: Input field for custom score -->
            <div v-if="localIsOverrideMode" class="flex justify-center">
              <InputNumber
                :modelValue="getOverrideCostValue(data.id, participant.id)"
                @update:modelValue="(val) => setOverrideCostValue(data.id, participant.id, val)"
                :disabled="!canEdit"
                :min="-999"
                :max="999"
                inputClass="w-16 text-center"
                class="p-inputnumber-sm"
                placeholder="0"
              />
            </div>
            <!-- Standard Mode: Radio buttons for correct/incorrect -->
            <div v-else class="flex justify-center gap-3">
              <div
                :class="['flex items-center gap-1', canEdit ? 'cursor-pointer' : 'opacity-75']"
                @click="canEdit && setAnswerValue(data.id, participant.id, true)"
              >
                <RadioButton
                  :modelValue="getAnswerValue(data.id, participant.id)"
                  :value="true"
                  :pt="{ input: { class: 'pointer-events-none' } }"
                  :disabled="!canEdit"
                />
                <span class="text-green-600 dark:text-green-400 text-xs">
                  <i class="pi pi-check"></i>
                </span>
              </div>
              <div
                :class="['flex items-center gap-1', canEdit ? 'cursor-pointer' : 'opacity-75']"
                @click="canEdit && setAnswerValue(data.id, participant.id, false)"
              >
                <RadioButton
                  :modelValue="getAnswerValue(data.id, participant.id)"
                  :value="false"
                  :pt="{ input: { class: 'pointer-events-none' } }"
                  :disabled="!canEdit"
                />
                <span class="text-red-600 dark:text-red-400 text-xs">
                  <i class="pi pi-times"></i>
                </span>
              </div>
            </div>
          </template>
        </Column>
      </DataTable>

      <!-- Action Buttons (Creator only) -->
      <div v-if="canEdit" class="mt-4 flex justify-end gap-2">
        <Button
          :label="roundState.existingRoundId ? 'Delete Round' : 'Remove Round'"
          icon="pi pi-trash"
          severity="danger"
          outlined
          :disabled="isProcessing"
          @click="handleDelete"
        />
        <Button
          :label="roundState.existingRoundId ? 'Update Answers' : 'Save Round'"
          :icon="roundState.existingRoundId ? 'pi pi-sync' : 'pi pi-save'"
          :loading="isProcessing"
          :disabled="isProcessing || !roundState.hasChanges"
          @click="handleSubmit"
        />
      </div>
    </div>

    <!-- No topic selected message -->
    <div
      v-else-if="!roundState.selectedTopicId"
      class="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-900 rounded-lg"
    >
      <i class="pi pi-info-circle text-3xl mb-2 block"></i>
      <p v-if="canEdit">Select a topic to view questions and record answers</p>
      <p v-else>No round played yet</p>
    </div>

    <!-- No questions available -->
    <div
      v-else-if="!isLoadingTopicDetails && localQuestions.length === 0"
      class="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-900 rounded-lg"
    >
      <i class="pi pi-exclamation-circle text-3xl mb-2 block"></i>
      <p>No questions found for this topic</p>
    </div>
  </div>
</template>

<style scoped></style>
