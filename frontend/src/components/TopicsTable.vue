<script setup>
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
});

const emit = defineEmits(["reorder", "remove"]);

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
</script>

<template>
  <DataTable
    :value="topics"
    :loading="loading || disabled"
    :dataKey="dataKey"
    rowHover
    :rowClass="(data) => (data.isEmptySlot ? '' : 'cursor-pointer')"
    @row-click="openTopic"
    @row-reorder="handleRowReorder"
  >
    <Column v-if="reorderable" rowReorder style="width: 3rem" :bodyClass="REORDER_CELL_CLASS" />
    <Column v-if="showPosition" header="#" style="width: 3rem">
      <template #body="{ index }">{{ index + 1 }}</template>
    </Column>
    <Column header="Title">
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
    <Column header="Questions">
      <template #body="{ data }">{{ data.questions?.length }}</template>
    </Column>
    <Column header="Authors">
      <template #body="{ data }">{{ data.authors?.length }}</template>
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
    <template #empty>{{ emptyMessage }}</template>
  </DataTable>
</template>
