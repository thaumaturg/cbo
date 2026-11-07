<script setup>
import TournamentDialog from "@/components/TournamentDialog.vue";
import TournamentsTable from "@/components/TournamentsTable.vue";
import { tournamentService } from "@/services/tournament-service.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();
const notify = useNotify();

const tournaments = ref([]);
const isLoading = ref(false);
const showCreateDialog = ref(false);

const fetchTournaments = async () => {
  isLoading.value = true;
  try {
    const result = await tournamentService.getAllTournaments();
    if (result.success) {
      tournaments.value = result.data;
    } else {
      notify.error("Load Failed", result.message);
    }
  } finally {
    isLoading.value = false;
  }
};

const handleTournamentCreated = (tournament) => {
  notify.success("Tournament created", `"${tournament.title}" was created.`);
  showCreateDialog.value = false;
  router.push({ name: "tournament-view", params: { tournamentId: tournament.id } });
};

onMounted(fetchTournaments);
</script>

<template>
  <TournamentDialog v-model:visible="showCreateDialog" mode="create" @tournament-created="handleTournamentCreated" />

  <main class="container mx-auto px-4 py-8">
    <div class="mb-6 flex items-center gap-4">
      <h1 class="text-3xl">Tournaments</h1>
      <Button icon="pi pi-plus" label="New" severity="secondary" outlined @click="showCreateDialog = true" />
    </div>

    <TournamentsTable :tournaments="tournaments" :loading="isLoading" />
  </main>
</template>
