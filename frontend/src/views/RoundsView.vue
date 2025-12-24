<script setup>
import { tournamentService } from "@/services/tournament-service.js";
import Accordion from "primevue/accordion";
import AccordionContent from "primevue/accordioncontent";
import AccordionHeader from "primevue/accordionheader";
import AccordionPanel from "primevue/accordionpanel";
import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import RadioButton from "primevue/radiobutton";
import Select from "primevue/select";
import Toast from "primevue/toast";
import { useToast } from "primevue/usetoast";
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

const route = useRoute();
const router = useRouter();
const toast = useToast();

const tournamentId = computed(() => parseInt(route.params.tournamentId));
const matchId = computed(() => parseInt(route.params.matchId));

const match = ref(null);
const availableTopics = ref([]);
const isLoading = ref(true);
const loadError = ref(null);
const isProcessing = ref(false);

const roundStates = ref([
  { numberInMatch: 1, selectedTopicId: null, questions: [], answers: {}, existingRoundId: null },
  { numberInMatch: 2, selectedTopicId: null, questions: [], answers: {}, existingRoundId: null },
  { numberInMatch: 3, selectedTopicId: null, questions: [], answers: {}, existingRoundId: null },
  { numberInMatch: 4, selectedTopicId: null, questions: [], answers: {}, existingRoundId: null },
]);

const matchTitle = computed(() => {
  if (!match.value) return "Match";
  const stage = match.value.createdOnStage || "Match";
  const number = match.value.numberInStage || match.value.numberInTournament;
  return `${stage} #${number}`;
});

const participants = computed(() => match.value?.matchParticipants || []);

// Exclude topics used in other rounds of this match
const getAvailableTopicsForRound = (roundIndex) => {
  const usedTopicIds = roundStates.value
    .filter((_, idx) => idx !== roundIndex)
    .map((r) => r.selectedTopicId)
    .filter((id) => id !== null);

  return availableTopics.value.filter((t) => !usedTopicIds.includes(t.topicId));
};

const fetchData = async () => {
  isLoading.value = true;
  loadError.value = null;

  try {
    const [matchResult, topicsResult] = await Promise.all([
      tournamentService.getMatchWithRounds(tournamentId.value, matchId.value),
      tournamentService.getAvailableTopics(tournamentId.value, matchId.value),
    ]);

    if (!matchResult.success) {
      loadError.value = matchResult.error;
      toast.add({ severity: "error", summary: "Error", detail: matchResult.error, life: 5000 });
      return;
    }

    if (!topicsResult.success) {
      loadError.value = topicsResult.error;
      toast.add({ severity: "error", summary: "Error", detail: topicsResult.error, life: 5000 });
      return;
    }

    match.value = matchResult.data;
    availableTopics.value = topicsResult.data;

    initializeRoundStates();
  } catch (error) {
    console.error("Error fetching data:", error);
    loadError.value = "Failed to load data. Please try again.";
    toast.add({ severity: "error", summary: "Error", detail: "Failed to load data.", life: 5000 });
  } finally {
    isLoading.value = false;
  }
};

const initializeRoundStates = () => {
  roundStates.value = [
    { numberInMatch: 1, selectedTopicId: null, questions: [], answers: {}, existingRoundId: null },
    { numberInMatch: 2, selectedTopicId: null, questions: [], answers: {}, existingRoundId: null },
    { numberInMatch: 3, selectedTopicId: null, questions: [], answers: {}, existingRoundId: null },
    { numberInMatch: 4, selectedTopicId: null, questions: [], answers: {}, existingRoundId: null },
  ];

  if (match.value?.rounds) {
    for (const round of match.value.rounds) {
      const idx = round.numberInMatch - 1;
      if (idx >= 0 && idx < 4) {
        roundStates.value[idx].existingRoundId = round.id;
        roundStates.value[idx].selectedTopicId = round.topicId;
        roundStates.value[idx].questions = round.questions || [];

        // Build answers map: { "questionId-participantId": "correct" | "wrong" | null }
        const answersMap = {};
        for (const answer of round.roundAnswers || []) {
          const key = `${answer.questionId}-${answer.matchParticipantId}`;
          answersMap[key] = answer.isAnswerAccepted ? "correct" : "wrong";
        }
        roundStates.value[idx].answers = answersMap;
      }
    }
  }
};

const onTopicChange = async (roundIndex, topicId) => {
  const roundState = roundStates.value[roundIndex];
  roundState.selectedTopicId = topicId;
  roundState.answers = {};

  if (topicId) {
    const existingRound = match.value?.rounds?.find((r) => r.topicId === topicId);
    if (existingRound) {
      roundState.questions = existingRound.questions || [];
    } else {
      try {
        const result = await tournamentService.getTournamentTopic(tournamentId.value, topicId);
        if (result.success) {
          roundState.questions = result.data.questions || [];
        } else {
          toast.add({
            severity: "error",
            summary: "Error",
            detail: result.error || "Failed to load topic questions.",
            life: 3000,
          });
          roundState.questions = [];
        }
      } catch {
        toast.add({ severity: "error", summary: "Error", detail: "Failed to load topic questions.", life: 3000 });
        roundState.questions = [];
      }
    }
  } else {
    roundState.questions = [];
  }
};

const getAnswerValue = (roundIndex, questionId, participantId) => {
  const key = `${questionId}-${participantId}`;
  return roundStates.value[roundIndex].answers[key] || null;
};

const setAnswerValue = (roundIndex, questionId, participantId, value) => {
  const key = `${questionId}-${participantId}`;
  const currentValue = roundStates.value[roundIndex].answers[key];

  if (currentValue === value) {
    roundStates.value[roundIndex].answers[key] = null;
  } else {
    roundStates.value[roundIndex].answers[key] = value;
  }
};

const submitRound = async (roundIndex) => {
  const roundState = roundStates.value[roundIndex];

  if (!roundState.selectedTopicId) {
    toast.add({ severity: "warn", summary: "Warning", detail: "Please select a topic first.", life: 3000 });
    return;
  }

  isProcessing.value = true;

  try {
    const answers = [];
    for (const [key, value] of Object.entries(roundState.answers)) {
      if (value !== null) {
        const [questionId, participantId] = key.split("-").map(Number);
        answers.push({
          questionId,
          matchParticipantId: participantId,
          isAnswerAccepted: value === "correct",
        });
      }
    }

    const roundData = {
      numberInMatch: roundState.numberInMatch,
      topicId: roundState.selectedTopicId,
      answers,
    };

    let result;
    if (roundState.existingRoundId) {
      result = await tournamentService.updateRound(
        tournamentId.value,
        matchId.value,
        roundState.numberInMatch,
        roundData,
      );
    } else {
      result = await tournamentService.createRound(tournamentId.value, matchId.value, roundData);
    }

    if (result.success) {
      toast.add({
        severity: "success",
        summary: "Success",
        detail: `Round ${roundState.numberInMatch} saved successfully.`,
        life: 3000,
      });

      match.value = result.data;

      const topicsResult = await tournamentService.getAvailableTopics(tournamentId.value, matchId.value);
      if (topicsResult.success) {
        availableTopics.value = topicsResult.data;
      }

      initializeRoundStates();
    } else {
      toast.add({ severity: "error", summary: "Error", detail: result.error, life: 5000 });
    }
  } catch (error) {
    console.error("Error submitting round:", error);
    toast.add({ severity: "error", summary: "Error", detail: "Failed to save round.", life: 5000 });
  } finally {
    isProcessing.value = false;
  }
};

const deleteRound = async (roundIndex) => {
  const roundState = roundStates.value[roundIndex];

  if (!roundState.existingRoundId) {
    roundState.selectedTopicId = null;
    roundState.questions = [];
    roundState.answers = {};
    return;
  }

  isProcessing.value = true;

  try {
    const result = await tournamentService.deleteRound(tournamentId.value, matchId.value, roundState.numberInMatch);

    if (result.success) {
      toast.add({
        severity: "success",
        summary: "Success",
        detail: `Round ${roundState.numberInMatch} deleted.`,
        life: 3000,
      });

      match.value = result.data;

      const topicsResult = await tournamentService.getAvailableTopics(tournamentId.value, matchId.value);
      if (topicsResult.success) {
        availableTopics.value = topicsResult.data;
      }

      initializeRoundStates();
    } else {
      toast.add({ severity: "error", summary: "Error", detail: result.error, life: 5000 });
    }
  } catch (error) {
    console.error("Error deleting round:", error);
    toast.add({ severity: "error", summary: "Error", detail: "Failed to delete round.", life: 5000 });
  } finally {
    isProcessing.value = false;
  }
};

const goBack = () => {
  router.push({ name: "tournament-view", params: { id: tournamentId.value } });
};

onMounted(() => {
  fetchData();
});
</script>

<template>
  <Toast />

  <main class="container mx-auto px-4 py-8 max-w-[95%]">
    <!-- Header -->
    <div class="mb-8">
      <div class="flex items-center gap-4 mb-2">
        <Button icon="pi pi-arrow-left" severity="secondary" text rounded @click="goBack" />
        <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ matchTitle }} - Rounds</h1>
      </div>
      <p class="text-gray-600 dark:text-gray-400 ml-14">Manage rounds and record participant answers</p>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="text-center py-16">
      <div class="text-gray-500 dark:text-gray-400">
        <i class="pi pi-spin pi-spinner text-5xl mb-4 block"></i>
        <p class="text-xl">Loading match data...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="loadError" class="text-center py-16">
      <div class="text-gray-500 dark:text-gray-400">
        <i class="pi pi-exclamation-triangle text-5xl mb-4 block text-red-500"></i>
        <p class="text-xl mb-4">{{ loadError }}</p>
        <Button label="Go Back" icon="pi pi-arrow-left" @click="goBack" />
      </div>
    </div>

    <!-- Main Content -->
    <div v-else>
      <!-- Participants Info -->
      <div class="mb-6 p-4 bg-white dark:bg-gray-800 rounded-xl shadow-md">
        <h2 class="text-lg font-semibold mb-3 text-gray-900 dark:text-gray-100">
          <i class="pi pi-users mr-2 text-blue-500"></i>Participants
        </h2>
        <div class="flex flex-wrap gap-4">
          <div
            v-for="participant in participants"
            :key="participant.id"
            class="px-4 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg"
          >
            <span class="font-medium text-gray-800 dark:text-gray-200">{{ participant.username }}</span>
          </div>
        </div>
      </div>

      <!-- Rounds Accordion -->
      <Accordion :multiple="true" :value="['0', '1', '2', '3']">
        <AccordionPanel v-for="(roundState, index) in roundStates" :key="index" :value="String(index)">
          <AccordionHeader>
            <div class="flex items-center gap-3">
              <span class="font-semibold">Round {{ roundState.numberInMatch }}</span>
              <span v-if="roundState.existingRoundId" class="text-green-600 dark:text-green-400 text-sm">
                <i class="pi pi-check-circle mr-1"></i>Saved
              </span>
              <span v-else class="text-gray-400 text-sm"> <i class="pi pi-circle mr-1"></i>Not saved </span>
            </div>
          </AccordionHeader>
          <AccordionContent>
            <div class="p-4 space-y-6">
              <!-- Topic Selection -->
              <div class="flex flex-col gap-2">
                <label class="font-semibold text-gray-700 dark:text-gray-300">Select Topic</label>
                <div class="flex items-center gap-3">
                  <Select
                    :modelValue="roundState.selectedTopicId"
                    @update:modelValue="(val) => onTopicChange(index, val)"
                    :options="getAvailableTopicsForRound(index)"
                    optionLabel="topicTitle"
                    optionValue="topicId"
                    placeholder="Choose a topic..."
                    class="w-full md:w-1/2"
                    :showClear="!roundState.existingRoundId"
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
                        <template v-if="availableTopics.find((t) => t.topicId === value)">
                          <span
                            v-for="topic in availableTopics.filter((t) => t.topicId === value)"
                            :key="topic.topicId"
                            class="flex items-center gap-2"
                          >
                            <span class="font-mono text-sm">#{{ topic.priorityIndex }}</span>
                            <span>{{ topic.topicTitle }}</span>
                            <span class="text-gray-500 text-sm">by {{ topic.ownerUsername }}</span>
                          </span>
                        </template>
                        <!-- Handle case where topic is from existing round but not in available list -->
                        <template v-else>
                          <span class="flex items-center gap-2">
                            <span
                              v-if="match?.rounds?.find((r) => r.topicId === value)"
                              class="text-gray-700 dark:text-gray-300"
                            >
                              {{ match.rounds.find((r) => r.topicId === value)?.topicTitle }}
                              <span class="text-amber-600 dark:text-amber-400 text-sm ml-2">(topic already used)</span>
                            </span>
                            <span v-else class="text-gray-500">Topic ID: {{ value }}</span>
                          </span>
                        </template>
                      </template>
                      <span v-else class="text-gray-400">Choose a topic...</span>
                    </template>
                  </Select>
                </div>
                <small v-if="roundState.existingRoundId" class="text-gray-500 dark:text-gray-400">
                  Topic is locked. Delete the round to select a different topic.
                </small>
              </div>

              <!-- Questions Table (shown when topic is selected) -->
              <div v-if="roundState.selectedTopicId && roundState.questions.length > 0">
                <DataTable
                  :value="roundState.questions"
                  dataKey="id"
                  class="rounds-questions-table"
                  responsiveLayout="scroll"
                  stripedRows
                  size="small"
                >
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
                  <Column
                    v-for="participant in participants"
                    :key="participant.id"
                    :header="participant.username"
                    style="min-width: 140px"
                    class="text-center"
                  >
                    <template #body="{ data }">
                      <div class="flex justify-center gap-3">
                        <div class="flex items-center gap-1">
                          <RadioButton
                            :modelValue="getAnswerValue(index, data.id, participant.id)"
                            :inputId="`correct-${index}-${data.id}-${participant.id}`"
                            name="`answer-${index}-${data.id}-${participant.id}`"
                            value="correct"
                            @click="setAnswerValue(index, data.id, participant.id, 'correct')"
                          />
                          <label
                            :for="`correct-${index}-${data.id}-${participant.id}`"
                            class="text-green-600 dark:text-green-400 text-xs cursor-pointer"
                          >
                            <i class="pi pi-check"></i>
                          </label>
                        </div>
                        <div class="flex items-center gap-1">
                          <RadioButton
                            :modelValue="getAnswerValue(index, data.id, participant.id)"
                            :inputId="`wrong-${index}-${data.id}-${participant.id}`"
                            name="`answer-${index}-${data.id}-${participant.id}`"
                            value="wrong"
                            @click="setAnswerValue(index, data.id, participant.id, 'wrong')"
                          />
                          <label
                            :for="`wrong-${index}-${data.id}-${participant.id}`"
                            class="text-red-600 dark:text-red-400 text-xs cursor-pointer"
                          >
                            <i class="pi pi-times"></i>
                          </label>
                        </div>
                      </div>
                    </template>
                  </Column>
                </DataTable>

                <!-- Action Buttons -->
                <div class="mt-4 flex justify-end gap-2">
                  <Button
                    v-if="roundState.existingRoundId"
                    label="Delete Round"
                    icon="pi pi-trash"
                    severity="danger"
                    outlined
                    :disabled="isProcessing"
                    @click="deleteRound(index)"
                  />
                  <Button
                    :label="roundState.existingRoundId ? 'Update Answers' : 'Save Round'"
                    :icon="roundState.existingRoundId ? 'pi pi-sync' : 'pi pi-save'"
                    :loading="isProcessing"
                    :disabled="isProcessing"
                    @click="submitRound(index)"
                  />
                </div>
              </div>

              <!-- No topic selected message -->
              <div
                v-else-if="!roundState.selectedTopicId"
                class="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-900 rounded-lg"
              >
                <i class="pi pi-info-circle text-3xl mb-2 block"></i>
                <p>Select a topic to view questions and record answers</p>
              </div>

              <!-- No questions available -->
              <div
                v-else-if="roundState.questions.length === 0"
                class="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-900 rounded-lg"
              >
                <i class="pi pi-exclamation-circle text-3xl mb-2 block"></i>
                <p>No questions found for this topic</p>
              </div>
            </div>
          </AccordionContent>
        </AccordionPanel>
      </Accordion>
    </div>
  </main>
</template>

<style scoped>
.rounds-questions-table :deep(.p-datatable-thead > tr > th) {
  background-color: var(--surface-ground);
  padding: 0.75rem 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
}

.rounds-questions-table :deep(.p-datatable-tbody > tr > td) {
  padding: 0.75rem 0.5rem;
  font-size: 0.875rem;
  vertical-align: middle;
}

:deep(.p-accordion-header-link) {
  background-color: var(--surface-card);
}

:deep(.p-accordion-content) {
  background-color: var(--surface-ground);
}
</style>
