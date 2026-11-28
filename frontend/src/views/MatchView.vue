<script setup>
import MatchRoundPanel from "@/components/MatchRoundPanel.vue";
import { LIMITS } from "@/domain/limits.js";
import { tournamentMatchesService } from "@/services/tournament-matches-service.js";
import { tournamentRoundsService } from "@/services/tournament-rounds-service.js";
import { tournamentService } from "@/services/tournament-service.js";
import {
  confirmationFollowUp,
  isMatchFinished,
  isQualificationMatch,
  matchTitle,
  outcomeLabel,
  outcomeTagSeverity,
  playersByOutcome,
  reopenDeletionTitle,
} from "@/utils/match.js";
import { useNotify } from "@/utils/notify.js";
import { isPlayoffStage } from "@/utils/tournament.js";
import Accordion from "primevue/accordion";
import AccordionContent from "primevue/accordioncontent";
import AccordionHeader from "primevue/accordionheader";
import AccordionPanel from "primevue/accordionpanel";
import Button from "primevue/button";
import ConfirmDialog from "primevue/confirmdialog";
import Tag from "primevue/tag";
import { useConfirm } from "primevue/useconfirm";
import { computed, ref, watch } from "vue";
import { RouterLink, useRoute } from "vue-router";

const REOPEN_CONFIRM_GROUP = "reopen-match";

const route = useRoute();
const notify = useNotify();
const confirm = useConfirm();

const tournamentId = computed(() => route.params.tournamentId);
const matchId = computed(() => route.params.matchId);
const isCreator = computed(() => currentUserRole.value === "Creator");
const canEdit = computed(() => isCreator.value && match.value?.isLocked === false);

const match = ref(null);
const matches = ref([]);
const currentUserRole = ref(null);
const stage = ref(null);
const availableTopics = ref([]);
const isLoading = ref(true);
const loadError = ref(null);
const isProcessing = ref(false);
const reopenDeletions = ref([]);

const roundStates = ref([]);

// The playoff match or Final a shootout settles, for its title and links
const settledMatch = computed(() => matches.value.find((other) => other.id === match.value?.settledMatchId));

const title = computed(() => (match.value ? matchTitle(match.value, settledMatch.value) : "Match"));

const participants = computed(() => match.value?.matchParticipants || []);

const roundsPerMatch = computed(() => LIMITS.matches.roundsPerMatch[match.value?.type] ?? 0);

const canAddRound = computed(() => {
  return canEdit.value && roundStates.value.length < roundsPerMatch.value;
});

const isFinished = computed(
  () => match.value != null && isMatchFinished({ type: match.value.type, roundsCount: match.value.rounds.length }),
);
const hasUnsavedRounds = computed(() => roundStates.value.some((round) => round.hasChanges));

const showConfirm = computed(
  () =>
    isCreator.value &&
    isPlayoffStage(stage.value) &&
    match.value != null &&
    !isQualificationMatch(match.value) &&
    !match.value.confirmedAt,
);
const canConfirm = computed(() => isFinished.value && !hasUnsavedRounds.value);
const confirmHint = computed(() => {
  if (!isFinished.value) return `Play all ${roundsPerMatch.value} rounds before confirming.`;
  if (hasUnsavedRounds.value) return "Save the changed rounds before confirming.";
  return "";
});

const showReopen = computed(() => isCreator.value && isPlayoffStage(stage.value) && !!match.value?.confirmedAt);

const followUp = computed(() => (match.value ? confirmationFollowUp(match.value, matches.value) : null));
const matchRoute = (other) => ({
  name: "match-view",
  params: { tournamentId: tournamentId.value, matchId: other.id },
});

const roundsCountLabel = (count) => `${count} ${count === 1 ? "round" : "rounds"}`;

const expandedPanels = ref([]);

// Following a link to another match reloads this page; a slower earlier load mustn't overwrite the newer one
let latestLoad = 0;

const fetchData = async () => {
  const load = ++latestLoad;
  isLoading.value = true;
  loadError.value = null;

  try {
    const [tournamentResult, matchResult, matchesResult] = await Promise.all([
      tournamentService.getTournamentById(tournamentId.value),
      tournamentMatchesService.getMatchWithRounds(tournamentId.value, matchId.value),
      tournamentMatchesService.getMatches(tournamentId.value),
    ]);
    if (load !== latestLoad) return;

    if (!tournamentResult.success) {
      loadError.value = tournamentResult.message;
      notify.error("Tournament Load Failed", tournamentResult.message);
      return;
    }
    currentUserRole.value = tournamentResult.data.currentUserRole;
    stage.value = tournamentResult.data.currentStage;

    if (!matchResult.success) {
      loadError.value = matchResult.message;
      notify.error("Match Load Failed", matchResult.message);
      return;
    }
    if (!matchesResult.success) {
      loadError.value = matchesResult.message;
      notify.error("Match Load Failed", matchesResult.message);
      return;
    }
    match.value = matchResult.data;
    matches.value = matchesResult.data;

    await loadAvailableTopics();
    if (load !== latestLoad) return;

    initializeRoundStates();
    expandedPanels.value = roundStates.value.map((r) => String(r.numberInMatch));
  } catch (error) {
    console.error("Error fetching data:", error);
    loadError.value = "Failed to load data. Please try again.";
    notify.error("Load Failed", "Could not load the match. Please try again.");
  } finally {
    if (load === latestLoad) isLoading.value = false;
  }
};

// Only a match that can be edited offers topics; reopening a match makes it editable again
const loadAvailableTopics = async () => {
  if (!canEdit.value) {
    availableTopics.value = [];
    return;
  }

  const topicsResult = await tournamentMatchesService.getAvailableTopics(tournamentId.value, matchId.value);
  if (topicsResult.success) {
    availableTopics.value = topicsResult.data;
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
  for (let i = 1; i <= roundsPerMatch.value; i++) {
    if (!usedNumbers.has(i)) return i;
  }
  return null;
};

const addRound = () => {
  if (!canAddRound.value) return;

  const nextNumber = getNextRoundNumber();
  if (nextNumber !== null) {
    roundStates.value.push(createEmptyRoundState(nextNumber));
    if (!expandedPanels.value.includes(String(nextNumber))) {
      expandedPanels.value = [...expandedPanels.value, String(nextNumber)];
    }
  }
};

const updateRoundState = (index, updatedState) => {
  roundStates.value[index] = updatedState;
};

const refreshMatchData = async () => {
  const [matchResult, matchesResult] = await Promise.all([
    tournamentMatchesService.getMatchWithRounds(tournamentId.value, matchId.value),
    tournamentMatchesService.getMatches(tournamentId.value),
  ]);

  if (matchResult.success) {
    match.value = matchResult.data;
  } else {
    notify.error("Match Load Failed", matchResult.message);
  }
  if (matchesResult.success) {
    matches.value = matchesResult.data;
  } else {
    notify.error("Match Load Failed", matchesResult.message);
  }

  await loadAvailableTopics();

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
    notify.warn("Topic Required", "Select a topic before saving.");
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
      notify.success("Round saved", `Round ${roundState.numberInMatch} was saved.`);
      await refreshMatchData();
    } else {
      notify.error("Save Failed", result.message);
    }
  } catch (error) {
    console.error("Error submitting round:", error);
    notify.error("Save Failed", "Could not save the round. Please try again.");
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
      notify.success("Round deleted", `Round ${roundState.numberInMatch} was deleted.`);
      await refreshMatchData();
    } else {
      notify.error("Delete Failed", result.message);
    }
  } catch (error) {
    console.error("Error deleting round:", error);
    notify.error("Delete Failed", "Could not delete the round. Please try again.");
  } finally {
    isProcessing.value = false;
  }
};

const handleConfirm = () => {
  confirm.require({
    message: `Are you sure you want to confirm ${title.value}? Its rounds will be locked, and the shootout or next matches its result calls for will be created. Reopen it to correct it later.`,
    header: `Confirm ${title.value}`,
    icon: "pi pi-exclamation-triangle",
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    acceptProps: { label: "Confirm" },
    accept: async () => {
      isProcessing.value = true;
      try {
        const confirmResult = await tournamentMatchesService.confirmMatch(tournamentId.value, matchId.value);
        if (confirmResult.success) {
          notify.success("Match confirmed", `${title.value} was confirmed.`);
          await refreshMatchData();
        } else {
          notify.error("Confirm Failed", confirmResult.message);
        }
      } catch (error) {
        console.error("Error confirming the match:", error);
        notify.error("Confirm Failed", "Could not confirm the match. Please try again.");
      } finally {
        isProcessing.value = false;
      }
    },
  });
};

const reopen = async () => {
  isProcessing.value = true;
  try {
    const reopenResult = await tournamentMatchesService.reopenMatch(tournamentId.value, matchId.value);
    if (reopenResult.success) {
      notify.success("Match reopened", `${title.value} can be edited again.`);
      await refreshMatchData();
    } else {
      notify.error("Reopen Failed", reopenResult.message);
    }
  } catch (error) {
    console.error("Error reopening the match:", error);
    notify.error("Reopen Failed", "Could not reopen the match. Please try again.");
  } finally {
    isProcessing.value = false;
  }
};

const handleReopen = async () => {
  isProcessing.value = true;
  try {
    const deletionsResult = await tournamentMatchesService.getReopenDeletions(tournamentId.value, matchId.value);
    if (!deletionsResult.success) {
      notify.error("Reopen Failed", deletionsResult.message);
      return;
    }
    reopenDeletions.value = deletionsResult.data;
  } catch (error) {
    console.error("Error fetching what reopening deletes:", error);
    notify.error("Reopen Failed", "Could not check what reopening deletes. Please try again.");
    return;
  } finally {
    isProcessing.value = false;
  }

  const deletesMatches = reopenDeletions.value.length > 0;
  confirm.require({
    group: REOPEN_CONFIRM_GROUP,
    message: deletesMatches
      ? `Are you sure you want to reopen ${title.value}? These matches were created from its result and will be deleted with their rounds:`
      : `Are you sure you want to reopen ${title.value}? Its rounds can be edited again; no other match is deleted.`,
    header: `Reopen ${title.value}`,
    icon: "pi pi-exclamation-triangle",
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    acceptProps: { label: "Reopen", severity: deletesMatches ? "danger" : undefined },
    accept: reopen,
  });
};

const backRoute = computed(() => ({
  name: "tournament-view",
  params: { tournamentId: tournamentId.value },
}));

// Links to a shootout or the match it settles stay on this page, so a new match id loads afresh. Leaving the page
// clears the id before the view unmounts, which mustn't load anything.
watch(
  matchId,
  (id) => {
    if (id) fetchData();
  },
  { immediate: true },
);
</script>

<template>
  <main class="container mx-auto px-4 py-8 max-w-[95%]">
    <!-- Header -->
    <div class="mb-8">
      <div class="flex items-center gap-4 mb-2">
        <RouterLink :to="backRoute" custom v-slot="{ navigate }">
          <Button icon="pi pi-arrow-left" severity="secondary" text rounded @click="navigate" />
        </RouterLink>
        <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ title }}</h1>
        <span v-if="match?.confirmedAt" class="text-sm text-gray-500 dark:text-gray-400">
          <i class="pi pi-lock mr-1"></i>Confirmed
        </span>
        <span v-else-if="match?.isLocked" class="text-sm text-gray-500 dark:text-gray-400">
          <i class="pi pi-lock mr-1"></i>Locked
        </span>
        <!-- The span shows the tooltip, since a disabled button gets no mouse events -->
        <span
          v-if="!isLoading && showConfirm"
          v-tooltip.top="{ value: confirmHint, disabled: canConfirm }"
          class="inline-flex"
        >
          <Button
            label="Confirm"
            icon="pi pi-check"
            severity="success"
            :disabled="!canConfirm || isProcessing"
            @click="handleConfirm"
          />
        </span>
        <Button
          v-if="!isLoading && showReopen"
          label="Reopen"
          icon="pi pi-lock-open"
          severity="secondary"
          outlined
          :disabled="isProcessing"
          @click="handleReopen"
        />
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

      <!-- After confirming: the shootout it created, or the Outcomes of the match it decided -->
      <div
        v-if="followUp && (followUp.shootout || followUp.decidedMatch)"
        class="mb-6 p-4 bg-white dark:bg-gray-800 rounded-xl shadow-md"
      >
        <h2 class="text-lg font-semibold mb-3 text-gray-900 dark:text-gray-100">
          <i class="pi pi-flag mr-2 text-green-500"></i>Outcome
        </h2>

        <p v-if="followUp.shootout" class="mb-3 text-gray-700 dark:text-gray-300">
          The tied players play
          <RouterLink :to="matchRoute(followUp.shootout)" class="font-medium text-blue-600 hover:underline">
            {{ matchTitle(followUp.shootout, followUp.settledMatch) }}
          </RouterLink>
        </p>

        <template v-if="followUp.decidedMatch">
          <p v-if="followUp.decidedMatch.id !== match.id" class="mb-3 text-gray-700 dark:text-gray-300">
            This decided
            <RouterLink :to="matchRoute(followUp.decidedMatch)" class="font-medium text-blue-600 hover:underline">
              {{ matchTitle(followUp.decidedMatch) }}
            </RouterLink>
          </p>
          <ul class="flex flex-col gap-2">
            <li
              v-for="player in playersByOutcome(followUp.decidedMatch)"
              :key="player.id"
              class="flex items-center gap-3"
            >
              <Tag :value="outcomeLabel(player)" :severity="outcomeTagSeverity(player)" class="text-xs min-w-24" />
              <span class="font-medium text-gray-800 dark:text-gray-200">{{ player.username }}</span>
            </li>
          </ul>
        </template>
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
      <Accordion v-if="roundStates.length > 0" :multiple="true" v-model:value="expandedPanels">
        <AccordionPanel
          v-for="(roundState, index) in roundStates"
          :key="roundState.numberInMatch"
          :value="String(roundState.numberInMatch)"
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
          {{ roundStates.length }} of {{ roundsPerMatch }} rounds
        </p>
      </div>

      <!-- Max rounds reached message -->
      <div v-else-if="canEdit && roundStates.length >= roundsPerMatch" class="mt-6 text-center">
        <p class="text-sm text-gray-500 dark:text-gray-400">
          <i class="pi pi-check-circle mr-1 text-green-500"></i>
          Maximum number of rounds ({{ roundsPerMatch }}) reached
        </p>
      </div>
    </div>

    <!-- Reopen confirmation, listing the matches it deletes -->
    <ConfirmDialog :group="REOPEN_CONFIRM_GROUP">
      <template #message="{ message }">
        <div class="flex flex-col gap-3">
          <p>{{ message.message }}</p>
          <ul v-if="reopenDeletions.length > 0" class="list-disc pl-6 text-sm">
            <li v-for="deletion in reopenDeletions" :key="deletion.matchId">
              {{ reopenDeletionTitle(deletion) }}: {{ roundsCountLabel(deletion.roundsCount) }}
            </li>
          </ul>
        </div>
      </template>
    </ConfirmDialog>
  </main>
</template>

<style scoped></style>
