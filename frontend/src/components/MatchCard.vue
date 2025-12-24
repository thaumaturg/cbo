<script setup>
import Button from "primevue/button";
import Card from "primevue/card";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import { computed } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

const props = defineProps({
  match: {
    type: Object,
    required: true,
    default: () => ({
      id: null,
      numberInTournament: null,
      numberInStage: null,
      createdOnStage: null,
      type: null,
      tournamentId: null,
      matchParticipants: [],
      roundsCount: 0,
    }),
  },
  tournamentId: {
    type: Number,
    required: true,
  },
});

const matchTitle = computed(() => {
  const stage = props.match.createdOnStage || "Match";
  const number = props.match.numberInStage || props.match.numberInTournament;
  return `${stage} #${number}`;
});

const roundsCount = computed(() => props.match.roundsCount ?? 0);

const handleRounds = () => {
  toast.add({
    severity: "info",
    summary: "Coming Soon",
    detail: "Rounds management feature will be available soon.",
    life: 3000,
  });
};
</script>

<template>
  <Card class="w-full shadow-md hover:shadow-lg transition-shadow duration-200">
    <template #content>
      <div class="p-4">
        <div class="mb-4">
          <div class="flex items-center justify-between gap-2 mb-2">
            <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100">
              {{ matchTitle }}
            </h3>
            <span class="text-sm text-gray-500 dark:text-gray-400">
              <i class="pi pi-list mr-1"></i>
              {{ roundsCount }} round{{ roundsCount !== 1 ? "s" : "" }}
            </span>
          </div>
        </div>

        <DataTable
          :value="match.matchParticipants"
          dataKey="id"
          class="match-participants-table"
          responsiveLayout="scroll"
          stripedRows
          size="small"
        >
          <Column field="username" header="Player" style="min-width: 120px">
            <template #body="{ data }">
              <span class="font-medium text-gray-800 dark:text-gray-200">{{ data.username }}</span>
            </template>
          </Column>

          <Column field="pointsSum" header="Points" style="width: 80px" class="text-center">
            <template #body="{ data }">
              <span class="text-gray-600 dark:text-gray-400">
                {{ data.pointsSum ?? "-" }}
              </span>
            </template>
          </Column>

          <Column field="scoreSum" header="Score" style="width: 80px" class="text-center">
            <template #body="{ data }">
              <span class="text-gray-600 dark:text-gray-400">
                {{ data.scoreSum ?? "-" }}
              </span>
            </template>
          </Column>
        </DataTable>

        <div class="mt-4 flex justify-start">
          <Button label="Rounds" icon="pi pi-list" severity="info" size="small" outlined @click="handleRounds" />
        </div>
      </div>
    </template>
  </Card>
</template>

<style scoped>
.match-participants-table :deep(.p-datatable-thead > tr > th) {
  background-color: var(--surface-ground);
  padding: 0.5rem;
  font-size: 0.875rem;
}

.match-participants-table :deep(.p-datatable-tbody > tr > td) {
  padding: 0.5rem;
  font-size: 0.875rem;
}
</style>
