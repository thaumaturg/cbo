<script setup>
import TournamentCard from "@/components/TournamentCard.vue";
import TopicCard from "@/components/TopicCard.vue";
import CreateNewButton from "@/components/CreateNewButton.vue";
import CreateTournamentDialog from "@/components/CreateTournamentDialog.vue";
import TournamentParticipantsDialog from "@/components/TournamentParticipantsDialog.vue";
import Toast from "primevue/toast";
import { tournamentService } from "@/services/tournament-service.js";
import { ref, onMounted, watch } from "vue";
import { useToast } from "primevue/usetoast";
import { useAuthStore } from "@/stores/auth.js";

const toast = useToast();
const authStore = useAuthStore();

const showCreateTournamentDialog = ref(false);
const showParticipantsDialog = ref(false);
const selectedTournament = ref(null);
const tournaments = ref([]);
const isLoadingTournaments = ref(false);

const topics = ref([
  {
    id: 1,
    name: "World History",
    authors: ["John Smith", "Jane Doe"],
    description:
      "Explore major historical events and their impact on modern civilization. From ancient empires to world wars.",
    isGuest: true,
    isPlayed: true,
  },
  {
    id: 2,
    name: "Science & Technology",
    authors: ["Alice Johnson"],
    description: "Dive into fascinating questions about physics, chemistry, biology, and cutting-edge technology.",
    isGuest: true,
    isPlayed: false,
  },
  {
    id: 3,
    name: "Pop Culture Trivia",
    authors: ["Bob Williams", "Sarah Brown"],
    description: "Test your knowledge of movies, music, TV shows, and celebrity gossip from the past decades.",
    isGuest: false,
    isPlayed: true,
  },
  {
    id: 4,
    name: "Geography Challenge",
    authors: ["Mike Davis"],
    description: "Journey across continents with questions about countries, capitals, landmarks, and cultures.",
    isGuest: false,
    isPlayed: false,
  },
]);

const fetchTournaments = async () => {
  if (!authStore.isAuthenticated) {
    return;
  }

  isLoadingTournaments.value = true;
  try {
    const result = await tournamentService.getAllTournaments();
    if (result.success) {
      tournaments.value = result.data;
    } else {
      console.error("Failed to fetch tournaments:", result.error);
    }
  } catch (error) {
    console.error("Error fetching tournaments:", error);
  } finally {
    isLoadingTournaments.value = false;
  }
};

onMounted(() => {
  fetchTournaments();
});

watch(
  () => authStore.isAuthenticated,
  (isAuthenticated) => {
    if (isAuthenticated) {
      fetchTournaments();
    } else {
      tournaments.value = [];
    }
  }
);

const handleTournamentSettings = (tournament) => {
  console.log("Settings for:", tournament.title);
  // open a settings modal or navigate to settings page
};

const handleTournamentParticipants = (tournament) => {
  selectedTournament.value = tournament;
  showParticipantsDialog.value = true;
};

const handleTournamentStart = (tournament) => {
  console.log("Starting tournament:", tournament.title);
  // start the tournament or navigate to tournament page
};

const handleTournamentDelete = async (tournament) => {
  if (!authStore.isAuthenticated) {
    return;
  }

  const tournamentTitle = tournament.title;
  if (confirm(`Are you sure you want to delete "${tournamentTitle}"?`)) {
    // Optimistic update - remove from UI immediately
    const index = tournaments.value.findIndex((t) => t.id === tournament.id);
    if (index > -1) {
      tournaments.value.splice(index, 1);
    }

    const result = await tournamentService.deleteTournament(tournament.id);

    if (!result.success) {
      console.error("Failed to delete tournament:", result.error);
      toast.add({
        severity: "error",
        summary: "Delete Failed",
        detail: `Failed to delete "${tournamentTitle}". ${result.error}`,
        life: 5000,
      });
      // Re-fetch to restore the tournament if deletion failed
      await fetchTournaments();
    } else {
      toast.add({
        severity: "success",
        summary: "Tournament Deleted",
        detail: `"${tournamentTitle}" has been deleted successfully.`,
        life: 3000,
      });
      // Background validation - ensure UI is in sync
      await fetchTournaments();
    }
  }
};

const handleTopicView = (topic) => {
  console.log("View topic:", topic.name);
  // navigate to topic view or open modal
};

const handleTopicAuthors = (topic) => {
  console.log("Authors for topic:", topic.name);
  // show authors list or navigate to authors page
};

const handleTopicDelete = (topic) => {
  console.log("Delete topic:", topic.name);
  // show a confirmation dialog and then delete
  if (confirm(`Are you sure you want to delete "${topic.name}"?`)) {
    const index = topics.value.findIndex((t) => t.id === topic.id);
    if (index > -1) {
      topics.value.splice(index, 1);
    }
  }
};

const handleCreateTournament = () => {
  showCreateTournamentDialog.value = true;
};

const handleTournamentCreated = async (newTournament) => {
  if (!authStore.isAuthenticated) {
    return;
  }

  // Step 1: OPTIMISTIC UPDATE - immediately add to UI for instant feedback
  tournaments.value.unshift(newTournament);

  toast.add({
    severity: "success",
    summary: "Tournament Created",
    detail: `"${newTournament.title}" has been created successfully!`,
    life: 3000,
  });

  setTimeout(() => {
    showCreateTournamentDialog.value = false;
  }, 500);

  // Step 2: BACKGROUND VALIDATION - fetch from backend to ensure data consistency
  try {
    const result = await tournamentService.getAllTournaments();
    if (result.success) {
      tournaments.value = result.data;
    }
  } catch (error) {
    console.error("Failed to sync tournaments after creation:", error);
    toast.add({
      severity: "warn",
      summary: "Sync Warning",
      detail: "Tournament created but failed to sync with server. Please refresh the page.",
      life: 5000,
    });
  }
};

const handleCreateTopic = () => {
  console.log("Create new topic");
  // open topic creation modal
};
</script>

<template>
  <Toast />
  <CreateTournamentDialog v-model:visible="showCreateTournamentDialog" @tournament-created="handleTournamentCreated" />
  <TournamentParticipantsDialog v-model:visible="showParticipantsDialog" :tournament="selectedTournament" />

  <main class="container mx-auto px-4 py-8">
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
      <!-- Tournaments Section -->
      <div>
        <div class="mb-6">
          <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">Tournaments</h1>
          <CreateNewButton v-if="authStore.isAuthenticated" entityType="Tournament" @create="handleCreateTournament" />
        </div>

        <!-- Not authenticated message -->
        <div v-if="!authStore.isAuthenticated" class="text-center py-12">
          <div class="text-gray-500 dark:text-gray-400">
            <i class="pi pi-lock text-4xl mb-4 block"></i>
            <p class="text-lg">Please log in to view tournaments</p>
          </div>
        </div>

        <!-- Authenticated content -->
        <template v-else>
          <div v-if="isLoadingTournaments" class="text-center py-12">
            <div class="text-gray-500 dark:text-gray-400">
              <i class="pi pi-spin pi-spinner text-4xl mb-4 block"></i>
              <p class="text-lg">Loading tournaments...</p>
            </div>
          </div>

          <div v-else class="space-y-4">
            <TournamentCard
              v-for="tournament in tournaments"
              :key="tournament.id"
              :tournament="tournament"
              @settings="handleTournamentSettings"
              @participants="handleTournamentParticipants"
              @start="handleTournamentStart"
              @delete="handleTournamentDelete"
              class="w-full"
            />
          </div>

          <div v-if="!isLoadingTournaments && tournaments.length === 0" class="text-center py-12">
            <div class="text-gray-500 dark:text-gray-400">
              <i class="pi pi-trophy text-4xl mb-4 block"></i>
              <p class="text-lg">No tournaments available</p>
              <p class="text-sm">Check back later for upcoming tournaments</p>
            </div>
          </div>
        </template>
      </div>

      <!-- Topics Section -->
      <div>
        <div class="mb-6">
          <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">Topics</h1>
          <CreateNewButton entityType="Topic" @create="handleCreateTopic" />
        </div>

        <div class="space-y-4">
          <TopicCard
            v-for="topic in topics"
            :key="topic.id"
            :topic="topic"
            @view="handleTopicView"
            @authors="handleTopicAuthors"
            @delete="handleTopicDelete"
            class="w-full"
          />
        </div>

        <div v-if="topics.length === 0" class="text-center py-12">
          <div class="text-gray-500 dark:text-gray-400">
            <i class="pi pi-book text-4xl mb-4 block"></i>
            <p class="text-lg">No topics available</p>
            <p class="text-sm">Check back later for new topics</p>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>
