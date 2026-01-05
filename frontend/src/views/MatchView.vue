<script setup>
import MatchRoundPanel from "@/components/MatchRoundPanel.vue";
import { tournamentMatchesService } from "@/services/tournament-matches-service.js";
import { tournamentRoundsService } from "@/services/tournament-rounds-service.js";
import { tournamentService } from "@/services/tournament-service.js";
import { useNotify } from "@/utils/notify.js";
import Accordion from "primevue/accordion";
import AccordionContent from "primevue/accordioncontent";
import AccordionHeader from "primevue/accordionheader";
import AccordionPanel from "primevue/accordionpanel";
import Button from "primevue/button";
import Toast from "primevue/toast";
import { computed, onMounted, ref } from "vue";
import { RouterLink, useRoute } from "vue-router";

const MAX_ROUNDS = 4;

const route = useRoute();
const notify = useNotify();

const tournamentId = computed(() => route.params.tournamentId);
const matchId = computed(() => route.params.matchId);
const canEdit = computed(() => currentUserRole.value === "Creator");

const match = ref(null);
const currentUserRole = ref(null);
const availableTopics = ref([]);
const isLoading = ref(true);
const loadError = ref(null);
const isProcessing = ref(false);

const roundStates = ref([]);

const matchTitle = computed(() => {
  if (!match.value) return "Match";
  const stage = match.value.createdOnStage || "Match";
  const number = match.value.numberInStage || match.value.numberInTournament;
  return `${stage} #${number}`;
});

const participants = computed(() => match.value?.matchParticipants || []);

const canAddRound = computed(() => {
  return canEdit.value && roundStates.value.length < MAX_ROUNDS;
});

const expandedPanels = computed(() => roundStates.value.map((_, idx) => String(idx)));

const fetchData = async () => {
  isLoading.value = true;
  loadError.value = null;

  try {
    const tournamentResult = await tournamentService.getTournamentById(tournamentId.value);
    if (!tournamentResult.success) {
      loadError.value = tournamentResult.error;
      notify.error("Tournament Load Failed", tournamentResult.error);
      return;
    }
    currentUserRole.value = tournamentResult.data.currentUserRole;

    const matchResult = await tournamentMatchesService.getMatchWithRounds(tournamentId.value, matchId.value);
    if (!matchResult.success) {
      loadError.value = matchResult.error;
      notify.error("Match Load Failed", matchResult.error);
      return;
    }
    match.value = matchResult.data;

    if (currentUserRole.value === "Creator") {
      const topicsResult = await tournamentMatchesService.getAvailableTopics(tournamentId.value, matchId.value);
      if (topicsResult.success) {
        availableTopics.value = topicsResult.data;
      }
    }

    initializeRoundStates();
  } catch (error) {
    console.error("Error fetching data:", error);
    loadError.value = "Failed to load data. Please try again.";
    notify.error("Load Failed", "Could not load match data");
  } finally {
    isLoading.value = false;
  }
};

const createEmptyRoundState = (numberInMatch) => ({
  numberInMatch,
  selectedTopicId: null,
  questions: [],
  answers: {},
  overrideCosts: {},
  isOverrideMode: false,
  existingRoundId: null,
  hasChanges: false,
});

const initializeRoundStates = () => {
  const newRoundStates = [];

  if (match.value?.rounds && match.value.rounds.length > 0) {
    const sortedRounds = [...match.value.rounds].sort((a, b) => a.numberInMatch - b.numberInMatch);

    for (const round of sortedRounds) {
      const answersMap = {};
      const overrideCostsMap = {};
      const isOverrideMode = round.isOverrideMode || false;

      for (const answer of round.roundAnswers || []) {
        const key = `${answer.questionId}::${answer.matchParticipantId}`;
        if (isOverrideMode) {
          overrideCostsMap[key] = answer.overrideCost;
        } else {
          answersMap[key] = answer.isAnswerAccepted;
        }
      }

      newRoundStates.push({
        numberInMatch: round.numberInMatch,
        existingRoundId: round.id,
        selectedTopicId: round.topicId,
        questions: round.questions || [],
        answers: answersMap,
        overrideCosts: overrideCostsMap,
        isOverrideMode,
        hasChanges: false,
      });
    }
  }

  roundStates.value = newRoundStates;
};

const getNextRoundNumber = () => {
  if (roundStates.value.length === 0) return 1;

  const usedNumbers = new Set(roundStates.value.map((r) => r.numberInMatch));
  for (let i = 1; i <= MAX_ROUNDS; i++) {
    if (!usedNumbers.has(i)) return i;
  }
  return null;
};

const addRound = () => {
  if (!canAddRound.value) return;

  const nextNumber = getNextRoundNumber();
  if (nextNumber !== null) {
    roundStates.value.push(createEmptyRoundState(nextNumber));
  }
};

const updateRoundState = (index, updatedState) => {
  roundStates.value[index] = updatedState;
};

const refreshMatchData = async () => {
  const [matchResult, topicsResult] = await Promise.all([
    tournamentMatchesService.getMatchWithRounds(tournamentId.value, matchId.value),
    tournamentMatchesService.getAvailableTopics(tournamentId.value, matchId.value),
  ]);

  if (matchResult.success) {
    match.value = matchResult.data;
  }
  if (topicsResult.success) {
    availableTopics.value = topicsResult.data;
  }

  initializeRoundStates();
};

const validateAnswers = (answers, isOverrideMode) => {
  if (isOverrideMode) {
    return null;
  }

  const correctAnswersByQuestion = {};
  for (const answer of answers) {
    if (answer.isAnswerAccepted) {
      if (correctAnswersByQuestion[answer.questionId]) {
        return `Question has multiple correct answers. Only one correct answer is allowed per question.`;
      }
      correctAnswersByQuestion[answer.questionId] = true;
    }
  }
  return null;
};

const submitRound = async (roundIndex) => {
  if (!canEdit.value) return;

  const roundState = roundStates.value[roundIndex];

  if (!roundState.selectedTopicId) {
    notify.warn("Topic Required", "Select a topic before saving");
    return;
  }

  const answers = [];
  const isOverrideMode = roundState.isOverrideMode || false;

  if (isOverrideMode) {
    for (const [key, value] of Object.entries(roundState.overrideCosts)) {
      if (value !== null && value !== undefined) {
        const [questionId, participantId] = key.split("::");
        answers.push({
          questionId,
          matchParticipantId: participantId,
          overrideCost: value,
          isAnswerAccepted: null,
        });
      }
    }
  } else {
    for (const [key, value] of Object.entries(roundState.answers)) {
      if (value !== null) {
        const [questionId, participantId] = key.split("::");
        answers.push({
          questionId,
          matchParticipantId: participantId,
          isAnswerAccepted: value,
          overrideCost: null,
        });
      }
    }
  }

  const validationError = validateAnswers(answers, isOverrideMode);
  if (validationError) {
    notify.error("Validation Error", validationError);
    return;
  }

  isProcessing.value = true;

  try {
    const roundData = {
      numberInMatch: roundState.numberInMatch,
      topicId: roundState.selectedTopicId,
      isOverrideMode,
      answers,
    };

    let result;
    if (roundState.existingRoundId) {
      result = await tournamentRoundsService.updateRound(
        tournamentId.value,
        matchId.value,
        roundState.numberInMatch,
        roundData,
      );
    } else {
      result = await tournamentRoundsService.createRound(tournamentId.value, matchId.value, roundData);
    }

    if (result.success) {
      notify.success("Round Saved", `Round ${roundState.numberInMatch} saved successfully`);
      await refreshMatchData();
    } else {
      notify.error("Save Failed", result.error);
    }
  } catch (error) {
    console.error("Error submitting round:", error);
    notify.error("Save Failed", "Could not save round data");
  } finally {
    isProcessing.value = false;
  }
};

const deleteRound = async (roundIndex) => {
  if (!canEdit.value) return;

  const roundState = roundStates.value[roundIndex];

  if (!roundState.existingRoundId) {
    roundStates.value.splice(roundIndex, 1);
    return;
  }

  isProcessing.value = true;

  try {
    const result = await tournamentRoundsService.deleteRound(
      tournamentId.value,
      matchId.value,
      roundState.numberInMatch,
    );
    if (result.success) {
      notify.success("Round Deleted", `Round ${roundState.numberInMatch} removed`);
      await refreshMatchData();
    } else {
      notify.error("Delete Failed", result.error);
    }
  } catch (error) {
    console.error("Error deleting round:", error);
    notify.error("Delete Failed", "Could not remove round");
  } finally {
    isProcessing.value = false;
  }
};

const backRoute = computed(() => ({
  name: "tournament-view",
  params: { tournamentId: tournamentId.value },
}));

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
        <RouterLink :to="backRoute" custom v-slot="{ navigate }">
          <Button icon="pi pi-arrow-left" severity="secondary" text rounded @click="navigate" />
        </RouterLink>
        <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ matchTitle }}</h1>
      </div>
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
        <RouterLink :to="backRoute" custom v-slot="{ navigate }">
          <Button label="Go Back" icon="pi pi-arrow-left" @click="navigate" />
        </RouterLink>
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

      <!-- No Rounds Message -->
      <div
        v-if="roundStates.length === 0 && !canEdit"
        class="text-center py-16 bg-white dark:bg-gray-800 rounded-xl shadow-md"
      >
        <i class="pi pi-info-circle text-5xl mb-4 block text-gray-400"></i>
        <p class="text-xl text-gray-500 dark:text-gray-400">No rounds have been played yet</p>
      </div>

      <!-- Rounds Accordion -->
      <Accordion v-if="roundStates.length > 0" :multiple="true" :value="expandedPanels">
        <AccordionPanel
          v-for="(roundState, index) in roundStates"
          :key="roundState.numberInMatch"
          :value="String(index)"
        >
          <AccordionHeader>
            <div class="flex items-center gap-3 w-full">
              <span class="font-semibold">Round {{ roundState.numberInMatch }}</span>
              <span v-if="canEdit && roundState.hasChanges" class="text-sm text-amber-600 dark:text-amber-400">
                <i class="pi pi-exclamation-circle mr-1"></i>Unsaved changes
              </span>
            </div>
          </AccordionHeader>
          <AccordionContent>
            <MatchRoundPanel
              :roundState="roundState"
              :roundIndex="index"
              :participants="participants"
              :availableTopics="availableTopics"
              :allRoundStates="roundStates"
              :matchRounds="match?.rounds || []"
              :tournamentId="tournamentId"
              :canEdit="canEdit"
              :isProcessing="isProcessing"
              @update:roundState="(state) => updateRoundState(index, state)"
              @submit="submitRound"
              @delete="deleteRound"
            />
          </AccordionContent>
        </AccordionPanel>
      </Accordion>

      <!-- Add Round Button -->
      <div v-if="canAddRound" class="mt-6">
        <Button
          label="Add Round"
          icon="pi pi-plus"
          severity="secondary"
          outlined
          class="w-full"
          :disabled="isProcessing"
          @click="addRound"
        />
        <p class="text-center text-sm text-gray-500 dark:text-gray-400 mt-2">
          {{ roundStates.length }} of {{ MAX_ROUNDS }} rounds
        </p>
      </div>

      <!-- Max rounds reached message -->
      <div v-else-if="canEdit && roundStates.length >= MAX_ROUNDS" class="mt-6 text-center">
        <p class="text-sm text-gray-500 dark:text-gray-400">
          <i class="pi pi-check-circle mr-1 text-green-500"></i>
          Maximum number of rounds ({{ MAX_ROUNDS }}) reached
        </p>
      </div>
    </div>
  </main>
</template>

<style scoped></style>
