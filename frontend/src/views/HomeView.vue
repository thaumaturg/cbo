<script setup>
import TournamentCard from "@/components/TournamentCard.vue";
import TopicCard from "@/components/TopicCard.vue";
import CreateNewButton from "@/components/CreateNewButton.vue";
import TournamentDialog from "@/components/TournamentDialog.vue";
import TournamentParticipantsDialog from "@/components/TournamentParticipantsDialog.vue";
import Toast from "primevue/toast";
import { tournamentService } from "@/services/tournament-service.js";
import { topicService } from "@/services/topic-service.js";
import { ref, onMounted, watch } from "vue";
import { useToast } from "primevue/usetoast";
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/auth.js";

const toast = useToast();
const router = useRouter();
const authStore = useAuthStore();

const showTournamentDialog = ref(false);
const showParticipantsDialog = ref(false);
const selectedTournament = ref(null);
const tournaments = ref([]);
const isLoadingTournaments = ref(false);
const tournamentDialogMode = ref("create");
const tournamentToEdit = ref(null);

const topics = ref([]);
const isLoadingTopics = ref(false);

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

const fetchTopics = async () => {
  if (!authStore.isAuthenticated) {
    return;
  }

  isLoadingTopics.value = true;
  try {
    const result = await topicService.getAllTopics();
    if (result.success) {
      topics.value = result.data;
    } else {
      console.error("Failed to fetch topics:", result.error);
    }
  } catch (error) {
    console.error("Error fetching topics:", error);
  } finally {
    isLoadingTopics.value = false;
  }
};

onMounted(() => {
  fetchTournaments();
  fetchTopics();
});

watch(
  () => authStore.isAuthenticated,
  (isAuthenticated) => {
    if (isAuthenticated) {
      fetchTournaments();
      fetchTopics();
    } else {
      tournaments.value = [];
      topics.value = [];
    }
  }
);

const handleTournamentSettings = (tournament) => {
  tournamentToEdit.value = tournament;
  tournamentDialogMode.value = "edit";
  showTournamentDialog.value = true;
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
  router.push(`/topics/${topic.id}`);
};

const handleTopicAuthors = (topic) => {
  console.log("Authors for topic:", topic.title);
  // TODO: implement authors management modal
};

const handleTopicDelete = async (topic) => {
  if (!authStore.isAuthenticated) {
    return;
  }

  const topicTitle = topic.title;
  if (confirm(`Are you sure you want to delete "${topicTitle}"?`)) {
    // Optimistic update - remove from UI immediately
    const index = topics.value.findIndex((t) => t.id === topic.id);
    if (index > -1) {
      topics.value.splice(index, 1);
    }

    const result = await topicService.deleteTopic(topic.id);

    if (!result.success) {
      console.error("Failed to delete topic:", result.error);
      toast.add({
        severity: "error",
        summary: "Delete Failed",
        detail: `Failed to delete "${topicTitle}". ${result.error}`,
        life: 5000,
      });
      // Re-fetch to restore the topic if deletion failed
      await fetchTopics();
    } else {
      toast.add({
        severity: "success",
        summary: "Topic Deleted",
        detail: `"${topicTitle}" has been deleted successfully.`,
        life: 3000,
      });
      // Background validation - ensure UI is in sync
      await fetchTopics();
    }
  }
};

const handleCreateTournament = () => {
  tournamentToEdit.value = null;
  tournamentDialogMode.value = "create";
  showTournamentDialog.value = true;
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
    showTournamentDialog.value = false;
  }, 1000);

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

const handleTournamentUpdated = async (updatedTournament) => {
  if (!authStore.isAuthenticated) {
    return;
  }

  // Step 1: OPTIMISTIC UPDATE - immediately update in UI for instant feedback
  const index = tournaments.value.findIndex((t) => t.id === updatedTournament.id);
  if (index > -1) {
    tournaments.value[index] = updatedTournament;
  }

  toast.add({
    severity: "success",
    summary: "Tournament Updated",
    detail: `"${updatedTournament.title}" has been updated successfully!`,
    life: 3000,
  });

  setTimeout(() => {
    showTournamentDialog.value = false;
  }, 1000);

  // Step 2: BACKGROUND VALIDATION - fetch from backend to ensure data consistency
  try {
    const result = await tournamentService.getAllTournaments();
    if (result.success) {
      tournaments.value = result.data;
    }
  } catch (error) {
    console.error("Failed to sync tournaments after update:", error);
    toast.add({
      severity: "warn",
      summary: "Sync Warning",
      detail: "Tournament updated but failed to sync with server. Please refresh the page.",
      life: 5000,
    });
  }
};

const handleCreateTopic = () => {
  router.push("/topics/new");
};
</script>

<template>
  <Toast />
  <TournamentDialog
    v-model:visible="showTournamentDialog"
    :mode="tournamentDialogMode"
    :tournament="tournamentToEdit"
    @tournament-created="handleTournamentCreated"
    @tournament-updated="handleTournamentUpdated"
  />
  <TournamentParticipantsDialog v-model:visible="showParticipantsDialog" :tournament="selectedTournament" />

  <main class="container mx-auto px-4 py-8">
    <!-- Not authenticated message -->
    <div v-if="!authStore.isAuthenticated" class="text-center py-16">
      <div class="text-gray-500 dark:text-gray-400">
        <i class="pi pi-lock text-5xl mb-4 block"></i>
        <p class="text-xl mb-2">Please log in to continue</p>
        <p class="text-sm">Sign in to view and manage your tournaments and topics</p>
      </div>
    </div>

    <!-- Authenticated content -->
    <div v-else class="grid grid-cols-1 lg:grid-cols-2 gap-8">
      <!-- Tournaments Section -->
      <div>
        <div class="mb-6 flex items-center justify-center gap-4">
          <h1 class="text-3xl">Tournaments</h1>
          <CreateNewButton @create="handleCreateTournament" />
        </div>

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
          </div>
        </div>
      </div>

      <!-- Topics Section -->
      <div>
        <div class="mb-6 flex items-center justify-center gap-4">
          <h1 class="text-3xl">Topics</h1>
          <CreateNewButton @create="handleCreateTopic" />
        </div>

        <div v-if="isLoadingTopics" class="text-center py-12">
          <div class="text-gray-500 dark:text-gray-400">
            <i class="pi pi-spin pi-spinner text-4xl mb-4 block"></i>
            <p class="text-lg">Loading topics...</p>
          </div>
        </div>

        <div v-else class="space-y-4">
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

        <div v-if="!isLoadingTopics && topics.length === 0" class="text-center py-12">
          <div class="text-gray-500 dark:text-gray-400">
            <i class="pi pi-book text-4xl mb-4 block"></i>
            <p class="text-lg">No topics available</p>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>
