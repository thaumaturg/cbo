<script setup>
import { PAGE_SIZES } from "@/composables/use-list-query.js";
import { roleTagSeverity, stageTagSeverity } from "@/utils/tournament.js";
import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import Tag from "primevue/tag";
import { computed } from "vue";
import { RouterLink, useRouter } from "vue-router";

const props = defineProps({
  tournaments: {
    type: Array,
    required: true,
  },
  loading: {
    type: Boolean,
    default: false,
  },
  // Server paging and sorting: { page, pageSize, totalCount, sort }. Without it the table shows every row it gets.
  // `sort` is a server sort key with an optional leading `-`; undefined means the list's default order,
  // which shows no sort indicator, so removing a column's sort and the default order look the same.
  paging: {
    type: Object,
    default: undefined,
  },
  // Whether filters hide some rows, which changes the empty message and offers to clear them
  filtered: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["page", "sort", "clear-filters"]);

const router = useRouter();

const tournamentRoute = (tournament) => ({ name: "tournament-view", params: { tournamentId: tournament.id } });

const openTournament = ({ data }) => {
  router.push(tournamentRoute(data));
};

const isPaged = computed(() => props.paging !== undefined);

// An empty page past the end of a non-empty list isn't an empty list: the view moves to the last page
const isListEmpty = computed(() => !isPaged.value || props.paging.totalCount === 0);

const sortField = computed(() => props.paging?.sort?.replace(/^-/, "") ?? null);
const sortOrder = computed(() => {
  const sort = props.paging?.sort;
  if (!sort) return null;
  return sort.startsWith("-") ? -1 : 1;
});

// PrimeVue pages are 0-based; the API's start at 1
const handlePage = (event) => {
  emit("page", { page: event.page + 1, pageSize: event.rows });
};

const handleSort = (event) => {
  if (!event.sortField) {
    emit("sort", { sort: undefined });
    return;
  }
  emit("sort", { sort: event.sortOrder === -1 ? `-${event.sortField}` : event.sortField });
};
</script>

<template>
  <DataTable
    :value="tournaments"
    :loading="loading"
    dataKey="id"
    rowHover
    :rowClass="() => 'cursor-pointer'"
    :lazy="isPaged"
    :paginator="isPaged"
    :rows="paging?.pageSize"
    :first="isPaged ? (paging.page - 1) * paging.pageSize : undefined"
    :totalRecords="paging?.totalCount"
    :rowsPerPageOptions="isPaged ? PAGE_SIZES : undefined"
    :removableSort="isPaged"
    :sortField="sortField"
    :sortOrder="sortOrder"
    @row-click="openTournament"
    @page="handlePage"
    @sort="handleSort"
  >
    <Column header="Title" :sortable="isPaged" sortField="title">
      <template #body="{ data }">
        <RouterLink :to="tournamentRoute(data)" class="font-semibold hover:underline" @click.stop>
          {{ data.title }}
        </RouterLink>
      </template>
    </Column>
    <Column header="Stage" :sortable="isPaged" sortField="stage">
      <template #body="{ data }">
        <Tag :value="data.currentStage" :severity="stageTagSeverity(data.currentStage)" />
      </template>
    </Column>
    <Column header="My role">
      <template #body="{ data }">
        <Tag :value="data.currentUserRole" :severity="roleTagSeverity(data.currentUserRole)" />
      </template>
    </Column>
    <Column header="Created" :sortable="isPaged" sortField="createdAt">
      <template #body="{ data }">
        {{ new Date(data.createdAt).toLocaleDateString() }}
      </template>
    </Column>
    <template #empty>
      <template v-if="isListEmpty">
        <div v-if="filtered" class="flex items-center gap-3">
          <span>No tournaments match these filters</span>
          <Button label="Clear filters" size="small" severity="secondary" text @click="emit('clear-filters')" />
        </div>
        <template v-else>No tournaments yet</template>
      </template>
    </template>
  </DataTable>
</template>
