<script setup>
import { roleTagSeverity, stageTagSeverity } from "@/utils/tournament.js";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import Tag from "primevue/tag";
import { RouterLink, useRouter } from "vue-router";

defineProps({
  tournaments: {
    type: Array,
    required: true,
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const router = useRouter();

const tournamentRoute = (tournament) => ({ name: "tournament-view", params: { tournamentId: tournament.id } });

const openTournament = ({ data }) => {
  router.push(tournamentRoute(data));
};
</script>

<template>
  <DataTable
    :value="tournaments"
    :loading="loading"
    dataKey="id"
    rowHover
    :rowClass="() => 'cursor-pointer'"
    @row-click="openTournament"
  >
    <Column header="Title">
      <template #body="{ data }">
        <RouterLink :to="tournamentRoute(data)" class="font-semibold hover:underline" @click.stop>
          {{ data.title }}
        </RouterLink>
      </template>
    </Column>
    <Column header="Stage">
      <template #body="{ data }">
        <Tag :value="data.currentStage" :severity="stageTagSeverity(data.currentStage)" />
      </template>
    </Column>
    <Column header="My role">
      <template #body="{ data }">
        <Tag :value="data.currentUserRole" :severity="roleTagSeverity(data.currentUserRole)" />
      </template>
    </Column>
    <Column header="Created">
      <template #body="{ data }">
        {{ new Date(data.createdAt).toLocaleDateString() }}
      </template>
    </Column>
    <template #empty>No tournaments yet</template>
  </DataTable>
</template>
