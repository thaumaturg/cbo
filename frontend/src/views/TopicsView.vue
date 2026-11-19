<script setup>
import TopicsTable from "@/components/TopicsTable.vue";
import {
  DEFAULT_PAGE_SIZE,
  parseBoolean,
  parsePage,
  parsePageSize,
  parseSort,
  parseText,
  useListQuery,
} from "@/composables/use-list-query.js";
import { topicService } from "@/services/topic-service.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import InputText from "primevue/inputtext";
import Select from "primevue/select";
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { RouterLink } from "vue-router";

const SORT_KEYS = ["title", "questionCount", "createdAt"];
const SEARCH_DELAY_MS = 300;

const defaults = {
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  sort: "-createdAt",
  search: undefined,
  played: undefined,
  isAuthor: undefined,
};

const parse = (query) => ({
  page: parsePage(query.page),
  pageSize: parsePageSize(query.pageSize),
  sort: parseSort(query.sort, SORT_KEYS, defaults.sort),
  search: parseText(query.search),
  played: parseBoolean(query.played),
  isAuthor: parseBoolean(query.isAuthor),
});

const playedOptions = [
  { label: "Played", value: true },
  { label: "Not played", value: false },
];
const isAuthorOptions = [
  { label: "I'm an author: yes", value: true },
  { label: "I'm an author: no", value: false },
];

const notify = useNotify();
const { params, update, isFiltered, reset } = useListQuery({
  parse,
  defaults,
  filterKeys: ["search", "played", "isAuthor"],
});

const topics = ref([]);
const totalCount = ref(0);
const isLoading = ref(false);

// The default order shows no sort indicator (see TopicsTable)
const paging = computed(() => ({
  page: params.value.page,
  pageSize: params.value.pageSize,
  totalCount: totalCount.value,
  sort: params.value.sort === defaults.sort ? undefined : params.value.sort,
}));

let requestController;

const fetchTopics = async (listParams) => {
  requestController?.abort();
  const controller = new AbortController();
  requestController = controller;

  isLoading.value = true;
  const result = await topicService.getTopics(listParams, { signal: controller.signal });
  // A newer request replaced this one, or the view unmounted: its result is stale, not an error
  if (controller.signal.aborted) return;

  isLoading.value = false;
  if (result.success) {
    topics.value = result.data.items;
    totalCount.value = result.data.totalCount;
  } else {
    notify.error("Load Failed", result.message);
  }
};

watch(params, fetchTopics, { immediate: true });

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

const handlePlayedChange = (played) => {
  update({ played: played ?? undefined }, { resetPage: true });
};

const handleIsAuthorChange = (isAuthor) => {
  update({ isAuthor: isAuthor ?? undefined }, { resetPage: true });
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
</script>

<template>
  <main class="container mx-auto px-4 py-8">
    <div class="mb-6 flex items-center gap-4">
      <h1 class="text-3xl">Topics</h1>
      <RouterLink :to="{ name: 'topic-new' }" custom v-slot="{ href, navigate }">
        <Button as="a" :href="href" icon="pi pi-plus" label="New" severity="secondary" outlined @click="navigate" />
      </RouterLink>
    </div>

    <div class="mb-4 flex flex-wrap items-center gap-3">
      <InputText
        :modelValue="searchText"
        placeholder="Search by title"
        aria-label="Search by title"
        class="w-64"
        @update:modelValue="handleSearchInput"
      />
      <Select
        :modelValue="params.played"
        :options="playedOptions"
        optionLabel="label"
        optionValue="value"
        placeholder="Played: all"
        aria-label="Played"
        showClear
        class="w-48"
        @update:modelValue="handlePlayedChange"
      />
      <Select
        :modelValue="params.isAuthor"
        :options="isAuthorOptions"
        optionLabel="label"
        optionValue="value"
        placeholder="I'm an author: all"
        aria-label="I'm an author"
        showClear
        class="w-56"
        @update:modelValue="handleIsAuthorChange"
      />
    </div>

    <TopicsTable
      :topics="topics"
      :loading="isLoading"
      :paging="paging"
      :filtered="isFiltered"
      @page="handlePage"
      @sort="handleSort"
      @clear-filters="clearFilters"
    />
  </main>
</template>
