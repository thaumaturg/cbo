<script setup>
import Card from "primevue/card";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import { ref } from "vue";

defineProps({
  participants: {
    type: Array,
    required: true,
    default: () => [],
  },
});

const multiSortMeta = ref([
  { field: "pointsSum", order: -1 },
  { field: "scoreSum", order: -1 },
]);
</script>

<template>
  <Card class="w-full shadow-md mb-6">
    <template #content>
      <div class="p-4">
        <div class="mb-4">
          <div class="flex items-center justify-between gap-2">
            <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100">
              <i class="pi pi-chart-bar mr-2"></i>
              Standings
            </h3>
            <span class="text-xs text-gray-500 dark:text-gray-400"> Hold Ctrl/Cmd + click to multi-sort </span>
          </div>
        </div>

        <DataTable
          :value="participants"
          dataKey="id"
          responsiveLayout="scroll"
          stripedRows
          size="small"
          sortMode="multiple"
          :multiSortMeta="multiSortMeta"
          removableSort
        >
          <Column field="username" header="Player" sortable style="min-width: 150px">
            <template #body="{ data }">
              <span class="font-medium text-gray-800 dark:text-gray-200">{{ data.username }}</span>
            </template>
          </Column>

          <Column field="pointsSum" header="Points" sortable style="width: 100px" class="text-center">
            <template #body="{ data }">
              <span class="font-semibold text-gray-700 dark:text-gray-300">
                {{ data.pointsSum ?? 0 }}
              </span>
            </template>
          </Column>

          <Column field="scoreSum" header="Score" sortable style="width: 100px" class="text-center">
            <template #body="{ data }">
              <span class="text-gray-600 dark:text-gray-400">
                {{ data.scoreSum ?? 0 }}
              </span>
            </template>
          </Column>
        </DataTable>
      </div>
    </template>
  </Card>
</template>

<style scoped></style>
