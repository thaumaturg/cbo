<script setup>
import { FIRST_PAGE, PAGE_SIZES } from "@/composables/use-list-query.js";
import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import Tag from "primevue/tag";
import { computed } from "vue";
import { RouterLink, useRouter } from "vue-router";

const props = defineProps({
  // Topic rows, or empty-slot rows ({ key, isEmptySlot: true }) in reorder mode
  topics: {
    type: Array,
    required: true,
  },
  loading: {
    type: Boolean,
    default: false,
  },
  reorderable: {
    type: Boolean,
    default: false,
  },
  removable: {
    type: Boolean,
    default: false,
  },
  showPosition: {
    type: Boolean,
    default: false,
  },
  dataKey: {
    type: String,
    default: "id",
  },
  disabled: {
    type: Boolean,
    default: false,
  },
  emptyMessage: {
    type: String,
    default: "No topics yet",
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

const emit = defineEmits(["reorder", "remove", "page", "sort", "clear-filters"]);

const router = useRouter();

const REORDER_CELL_CLASS = "topics-table-reorder-cell";

// Empty slots aren't stored, so the ones after the last topic are lost on reload
const lastTopicIndex = computed(() => props.topics.findLastIndex((row) => !row.isEmptySlot));
const isUnsavedEmptySlot = (index) => index > lastTopicIndex.value;

const topicRoute = (topic) => ({ name: "topic-edit", params: { id: topic.topicId ?? topic.id } });

const openTopic = ({ data, originalEvent }) => {
  if (data.isEmptySlot) return;
  // Clicking the drag handle (or its cell) mustn't open the topic
  if (originalEvent.target.closest?.(`.${REORDER_CELL_CLASS}`)) return;
  router.push(topicRoute(data));
};

const handleRowReorder = (event) => {
  if (props.disabled) return;
  emit("reorder", event.value);
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

// PrimeVue pages are 0-based; the API's start at FIRST_PAGE
const handlePage = (event) => {
  emit("page", { page: event.page + FIRST_PAGE, pageSize: event.rows });
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
    :value="topics"
    :loading="loading || disabled"
    :dataKey="dataKey"
    rowHover
    :rowClass="(data) => (data.isEmptySlot ? '' : 'cursor-pointer')"
    :lazy="isPaged"
    :paginator="isPaged"
    :rows="paging?.pageSize"
    :first="isPaged ? (paging.page - FIRST_PAGE) * paging.pageSize : undefined"
    :totalRecords="paging?.totalCount"
    :rowsPerPageOptions="isPaged ? PAGE_SIZES : undefined"
    :removableSort="isPaged"
    :sortField="sortField"
    :sortOrder="sortOrder"
    @row-click="openTopic"
    @row-reorder="handleRowReorder"
    @page="handlePage"
    @sort="handleSort"
  >
    <Column v-if="reorderable" rowReorder style="width: 3rem" :bodyClass="REORDER_CELL_CLASS" />
    <Column v-if="showPosition" header="#" style="width: 3rem">
      <template #body="{ index }">{{ index + 1 }}</template>
    </Column>
    <Column header="Title" :sortable="isPaged" sortField="title">
      <template #body="{ data, index }">
        <div v-if="data.isEmptySlot" class="flex flex-wrap items-center gap-2">
          <Tag value="Empty slot" severity="secondary" />
          <span v-if="isUnsavedEmptySlot(index)" class="text-sm text-gray-500 dark:text-gray-400">
            Saved once a topic follows
          </span>
        </div>
        <RouterLink v-else :to="topicRoute(data)" class="font-semibold hover:underline" @click.stop>
          {{ data.title }}
        </RouterLink>
      </template>
    </Column>
    <Column header="Questions" :sortable="isPaged" sortField="questionCount">
      <template #body="{ data }">{{ data.questionCount }}</template>
    </Column>
    <Column header="Authors">
      <template #body="{ data }">{{ data.authorCount }}</template>
    </Column>
    <Column header="I'm an author">
      <template #body="{ data }">
        <i v-if="data.isAuthor" class="pi pi-check" role="img" aria-label="Yes"></i>
      </template>
    </Column>
    <Column header="Played">
      <template #body="{ data }">
        <Tag v-if="data.isPlayed" value="Played" severity="success" />
      </template>
    </Column>
    <Column v-if="isPaged" header="Created" sortable sortField="createdAt">
      <template #body="{ data }">
        {{ new Date(data.createdAt).toLocaleDateString() }}
      </template>
    </Column>
    <Column v-if="removable" style="width: 4rem" class="text-center">
      <template #body="{ index }">
        <Button
          icon="pi pi-times"
          severity="danger"
          text
          rounded
          aria-label="Remove"
          :disabled="disabled"
          @click.stop="emit('remove', index)"
        />
      </template>
    </Column>
    <template #empty>
      <template v-if="isListEmpty">
        <div v-if="filtered" class="flex items-center gap-3">
          <span>No topics match these filters</span>
          <Button label="Clear filters" size="small" severity="secondary" text @click="emit('clear-filters')" />
        </div>
        <template v-else>{{ emptyMessage }}</template>
      </template>
    </template>
  </DataTable>
</template>
