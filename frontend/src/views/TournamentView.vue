<script setup>
import MatchCard from "@/components/MatchCard.vue";
import { tournamentService } from "@/services/tournament-service.js";
import Badge from "primevue/badge";
import Toast from "primevue/toast";
import { useToast } from "primevue/usetoast";
import { computed, onMounted, ref } from "vue";
import { useRoute } from "vue-router";

const route = useRoute();
const toast = useToast();

const tournamentId = computed(() => parseInt(route.params.id));
const tournament = ref(null);
const matches = ref([]);
const isLoadingTournament = ref(true);
const isLoadingMatches = ref(false);
const loadError = ref(null);

const isInPreparations = computed(() => tournament.value?.currentStage === "Preparations");

const stageBadgeSeverity = computed(() => {
  switch (tournament.value?.currentStage) {
    case "Preparations":
      return "secondary";
    case "Qualifications":
      return "info";
    default:
      return "success";
  }
});

const fetchTournament = async () => {
  isLoadingTournament.value = true;
  loadError.value = null;

  try {
    const result = await tournamentService.getTournamentById(tournamentId.value);
    if (result.success) {
      tournament.value = result.data;
    } else {
      loadError.value = result.error;
      toast.add({
        severity: "error",
        summary: "Error",
        detail: result.error,
        life: 5000,
      });
    }
  } catch (error) {
    console.error("Error fetching tournament:", error);
    loadError.value = "Failed to load tournament. Please try again.";
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "Failed to load tournament. Please try again.",
      life: 5000,
    });
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
    const result = await tournamentService.getMatches(tournamentId.value);
    if (result.success) {
      matches.value = result.data;
    } else {
      console.error("Failed to fetch matches:", result.error);
      toast.add({
        severity: "error",
        summary: "Error",
        detail: result.error,
        life: 5000,
      });
    }
  } catch (error) {
    console.error("Error fetching matches:", error);
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "Failed to load matches. Please try again.",
      life: 5000,
    });
  } finally {
    isLoadingMatches.value = false;
  }
};

onMounted(async () => {
  await fetchTournament();
  if (tournament.value && !isInPreparations.value) {
    await fetchMatches();
  }
});
</script>

<template>
  <Toast />

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
          <Badge :value="tournament.currentStage" :severity="stageBadgeSeverity" class="text-sm" />
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
          <p class="text-sm">Matches will be generated when the tournament starts.</p>
        </div>
      </div>

      <!-- Matches Section -->
      <div v-else>
        <!-- Loading Matches -->
        <div v-if="isLoadingMatches" class="text-center py-12">
          <div class="text-gray-500 dark:text-gray-400">
            <i class="pi pi-spin pi-spinner text-4xl mb-4 block"></i>
            <p class="text-lg">Loading matches...</p>
          </div>
        </div>

        <!-- Matches Grid -->
        <div v-else>
          <div v-if="matches.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <MatchCard v-for="match in matches" :key="match.id" :match="match" />
          </div>

          <div v-else class="text-center py-12">
            <div class="text-gray-500 dark:text-gray-400">
              <i class="pi pi-inbox text-4xl mb-4 block"></i>
              <p class="text-lg">No matches found</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>
