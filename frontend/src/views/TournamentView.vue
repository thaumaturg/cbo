<script setup>
import MatchCard from "@/components/MatchCard.vue";
import TournamentParticipantsPanel from "@/components/TournamentParticipantsPanel.vue";
import TournamentSettingsPanel from "@/components/TournamentSettingsPanel.vue";
import TournamentStagePanel from "@/components/TournamentStagePanel.vue";
import TournamentStandingsCard from "@/components/TournamentStandingsCard.vue";
import TournamentTopicsPanel from "@/components/TournamentTopicsPanel.vue";
import { tournamentMatchesService } from "@/services/tournament-matches-service.js";
import { tournamentParticipantsService } from "@/services/tournament-participants-service.js";
import { tournamentService } from "@/services/tournament-service.js";
import { useNotify } from "@/utils/notify.js";
import { isPreparationsStage, stageTagSeverity } from "@/utils/tournament.js";
import { useUnsavedChangesGuard } from "@/utils/unsaved-changes.js";
import Button from "primevue/button";
import Tab from "primevue/tab";
import TabList from "primevue/tablist";
import TabPanel from "primevue/tabpanel";
import TabPanels from "primevue/tabpanels";
import Tabs from "primevue/tabs";
import Tag from "primevue/tag";
import { useConfirm } from "primevue/useconfirm";
import { computed, nextTick, onMounted, ref } from "vue";
import { useRoute } from "vue-router";

const route = useRoute();
const notify = useNotify();
const confirm = useConfirm();

const tournamentId = computed(() => route.params.tournamentId);
const tournament = ref(null);
const matches = ref([]);
const participants = ref([]);
const isLoadingTournament = ref(true);
const isLoadingMatches = ref(false);
const isLoadingParticipants = ref(false);
const loadError = ref(null);

const isStartingQualifications = ref(false);
const activeTab = ref("participants");

const isInPreparations = computed(() => isPreparationsStage(tournament.value?.currentStage));
const isCreator = computed(() => tournament.value?.currentUserRole === "Creator");
// Only the Creator can change the tournament, and only before Start Qualifications
const canManage = computed(() => isCreator.value && isInPreparations.value);

const participantsReadOnly = computed(() => !canManage.value);
const topicsReadOnly = computed(() => !isInPreparations.value);
const settingsReadOnly = computed(() => !canManage.value);

const stageSeverity = computed(() => stageTagSeverity(tournament.value?.currentStage));

const settingsPanel = ref(null);
const settingsDirty = ref(false);
const { confirmDiscard } = useUnsavedChangesGuard(settingsDirty);

const handleTabChange = async (nextTab) => {
  if (activeTab.value !== "settings" || !settingsDirty.value) {
    activeTab.value = nextTab;
    return;
  }

  // Tabs has already switched itself and only follows `value` when it changes, so send it to the
  // new tab and back to keep Settings on screen while the user decides
  activeTab.value = nextTab;
  await nextTick();
  activeTab.value = "settings";

  if (await confirmDiscard()) {
    settingsPanel.value?.reset();
    activeTab.value = nextTab;
  }
};

const handleTournamentUpdated = (updatedTournament) => {
  tournament.value = updatedTournament;
};

// A roll back wipes matches and totals, so they are loaded again for the stage it went back to
const handleRolledBack = async (rolledBackTournament) => {
  tournament.value = rolledBackTournament;
  await Promise.all([fetchMatches(), fetchParticipants()]);
};

const fetchTournament = async () => {
  isLoadingTournament.value = true;
  loadError.value = null;

  try {
    const result = await tournamentService.getTournamentById(tournamentId.value);
    if (result.success) {
      tournament.value = result.data;
    } else {
      loadError.value = result.message;
      notify.error("Tournament Load Failed", result.message);
    }
  } catch (error) {
    console.error("Error fetching tournament:", error);
    loadError.value = "Failed to load tournament. Please try again.";
    notify.error("Tournament Load Failed", "Could not load the tournament. Please try again.");
  } finally {
    isLoadingTournament.value = false;
  }
};

const fetchMatches = async () => {
  if (isInPreparations.value) {
    matches.value = [];
    return;
  }

  isLoadingMatches.value = true;

  try {
    const result = await tournamentMatchesService.getMatches(tournamentId.value);
    if (result.success) {
      matches.value = result.data;
    } else {
      console.error("Failed to fetch matches:", result.message);
      notify.error("Matches Load Failed", result.message);
    }
  } catch (error) {
    console.error("Error fetching matches:", error);
    notify.error("Matches Load Failed", "Could not load the matches. Please try again.");
  } finally {
    isLoadingMatches.value = false;
  }
};

const fetchParticipants = async () => {
  if (isInPreparations.value) {
    participants.value = [];
    return;
  }

  isLoadingParticipants.value = true;

  try {
    const result = await tournamentParticipantsService.getAllParticipants(tournamentId.value, "Player");
    if (result.success) {
      participants.value = result.data;
    } else {
      console.error("Failed to fetch participants:", result.message);
    }
  } catch (error) {
    console.error("Error fetching participants:", error);
  } finally {
    isLoadingParticipants.value = false;
  }
};

// Reloads the tournament while the page stays on screen, unlike fetchTournament's loading state
const refreshTournament = async () => {
  const result = await tournamentService.getTournamentById(tournamentId.value);
  if (result.success) {
    tournament.value = result.data;
  } else {
    notify.error("Tournament Load Failed", result.message);
  }
};

const handleStartQualifications = () => {
  const { id, title } = tournament.value;

  confirm.require({
    message: `Are you sure you want to start Qualifications in "${title}"? Its qualification matches will be generated.`,
    header: "Start Qualifications",
    icon: "pi pi-exclamation-triangle",
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    acceptProps: { label: "Start Qualifications" },
    accept: async () => {
      isStartingQualifications.value = true;
      try {
        const result = await tournamentService.startQualifications(id);

        if (result.success) {
          notify.success("Qualifications started", `"${title}" is in Qualifications.`);
          await refreshTournament();
          await Promise.all([fetchMatches(), fetchParticipants()]);
        } else {
          notify.error("Start Qualifications Failed", result.message);
        }
      } catch (error) {
        console.error("Error starting Qualifications:", error);
        notify.error("Start Qualifications Failed", "An unexpected error occurred. Please try again.");
      } finally {
        isStartingQualifications.value = false;
      }
    },
  });
};

onMounted(async () => {
  await fetchTournament();
  if (tournament.value && !isInPreparations.value) {
    await Promise.all([fetchMatches(), fetchParticipants()]);
  }
});
</script>

<template>
  <main class="container mx-auto px-4 py-8">
    <!-- Loading State -->
    <div v-if="isLoadingTournament" class="text-center py-16">
      <div class="text-gray-500 dark:text-gray-400">
        <i class="pi pi-spin pi-spinner text-5xl mb-4 block"></i>
        <p class="text-xl">Loading tournament...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="loadError" class="text-center py-16">
      <div class="text-gray-500 dark:text-gray-400">
        <i class="pi pi-exclamation-triangle text-5xl mb-4 block text-red-500"></i>
        <p class="text-xl mb-4">{{ loadError }}</p>
      </div>
    </div>

    <!-- Tournament Content -->
    <div v-else-if="tournament">
      <!-- Header -->
      <div class="mb-8">
        <div class="flex items-center gap-4 mb-2">
          <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100">
            {{ tournament.title }}
          </h1>
          <Tag :value="tournament.currentStage" :severity="stageSeverity" class="text-sm" />
          <Button
            v-if="canManage"
            label="Start Qualifications"
            icon="pi pi-play"
            severity="success"
            :loading="isStartingQualifications"
            @click="handleStartQualifications"
          />
        </div>
        <p v-if="tournament.description" class="text-gray-600 dark:text-gray-400 ml-14">
          {{ tournament.description }}
        </p>
      </div>

      <!-- Preparations Stage Message -->
      <div v-if="isInPreparations" class="text-center py-16">
        <div class="text-gray-500 dark:text-gray-400">
          <i class="pi pi-clock text-5xl mb-4 block"></i>
          <p class="text-xl mb-2">Tournament is in Preparations</p>
          <p class="text-sm">Matches will be generated when Qualifications start.</p>
        </div>
      </div>

      <!-- Matches Section -->
      <div v-else>
        <!-- Loading State -->
        <div v-if="isLoadingMatches || isLoadingParticipants" class="text-center py-12">
          <div class="text-gray-500 dark:text-gray-400">
            <i class="pi pi-spin pi-spinner text-4xl mb-4 block"></i>
            <p class="text-lg">Loading tournament data...</p>
          </div>
        </div>

        <!-- Tournament Content -->
        <div v-else>
          <!-- Standings -->
          <TournamentStandingsCard v-if="participants.length > 0" :participants="participants" />

          <!-- Matches Grid -->
          <div v-if="matches.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <MatchCard v-for="match in matches" :key="match.id" :match="match" :tournamentId="tournamentId" />
          </div>

          <div v-else class="text-center py-12">
            <div class="text-gray-500 dark:text-gray-400">
              <i class="pi pi-inbox text-4xl mb-4 block"></i>
              <p class="text-lg">No matches found</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Management Tabs -->
      <Tabs :value="activeTab" class="mt-8" @update:value="handleTabChange">
        <TabList>
          <Tab value="participants">Participants</Tab>
          <Tab value="my-topics">My Topics</Tab>
          <Tab value="settings">Settings</Tab>
        </TabList>
        <TabPanels>
          <TabPanel value="participants">
            <TournamentParticipantsPanel :tournament="tournament" :read-only="participantsReadOnly" />
          </TabPanel>
          <TabPanel value="my-topics">
            <TournamentTopicsPanel :tournament="tournament" :read-only="topicsReadOnly" />
          </TabPanel>
          <TabPanel value="settings">
            <TournamentStagePanel :tournament="tournament" @rolled-back="handleRolledBack" />
            <TournamentSettingsPanel
              ref="settingsPanel"
              v-model:dirty="settingsDirty"
              :tournament="tournament"
              :read-only="settingsReadOnly"
              @updated="handleTournamentUpdated"
            />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  </main>
</template>
