<script setup>
import TournamentDialog from "@/components/TournamentDialog.vue";
import TournamentsTable from "@/components/TournamentsTable.vue";
import {
  DEFAULT_PAGE_SIZE,
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
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { useRouter } from "vue-router";

const SORT_KEYS = ["title", "stage", "createdAt"];
const STAGES = ["Preparations", "Started"];
const ROLES = ["Creator", "Organizer", "Player"];
const SEARCH_DELAY_MS = 300;

const defaults = {
  page: 1,
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
const { params, update, isFiltered, reset } = useListQuery({
  parse,
  defaults,
  filterKeys: ["search", "stage", "role"],
});

const tournaments = ref([]);
const totalCount = ref(0);
const isLoading = ref(false);
const showCreateDialog = ref(false);

// The default order shows no sort indicator (see TournamentsTable)
const paging = computed(() => ({
  page: params.value.page,
  pageSize: params.value.pageSize,
  totalCount: totalCount.value,
  sort: params.value.sort === defaults.sort ? undefined : params.value.sort,
}));

let requestController;

const fetchTournaments = async (listParams) => {
  requestController?.abort();
  const controller = new AbortController();
  requestController = controller;

  isLoading.value = true;
  const result = await tournamentService.getTournaments(listParams, { signal: controller.signal });
  // A newer request replaced this one, or the view unmounted: its result is stale, not an error
  if (controller.signal.aborted) return;

  isLoading.value = false;
  if (result.success) {
    tournaments.value = result.data.items;
    totalCount.value = result.data.totalCount;
    // A page past the end of a non-empty list goes to the last page. PrimeVue's paginator only does that when the
    // total changes. It can't loop: the last page isn't past the end.
    if (result.data.items.length === 0 && result.data.totalCount > 0) {
      update({ page: Math.ceil(result.data.totalCount / listParams.pageSize) }, { replace: true });
    }
  } else {
    notify.error("Load Failed", result.message);
  }
};

watch(params, fetchTournaments, { immediate: true });

// The search box has its own text, written to the URL once typing stops
const searchText = ref(params.value.search ?? "");
let searchTimer;

const cancelPendingSearch = () => {
  clearTimeout(searchTimer);
  searchTimer = undefined;
};

const handleSearchInput = (text) => {
  searchText.value = text ?? "";
  cancelPendingSearch();
  searchTimer = setTimeout(() => {
    searchTimer = undefined;
    const search = parseText(searchText.value);
    if (search !== params.value.search) {
      update({ search }, { resetPage: true, replace: true });
    }
  }, SEARCH_DELAY_MS);
};

// Back/Forward and Clear filters change the search in the URL; only sync the box when the user isn't typing
watch(
  () => params.value.search,
  (search) => {
    if (searchTimer === undefined && parseText(searchText.value) !== search) {
      searchText.value = search ?? "";
    }
  },
);

const handleStageChange = (stage) => {
  update({ stage: stage ?? undefined }, { resetPage: true });
};

const handleRoleChange = (role) => {
  update({ role }, { resetPage: true });
};

const handlePage = ({ page, pageSize }) => {
  // Once the total arrives, PrimeVue's paginator moves a page past the end to the last page.
  // That isn't a step the user took, so it replaces the URL instead of adding a history entry.
  const isPastEnd = (params.value.page - 1) * params.value.pageSize >= totalCount.value;
  // A new page size starts again from the first page
  update({ page, pageSize }, { resetPage: pageSize !== params.value.pageSize, replace: isPastEnd });
};

const handleSort = ({ sort }) => {
  update({ sort }, { resetPage: true });
};

const clearFilters = () => {
  cancelPendingSearch();
  searchText.value = "";
  reset();
};

onBeforeUnmount(() => {
  cancelPendingSearch();
  requestController?.abort();
});

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
