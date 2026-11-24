<script setup>
import TopicsTable from "@/components/TopicsTable.vue";
import { useListPage } from "@/composables/use-list-page.js";
import {
  DEFAULT_PAGE_SIZE,
  FIRST_PAGE,
  MAX_SEARCH_LENGTH,
  parseBoolean,
  parsePage,
  parsePageSize,
  parseSort,
  parseText,
  useListQuery,
} from "@/composables/use-list-query.js";
import { topicService } from "@/services/topic-service.js";
import Button from "primevue/button";
import InputText from "primevue/inputtext";
import Select from "primevue/select";
import { RouterLink } from "vue-router";

const SORT_KEYS = ["title", "questionCount", "createdAt"];

const defaults = {
  page: FIRST_PAGE,
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

const listQuery = useListQuery({
  parse,
  defaults,
  filterKeys: ["search", "played", "isAuthor"],
});
const { params, update, isFiltered } = listQuery;
const {
  items: topics,
  isLoading,
  paging,
  searchText,
  handleSearchInput,
  handlePage,
  handleSort,
  clearFilters,
} = useListPage({ listQuery, defaultSort: defaults.sort, load: topicService.getTopics });

const handlePlayedChange = (played) => {
  update({ played: played ?? undefined }, { resetPage: true });
};

const handleIsAuthorChange = (isAuthor) => {
  update({ isAuthor: isAuthor ?? undefined }, { resetPage: true });
};
</script>

<template>
  <main class="container mx-auto px-4 py-8">
    <div class="mb-6 flex items-center gap-4">
      <h1 class="text-3xl">Topics</h1>
      <RouterLink :to="{ name: 'topic-new' }" custom v-slot="{ href, navigate }">
        <Button as="a" :href="href" icon="pi pi-plus" label="New" severity="secondary" outlined @click="navigate" />
      </RouterLink>
      <RouterLink :to="{ name: 'topic-import' }" custom v-slot="{ href, navigate }">
        <Button
          as="a"
          :href="href"
          icon="pi pi-file-import"
          label="Import"
          severity="secondary"
          outlined
          @click="navigate"
        />
      </RouterLink>
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
