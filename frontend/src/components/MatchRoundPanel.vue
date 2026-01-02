<script setup>
import { tournamentTopicsService } from "@/services/tournament-topics-service.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import RadioButton from "primevue/radiobutton";
import Select from "primevue/select";
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
const topicAuthors = ref([]);
const authorsLoadedForTopicId = ref(null);

watch(
  () => props.roundState,
  (newState) => {
    localQuestions.value = newState.questions || [];
    localAnswers.value = { ...newState.answers };

    if (!newState.selectedTopicId) {
      topicAuthors.value = [];
      authorsLoadedForTopicId.value = null;
    }
  },
  { immediate: true, deep: true },
);

onMounted(async () => {
  if (props.roundState.selectedTopicId && !authorsLoadedForTopicId.value) {
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

const fetchTopicDetails = async (topicId) => {
  try {
    const result = await tournamentTopicsService.getTournamentTopic(props.tournamentId, topicId);
    if (result.success) {
      topicAuthors.value = result.data.authors || [];
      authorsLoadedForTopicId.value = topicId;
    } else {
      topicAuthors.value = [];
      authorsLoadedForTopicId.value = null;
    }
  } catch {
    topicAuthors.value = [];
    authorsLoadedForTopicId.value = null;
  }
};

const getAnswerValue = (questionId, participantId) => {
  const key = `${questionId}::${participantId}`;
  return localAnswers.value[key] ?? null;
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

const onTopicChange = async (topicId) => {
  if (!props.canEdit) return;

  emitUpdate({
    selectedTopicId: topicId,
    answers: {},
    hasChanges: true,
  });

  localAnswers.value = {};
  topicAuthors.value = [];
  authorsLoadedForTopicId.value = null;

  if (topicId) {
    try {
      const result = await tournamentTopicsService.getTournamentTopic(props.tournamentId, topicId);

      if (result.success) {
        localQuestions.value = result.data.questions || [];
        topicAuthors.value = result.data.authors || [];
        authorsLoadedForTopicId.value = topicId;
        emitUpdate({ questions: result.data.questions || [] });
      } else {
        notify.error("Topic Load Failed", result.error || "Failed to load questions");
        localQuestions.value = [];
        emitUpdate({ questions: [] });
      }
    } catch {
      notify.error("Topic Load Failed", "Failed to load questions");
      localQuestions.value = [];
      emitUpdate({ questions: [] });
    }
  } else {
    localQuestions.value = [];
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
    <!-- Topic Selection (only for Creator) -->
    <div v-if="canEdit" class="flex flex-col gap-2">
      <div class="flex items-center gap-3">
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

      <small v-if="roundState.existingRoundId" class="text-gray-500 dark:text-gray-400">
        Topic is locked. Delete the round to select a different topic.
      </small>

      <!-- Authors display (only people who actually wrote the topic) -->
      <div
        v-if="roundState.selectedTopicId && actualAuthors.length > 0"
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
    </div>

    <!-- Topic Display (read-only for non-creators) -->
    <div v-else-if="roundState.selectedTopicId" class="flex flex-col gap-2">
      <label class="font-semibold text-gray-700 dark:text-gray-300">Topic</label>
      <span class="text-gray-800 dark:text-gray-200">
        {{ getRoundByTopicId(roundState.selectedTopicId)?.topicTitle || `Topic ID: ${roundState.selectedTopicId}` }}
      </span>
    </div>

    <!-- Questions Table (shown when topic is selected) -->
    <div v-if="roundState.selectedTopicId && localQuestions.length > 0">
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
            <div class="flex justify-center gap-3">
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
      v-else-if="localQuestions.length === 0"
      class="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-900 rounded-lg"
    >
      <i class="pi pi-exclamation-circle text-3xl mb-2 block"></i>
      <p>No questions found for this topic</p>
    </div>
  </div>
</template>
