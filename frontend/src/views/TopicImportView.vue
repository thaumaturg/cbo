<script setup>
import { LIMITS } from "@/domain/limits.js";
import { topicService } from "@/services/topic-service.js";
import { parseClipboardTable } from "@/utils/clipboard-parser.js";
import { useNotify } from "@/utils/notify.js";
import { parseTopicBlocks } from "@/utils/topic-import-parser.js";
import { useUnsavedChangesGuard } from "@/utils/unsaved-changes.js";
import Button from "primevue/button";
import Checkbox from "primevue/checkbox";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import Message from "primevue/message";
import Tag from "primevue/tag";
import { computed, ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();
const notify = useNotify();

const LIMIT_MESSAGE = `At most ${LIMITS.topics.importMax} topics per import.`;
const TOO_MUCH_DATA_MESSAGE = "Too much data, import fewer topics.";

// The parsed paste: `{ topics, skippedCount }`, each topic with an `id` (its position in the sheet); null before a paste
const preview = ref(null);
const tickedIds = ref(new Set());
const isAuthor = ref(false);
const expandedRows = ref({});
const isImporting = ref(false);
// The server's messages for each topic row, by topic id
const serverErrors = ref({});
const formError = ref(null);

const topics = computed(() => preview.value?.topics ?? []);
const isValid = (topic) => topic.errors.length === 0;
const validTopics = computed(() => topics.value.filter(isValid));
const tickedCount = computed(() => tickedIds.value.size);
const selectableCount = computed(() => Math.min(validTopics.value.length, LIMITS.topics.importMax));
const isAtLimit = computed(() => tickedCount.value >= LIMITS.topics.importMax);
const showLimitMessage = computed(() => validTopics.value.length > LIMITS.topics.importMax);

const allTicked = computed(() => tickedCount.value > 0 && tickedCount.value === selectableCount.value);
const someTicked = computed(() => tickedCount.value > 0 && !allTicked.value);

// A preview that hasn't been imported yet is lost on leaving the page
const isDirty = computed(() => topics.value.length > 0);
useUnsavedChangesGuard(isDirty);

const plural = (count, word) => `${count} ${count === 1 ? word : `${word}s`}`;

const summary = computed(() => {
  if (!preview.value) return null;

  const { skippedCount } = preview.value;
  if (topics.value.length === 0 && skippedCount === 0) {
    return "Nothing to import was found in the pasted cells.";
  }

  const validCount = validTopics.value.length;
  const invalidCount = topics.value.length - validCount;
  return (
    `Found ${plural(topics.value.length, "topic")}: ${validCount} valid, ${invalidCount} with errors. ` +
    `Skipped ${plural(skippedCount, "empty topic")}.`
  );
});

// Ticks valid topics in sheet order, after the ones already ticked, until the limit
const tickUpToLimit = (ticked) => {
  for (const topic of validTopics.value) {
    if (ticked.size >= LIMITS.topics.importMax) break;
    ticked.add(topic.id);
  }
  return ticked;
};

const resetResult = () => {
  serverErrors.value = {};
  formError.value = null;
};

const clear = () => {
  preview.value = null;
  tickedIds.value = new Set();
  expandedRows.value = {};
  resetResult();
};

const handlePaste = (event) => {
  if (isImporting.value || !event.clipboardData) return;
  event.preventDefault();

  const rows = parseClipboardTable(event.clipboardData, { keepBlankRows: true });
  const { topics: parsedTopics, skippedCount } = parseTopicBlocks(rows);

  clear();
  preview.value = { topics: parsedTopics.map((topic, id) => ({ ...topic, id })), skippedCount };
  tickedIds.value = tickUpToLimit(new Set());
};

const isTicked = (topic) => tickedIds.value.has(topic.id);

// Invalid topics can't be ticked, and nor can more topics once the limit is reached
const canTick = (topic) => isValid(topic) && (isTicked(topic) || !isAtLimit.value);

const setTicked = (topic, ticked) => {
  const next = new Set(tickedIds.value);
  if (ticked && canTick(topic)) {
    next.add(topic.id);
  } else {
    next.delete(topic.id);
  }
  tickedIds.value = next;
};

const setAllTicked = (ticked) => {
  tickedIds.value = ticked ? tickUpToLimit(new Set(tickedIds.value)) : new Set();
};

const SERVER_TOPIC_FIELD = /^topics\[(\d+)\]\.(.+)$/;
const QUESTION_FIELD_PREFIX = /^questions\[(\d+)\]\./;

// Numbers questions as the expanded row's # column does
const toRowMessage = (field, message) => {
  const match = QUESTION_FIELD_PREFIX.exec(field);
  return match ? `Question ${Number(match[1]) + 1}: ${message}` : message;
};

// Places each `topics[i]…` error on the topic it came from (`i` indexes the submitted topics); the rest go above the list
const applyFailure = (result, submittedIds) => {
  if (result.status === 413) {
    formError.value = TOO_MUCH_DATA_MESSAGE;
    return;
  }

  const byTopic = {};
  const otherMessages = [];
  for (const [field, messages] of Object.entries(result.errors ?? {})) {
    const match = SERVER_TOPIC_FIELD.exec(field);
    const topicId = match ? submittedIds[Number(match[1])] : undefined;
    if (topicId === undefined) {
      otherMessages.push(...messages);
      continue;
    }
    byTopic[topicId] = [...(byTopic[topicId] ?? []), ...messages.map((message) => toRowMessage(match[2], message))];
  }

  serverErrors.value = byTopic;
  formError.value = [...new Set([result.error, ...otherMessages])].join(" ");
};

const importSelected = async () => {
  const ticked = topics.value.filter(isTicked);
  if (ticked.length === 0) return;

  isImporting.value = true;
  resetResult();

  try {
    const result = await topicService.importTopics({
      isAuthor: isAuthor.value,
      topics: ticked.map(({ title, description, questions }) => ({ title, description, questions })),
    });

    if (result.success) {
      // Imported, so there's nothing left to lose on leaving
      clear();
      notify.success("Topics imported", `Imported ${plural(result.data.count, "topic")}.`);
      router.push({ name: "topics" });
    } else {
      applyFailure(
        result,
        ticked.map((topic) => topic.id),
      );
    }
  } finally {
    isImporting.value = false;
  }
};
</script>

<template>
  <main class="container mx-auto px-4 py-8 max-w-8/10">
    <h1 class="text-3xl mb-6">Import Topics</h1>

    <div class="flex flex-col gap-2 mb-6">
      <div
        tabindex="0"
        role="region"
        aria-label="Paste topics from Google Sheets"
        aria-describedby="paste-hint"
        class="rounded-xl border-2 border-dashed border-gray-300 dark:border-gray-600 p-8 text-center cursor-text text-gray-600 dark:text-gray-300 focus:outline-none focus:border-blue-500 focus:bg-blue-50 dark:focus:bg-blue-900/20"
        @paste="handlePaste"
      >
        <i class="pi pi-clipboard text-3xl mb-2 block text-gray-400"></i>
        Click here and press Ctrl+V to paste topics from Google Sheets
      </div>
      <p id="paste-hint" class="text-sm text-gray-500 dark:text-gray-400">
        Copy whole topics: a title row, a header row (column A can hold the topic's description), then one row per
        question with Cost, Question, Answer and Comment in columns A–D, and a blank row between topics.
      </p>
    </div>

    <template v-if="preview">
      <Message :severity="topics.length > 0 ? 'info' : 'warn'" class="mb-4">{{ summary }}</Message>
      <Message v-if="showLimitMessage" severity="warn" class="mb-4">{{ LIMIT_MESSAGE }}</Message>
      <Message v-if="formError" severity="error" class="mb-4">{{ formError }}</Message>

      <DataTable
        v-if="topics.length > 0"
        v-model:expandedRows="expandedRows"
        :value="topics"
        dataKey="id"
        class="mb-6"
        stripedRows
      >
        <Column style="width: 3rem">
          <template #header>
            <Checkbox
              :modelValue="allTicked"
              :indeterminate="someTicked"
              binary
              aria-label="Select all valid topics"
              :disabled="validTopics.length === 0 || isImporting"
              @update:modelValue="setAllTicked"
            />
          </template>
          <template #body="{ data }">
            <Checkbox
              :modelValue="isTicked(data)"
              binary
              :aria-label="`Import ${data.title || 'this topic'}`"
              :disabled="!canTick(data) || isImporting"
              @update:modelValue="(ticked) => setTicked(data, ticked)"
            />
          </template>
        </Column>
        <Column expander style="width: 3rem" />
        <Column header="Title">
          <template #body="{ data }">
            <span v-if="data.title">{{ data.title }}</span>
            <span v-else class="text-gray-400 italic">No title</span>
          </template>
        </Column>
        <Column header="Questions" style="width: 7rem">
          <template #body="{ data }">{{ data.questions.length }}</template>
        </Column>
        <Column field="totalCost" header="Total cost" style="width: 7rem" />
        <Column header="Status">
          <template #body="{ data }">
            <div class="flex flex-col gap-1">
              <Tag v-if="isValid(data) && !serverErrors[data.id]" value="Valid" severity="success" class="self-start" />
              <Message
                v-for="(message, index) in [...data.errors, ...(serverErrors[data.id] ?? [])]"
                :key="index"
                severity="error"
                variant="simple"
                size="small"
              >
                {{ message }}
              </Message>
            </div>
          </template>
        </Column>

        <template #expansion="{ data }">
          <div class="flex flex-col gap-3 p-2">
            <p class="text-sm">
              <span class="font-semibold">Description:</span>
              <span v-if="data.description" class="whitespace-pre-line"> {{ data.description }}</span>
              <span v-else class="text-gray-400 italic"> none</span>
            </p>
            <DataTable :value="data.questions" dataKey="questionNumber" size="small">
              <Column field="questionNumber" header="#" style="width: 3rem" />
              <Column header="Cost" style="width: 5rem">
                <template #body="{ data: question }">{{ question.cost ?? "—" }}</template>
              </Column>
              <Column header="Question">
                <template #body="{ data: question }">
                  <span class="whitespace-pre-line">{{ question.text }}</span>
                </template>
              </Column>
              <Column header="Answer">
                <template #body="{ data: question }">
                  <span class="whitespace-pre-line">{{ question.answer }}</span>
                </template>
              </Column>
              <Column header="Comment">
                <template #body="{ data: question }">
                  <span class="whitespace-pre-line">{{ question.comment }}</span>
                </template>
              </Column>
            </DataTable>
          </div>
        </template>
      </DataTable>

      <div v-if="topics.length > 0" class="flex items-center gap-3 mb-6">
        <Checkbox v-model="isAuthor" inputId="isAuthor" binary :disabled="isImporting" />
        <label for="isAuthor" class="cursor-pointer text-gray-700 dark:text-gray-300">
          I'm an author of these topics
        </label>
      </div>

      <div class="flex items-center gap-4">
        <span v-if="topics.length > 0" class="text-sm text-gray-500 dark:text-gray-400">
          {{ tickedCount }} of {{ validTopics.length }} valid topics selected
        </span>
        <div class="ml-auto flex gap-4">
          <Button
            type="button"
            label="Clear"
            icon="pi pi-times"
            severity="secondary"
            outlined
            :disabled="isImporting"
            @click="clear"
          />
          <Button
            v-if="topics.length > 0"
            type="button"
            label="Import selected"
            icon="pi pi-check"
            :disabled="tickedCount === 0 || isImporting"
            :loading="isImporting"
            @click="importSelected"
          />
        </div>
      </div>
    </template>
  </main>
</template>
