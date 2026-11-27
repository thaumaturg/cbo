<script setup>
import TournamentDialog from "@/components/TournamentDialog.vue";
import TournamentsTable from "@/components/TournamentsTable.vue";
import { useListPage } from "@/composables/use-list-page.js";
import {
  DEFAULT_PAGE_SIZE,
  FIRST_PAGE,
  MAX_SEARCH_LENGTH,
  parseManyOf,
  parseOneOf,
  parsePage,
  parsePageSize,
  parseSort,
  parseText,
  useListQuery,
} from "@/composables/use-list-query.js";
import { tournamentService } from "@/services/tournament-service.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import InputText from "primevue/inputtext";
import MultiSelect from "primevue/multiselect";
import Select from "primevue/select";
import { ref } from "vue";
import { useRouter } from "vue-router";

const SORT_KEYS = ["title", "stage", "createdAt"];
const STAGES = ["Preparations", "Qualifications", "Playoff", "Ended"];
const ROLES = ["Creator", "Organizer", "Player"];

const defaults = {
  page: FIRST_PAGE,
  pageSize: DEFAULT_PAGE_SIZE,
  sort: "-createdAt",
  search: undefined,
  stage: undefined,
  role: [],
};

const parse = (query) => ({
  page: parsePage(query.page),
  pageSize: parsePageSize(query.pageSize),
  sort: parseSort(query.sort, SORT_KEYS, defaults.sort),
  search: parseText(query.search),
  stage: parseOneOf(query.stage, STAGES),
  role: parseManyOf(query.role, ROLES),
});

const stageOptions = STAGES.map((stage) => ({ label: stage, value: stage }));
const roleOptions = ROLES.map((role) => ({ label: role, value: role }));

const router = useRouter();
const notify = useNotify();
const listQuery = useListQuery({
  parse,
  defaults,
  filterKeys: ["search", "stage", "role"],
});
const { params, update, isFiltered } = listQuery;
const {
  items: tournaments,
  isLoading,
  paging,
  searchText,
  handleSearchInput,
  handlePage,
  handleSort,
  clearFilters,
} = useListPage({ listQuery, defaultSort: defaults.sort, load: tournamentService.getTournaments });

const showCreateDialog = ref(false);

const handleStageChange = (stage) => {
  update({ stage: stage ?? undefined }, { resetPage: true });
};

const handleRoleChange = (role) => {
  update({ role }, { resetPage: true });
};

const handleTournamentCreated = (tournament) => {
  notify.success("Tournament created", `"${tournament.title}" was created.`);
  showCreateDialog.value = false;
  router.push({ name: "tournament-view", params: { tournamentId: tournament.id } });
};
</script>

<template>
  <TournamentDialog v-model:visible="showCreateDialog" @tournament-created="handleTournamentCreated" />

  <main class="container mx-auto px-4 py-8">
    <div class="mb-6 flex items-center gap-4">
      <h1 class="text-3xl">Tournaments</h1>
      <Button icon="pi pi-plus" label="New" severity="secondary" outlined @click="showCreateDialog = true" />
    </div>

    <div class="mb-4 flex flex-wrap items-center gap-3">
      <InputText
        :modelValue="searchText"
        placeholder="Search by title"
        aria-label="Search by title"
        :maxlength="MAX_SEARCH_LENGTH"
        class="w-64"
        @update:modelValue="handleSearchInput"
      />
      <Select
        :modelValue="params.stage"
        :options="stageOptions"
        optionLabel="label"
        optionValue="value"
        placeholder="All stages"
        aria-label="Stage"
        showClear
        class="w-48"
        @update:modelValue="handleStageChange"
      />
      <MultiSelect
        :modelValue="params.role"
        :options="roleOptions"
        optionLabel="label"
        optionValue="value"
        placeholder="All my roles"
        aria-label="My role"
        class="w-56"
        @update:modelValue="handleRoleChange"
      />
    </div>

    <TournamentsTable
      :tournaments="tournaments"
      :loading="isLoading"
      :paging="paging"
      :filtered="isFiltered"
      @page="handlePage"
      @sort="handleSort"
      @clear-filters="clearFilters"
    />
  </main>
</template>
