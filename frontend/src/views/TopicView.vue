<script setup>
import { eager } from "@/includes/validation.js";
import { topicService } from "@/services/topic-service.js";
import { useAuthStore } from "@/stores/auth.js";
import { toFormErrors, toFormFailure } from "@/utils/error.js";
import { focusFirstInvalid } from "@/utils/focus.js";
import { useNotify } from "@/utils/notify.js";
import { parseClipboardTable } from "@/utils/clipboard-parser.js";
import { useUnsavedChangesGuard } from "@/utils/unsaved-changes.js";
import Button from "primevue/button";
import Checkbox from "primevue/checkbox";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import InputNumber from "primevue/inputnumber";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import Tag from "primevue/tag";
import Textarea from "primevue/textarea";
import { useConfirm } from "primevue/useconfirm";
import { computed, ref, watch } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";

const router = useRouter();
const route = useRoute();
const notify = useNotify();
const confirm = useConfirm();
const authStore = useAuthStore();

const TOPIC_ROUTE_NAMES = ["topic-new", "topic-edit"];
const PLAYED_DELETE_HINT = "Played topics can't be deleted.";

const isEditMode = computed(() => route.params.id && route.params.id !== "new");
const topicId = computed(() => (isEditMode.value ? route.params.id : null));

// The topic as last loaded or saved; null on /topics/new
const topic = ref(null);

// A played topic can't be changed or deleted, so every control reads this one flag
const readOnly = computed(() => isEditMode.value && !!topic.value?.isPlayed);

const pageTitle = computed(() => {
  if (readOnly.value) return "View Topic";
  return isEditMode.value ? "Edit Topic" : "Create New Topic";
});

const createEmptyFormData = () => ({
  title: "",
  description: "",
  isAuthor: true,
});

const formData = ref(createEmptyFormData());

const formStatus = ref("idle"); // idle | loading | error
const generalError = ref(null);
const isLoading = ref(false);
const isDeleting = ref(false);
const questionErrors = ref({});
const formRef = ref(null);

const TOPIC_LIMITS = {
  questionsPerTopicMin: 1,
  questionsPerTopicMax: 10,
  questionsCostMin: -1000,
  questionsCostMax: 1000,
};

const DEFAULT_QUESTION_COUNT = 5;

const defaultCost = (questionNumber) => Math.min(questionNumber * 10, TOPIC_LIMITS.questionsCostMax);

const createEmptyQuestion = (questionNumber) => ({
  id: null,
  questionNumber,
  cost: defaultCost(questionNumber),
  text: "",
  answer: "",
  comment: "",
});

const createEmptyQuestions = () =>
  Array.from({ length: DEFAULT_QUESTION_COUNT }, (_, index) => createEmptyQuestion(index + 1));

const questions = ref(createEmptyQuestions());

// Co-authors only, as `{ username }`; the owner is never part of the request's list (ADR-0006)
const coAuthors = ref([]);
const newAuthorUsername = ref("");
const addAuthorError = ref(null);
const authorErrors = ref({}); // co-author index -> message

const ownerUsername = computed(
  () => topic.value?.authors.find((author) => author.isOwner)?.username ?? authStore.userName,
);

const authorRows = computed(() => [
  { username: ownerUsername.value, isOwner: true, isAuthor: formData.value.isAuthor, coAuthorIndex: null },
  ...coAuthors.value.map((coAuthor, index) => ({
    username: coAuthor.username,
    isOwner: false,
    isAuthor: true,
    coAuthorIndex: index,
  })),
]);

const sameUsername = (a, b) => a?.toLowerCase() === b?.toLowerCase();

// Only local checks; the server checks that the account exists on Save
const addCoAuthor = () => {
  const username = newAuthorUsername.value.trim();

  if (!username) {
    addAuthorError.value = "Username is required.";
  } else if (sameUsername(username, ownerUsername.value)) {
    addAuthorError.value = "You're the owner of this topic already.";
  } else if (coAuthors.value.some((coAuthor) => sameUsername(coAuthor.username, username))) {
    addAuthorError.value = "This user is already in the list.";
  } else {
    coAuthors.value.push({ username });
    newAuthorUsername.value = "";
    authorErrors.value = {};
  }
};

const removeCoAuthor = (index) => {
  coAuthors.value.splice(index, 1);
  authorErrors.value = {};
};

watch(newAuthorUsername, () => {
  addAuthorError.value = null;
});

const serializeState = () =>
  JSON.stringify({ formData: formData.value, questions: questions.value, coAuthors: coAuthors.value });

// The state as last loaded or saved, to compare the current state against
const savedState = ref(serializeState());
const isDirty = computed(() => serializeState() !== savedState.value);

const markClean = () => {
  savedState.value = serializeState();
};

useUnsavedChangesGuard(isDirty);

const canAddQuestion = computed(() => questions.value.length < TOPIC_LIMITS.questionsPerTopicMax);
const canRemoveQuestion = computed(() => questions.value.length > TOPIC_LIMITS.questionsPerTopicMin);

const renumberQuestions = () => {
  questions.value.forEach((question, index) => {
    question.questionNumber = index + 1;
  });
};

const addQuestion = () => {
  if (!canAddQuestion.value) return;
  questions.value.push(createEmptyQuestion(questions.value.length + 1));
  questionErrors.value = {};
};

const removeQuestion = (index) => {
  if (!canRemoveQuestion.value) return;
  questions.value.splice(index, 1);
  renumberQuestions();
  questionErrors.value = {};
};

const isFormProcessing = computed(() => formStatus.value === "loading");
const isBusy = computed(() => isFormProcessing.value || isDeleting.value);
// Locked while saving too, since a successful save repopulates the form from the response
const inputsDisabled = computed(() => readOnly.value || isBusy.value);

const validateCostField = (value, fieldName) => {
  const { questionsCostMin, questionsCostMax } = TOPIC_LIMITS;
  if (value === null || value === undefined || value === "") {
    return `${fieldName} is required.`;
  }
  if (value < questionsCostMin) {
    return `${fieldName} must be at least ${questionsCostMin}.`;
  }
  if (value > questionsCostMax) {
    return `${fieldName} must be at most ${questionsCostMax}.`;
  }
  return null;
};

const validateQuestion = (question) => {
  const errors = {};

  if (!question.text?.trim()) {
    errors.text = "Question is required.";
  }
  if (!question.answer?.trim()) {
    errors.answer = "Answer is required.";
  }

  const costError = validateCostField(question.cost, "Cost");
  if (costError) errors.cost = costError;

  return errors;
};

const validateAllQuestions = () => {
  const allErrors = {};
  let hasAnyError = false;

  questions.value.forEach((q, index) => {
    const errors = validateQuestion(q, index);
    if (Object.keys(errors).length > 0) {
      allErrors[index] = errors;
      hasAnyError = true;
    }
  });

  questionErrors.value = allErrors;
  return !hasAnyError;
};

const getFieldError = (index, field) => {
  return questionErrors.value[index]?.[field] || null;
};

// Question cells validate eagerly, like the VeeFields (see `eager()`): first when the user leaves
// the cell, then on every change while the cell is invalid
const validateCell = (index, field) => {
  const question = questions.value[index];
  if (!question) return;

  const message = validateQuestion(question)[field];
  const rowErrors = { ...questionErrors.value[index] };
  if (message) {
    rowErrors[field] = message;
  } else {
    delete rowErrors[field];
  }
  questionErrors.value = { ...questionErrors.value, [index]: rowErrors };
};

// Deferred because InputNumber emits `blur` before the value it commits on blur
const onCellBlur = (index, field) => queueMicrotask(() => validateCell(index, field));

const onCellUpdate = (index, field, value) => {
  questions.value[index][field] = value;
  if (hasFieldError(index, field)) validateCell(index, field);
};

const SERVER_QUESTION_FIELD = /^questions\[(\d+)\]\.(cost|text|answer)$/;

// Server errors for the table's cells, in the shape validateAllQuestions produces
const toQuestionErrors = (errors = {}) => {
  const byIndex = {};
  for (const [field, messages] of Object.entries(errors)) {
    const match = SERVER_QUESTION_FIELD.exec(field);
    if (!match) continue;
    const [, index, name] = match;
    byIndex[index] = { ...byIndex[index], [name]: messages.join(" ") };
  }
  return byIndex;
};

const SERVER_AUTHOR_FIELD = /^authors\[(\d+)\]\.username$/;

// Server errors for the co-author rows, keyed by the index in the sent list
const toAuthorErrors = (errors = {}) => {
  const byIndex = {};
  for (const [field, messages] of Object.entries(errors)) {
    const match = SERVER_AUTHOR_FIELD.exec(field);
    if (match) byIndex[match[1]] = messages.join(" ");
  }
  return byIndex;
};

const hasFieldError = (index, field) => {
  return !!getFieldError(index, field);
};

// Shows the saved topic, or the empty form when `savedTopic` is null, and marks it clean
const applyTopic = (savedTopic) => {
  topic.value = savedTopic;

  formData.value = savedTopic
    ? {
        title: savedTopic.title,
        description: savedTopic.description || "",
        isAuthor: savedTopic.isAuthor,
      }
    : createEmptyFormData();

  questions.value =
    savedTopic?.questions?.length > 0
      ? savedTopic.questions.map((q) => ({
          id: q.id,
          questionNumber: q.questionNumber,
          cost: q.cost,
          text: q.text,
          answer: q.answer,
          comment: q.comment || "",
        }))
      : createEmptyQuestions();

  coAuthors.value = (savedTopic?.authors ?? [])
    .filter((author) => !author.isOwner)
    .map((author) => ({ username: author.username }));

  formStatus.value = "idle";
  generalError.value = null;
  questionErrors.value = {};
  authorErrors.value = {};
  newAuthorUsername.value = "";
  addAuthorError.value = null;
  markClean();
};

const loadTopic = async () => {
  if (!isEditMode.value) {
    applyTopic(null);
    return;
  }

  // Already shown, for example right after creating it
  if (topic.value?.id === topicId.value) return;

  isLoading.value = true;
  try {
    const result = await topicService.getTopicById(topicId.value);
    if (result.success) {
      applyTopic(result.data);
    } else {
      notify.error("Load Failed", result.message);
      router.push({ name: "topics" });
    }
  } catch (error) {
    console.error("Error fetching topic:", error);
    notify.error("Load Failed", "Could not load the topic. Please try again.");
    router.push({ name: "topics" });
  } finally {
    isLoading.value = false;
  }
};

// The router keeps this component when going from /topics/new to /topics/:id, so follow the route
watch(
  () => route.params.id,
  () => {
    if (TOPIC_ROUTE_NAMES.includes(route.name)) loadTopic();
  },
  { immediate: true },
);

const handlePaste = (event) => {
  if (readOnly.value || !event.clipboardData) return;

  const rows = parseClipboardTable(event.clipboardData);

  // Expected format: cost, question, answer, comment (4 columns)
  // Or: question, answer, comment (3 columns minimum)
  if (rows.length === 0) return;

  const firstRowCols = rows[0].length;

  if (firstRowCols !== 4 && firstRowCols !== 3) return;

  const maxQuestions = TOPIC_LIMITS.questionsPerTopicMax;
  const rowsToProcess = rows.slice(0, maxQuestions);

  questions.value = rowsToProcess.map((row, index) => {
    const existingId = questions.value[index]?.id || null;
    const questionNumber = index + 1;

    if (firstRowCols === 4) {
      // Full format: cost, question, answer, comment
      return {
        id: existingId,
        questionNumber,
        cost: parseInt(row[0]) || defaultCost(questionNumber),
        text: row[1]?.trim() || "",
        answer: row[2]?.trim() || "",
        comment: row[3]?.trim() || "",
      };
    }

    // Minimal format: question, answer, comment
    return {
      id: existingId,
      questionNumber,
      cost: defaultCost(questionNumber),
      text: row[0]?.trim() || "",
      answer: row[1]?.trim() || "",
      comment: row[2]?.trim() || "",
    };
  });

  questionErrors.value = {};
  event.preventDefault();

  if (rows.length > maxQuestions) {
    notify.warn("Too Many Rows", `Only the first ${maxQuestions} rows were imported.`);
  } else {
    notify.success("Questions imported", "Questions were pasted from the clipboard.");
  }
};

const onInvalidSubmit = () => {
  // Also validate questions when VeeValidate's validation fails
  validateAllQuestions();
  focusFirstInvalid(formRef.value?.$el);
};

const onSubmit = async (values, { setErrors }) => {
  if (!validateAllQuestions()) {
    generalError.value = null;
    await focusFirstInvalid(formRef.value?.$el);
    return;
  }

  formStatus.value = "loading";
  generalError.value = null;

  try {
    const questionsToSend = questions.value.map((q) => ({
      ...(q.id && { id: q.id }),
      questionNumber: q.questionNumber,
      cost: q.cost,
      text: q.text.trim(),
      answer: q.answer.trim(),
      comment: q.comment?.trim() || null,
    }));

    const topicData = {
      title: values.title,
      description: formData.value.description?.trim() || null,
      isAuthor: formData.value.isAuthor,
      questions: questionsToSend,
      authors: coAuthors.value.map((coAuthor) => ({ username: coAuthor.username })),
    };

    const isCreating = !isEditMode.value;
    const result = isCreating
      ? await topicService.createTopic(topicData)
      : await topicService.updateTopic(topicId.value, topicData);

    if (result.success) {
      // Repopulate from the response, so new questions get their ids and the next save keeps them
      applyTopic(result.data);
      const verb = isCreating ? "created" : "updated";
      notify.success(`Topic ${verb}`, `"${result.data.title}" was ${verb}.`);
      if (isCreating) {
        await router.replace({ name: "topic-edit", params: { id: result.data.id } });
      }
    } else {
      const fieldsShown = Object.keys(result.errors ?? {}).filter(
        (field) => SERVER_QUESTION_FIELD.test(field) || SERVER_AUTHOR_FIELD.test(field),
      );
      formStatus.value = "error";
      generalError.value = toFormFailure(result, ["title", ...fieldsShown]).banner;
      setErrors(toFormErrors(result.errors, ["title"]));
      questionErrors.value = toQuestionErrors(result.errors);
      authorErrors.value = toAuthorErrors(result.errors);
      await focusFirstInvalid(formRef.value?.$el);
    }
  } catch {
    formStatus.value = "error";
    generalError.value = "An unexpected error occurred. Please try again.";
  }
};

const handleDelete = () => {
  const { id, title } = topic.value;

  confirm.require({
    message: `Are you sure you want to delete "${title}"?`,
    header: "Delete Topic",
    icon: "pi pi-exclamation-triangle",
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    acceptProps: { label: "Delete", severity: "danger" },
    accept: async () => {
      isDeleting.value = true;
      const result = await topicService.deleteTopic(id);
      isDeleting.value = false;

      if (result.success) {
        // Unsaved edits of a deleted topic aren't worth a leave prompt
        markClean();
        notify.success("Topic deleted", `"${title}" was deleted.`);
        router.push({ name: "topics" });
      } else {
        notify.error("Delete Failed", result.message);
      }
    },
  });
};
</script>

<template>
  <main class="container mx-auto px-4 py-8 max-w-8/10">
    <!-- Page Header -->
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100">
        {{ pageTitle }}
      </h1>
    </div>

    <Message v-if="readOnly && !isLoading" severity="info" class="mb-8">
      This topic has been played, so it can't be changed or deleted.
    </Message>

    <!-- Loading State -->
    <div v-if="isLoading" class="text-center py-12">
      <div class="text-gray-500 dark:text-gray-400">
        <i class="pi pi-spin pi-spinner text-4xl mb-4 block"></i>
        <p class="text-lg">Loading topic...</p>
      </div>
    </div>

    <!-- Form -->
    <VeeForm v-else ref="formRef" @submit="onSubmit" @invalid-submit="onInvalidSubmit" class="space-y-8">
      <!-- Basic Information Card -->
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-md p-6">
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <!-- Title -->
          <div class="flex flex-col gap-2">
            <label for="title" class="font-semibold text-gray-700 dark:text-gray-300">Title *</label>
            <VeeField name="title" label="Title" rules="required|max:100" v-model="formData.title" v-slot="slot">
              <InputText
                v-bind="eager(slot)"
                id="title"
                class="w-full"
                placeholder="Enter topic title"
                :disabled="inputsDisabled"
              />
            </VeeField>
            <ErrorMessage name="title" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <!-- Description -->
          <div class="flex flex-col gap-2 md:col-span-2">
            <label for="description" class="font-semibold text-gray-700 dark:text-gray-300">Description</label>
            <Textarea
              id="description"
              v-model="formData.description"
              rows="3"
              class="w-full"
              placeholder="Enter topic description (optional)"
              :disabled="inputsDisabled"
            />
          </div>

          <!-- Checkbox -->
          <div class="flex items-center gap-3 md:col-span-2">
            <Checkbox v-model="formData.isAuthor" inputId="isAuthor" :binary="true" :disabled="inputsDisabled" />
            <label for="isAuthor" class="cursor-pointer text-gray-700 dark:text-gray-300">
              <span class="font-medium">I am the Author</span>
              <span class="block text-sm text-gray-500">Check if you authored this topic's questions</span>
            </label>
          </div>
        </div>
      </div>

      <!-- Questions Table Card -->
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-md p-6" @paste="handlePaste">
        <div class="flex items-center justify-between mb-6">
          <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100 flex items-center gap-2">
            <i class="pi pi-list text-blue-500"></i>
            Questions
          </h2>
        </div>

        <div v-if="!readOnly" class="mb-4 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
          <p class="text-sm text-blue-700 dark:text-blue-300">
            <strong>Paste Format:</strong> Select cells in Google Sheets containing your questions (4 columns: Cost,
            Question, Answer, Comment) and paste here. Alternatively, use 3 columns: Question, Answer, Comment - costs
            will be auto-generated. Pasting replaces the whole table (up to
            {{ TOPIC_LIMITS.questionsPerTopicMax }} rows).
          </p>
        </div>

        <DataTable
          :value="questions"
          editMode="cell"
          dataKey="questionNumber"
          class="editable-cells-table"
          responsiveLayout="scroll"
          stripedRows
        >
          <Column field="questionNumber" header="#" style="width: 50px" class="text-center">
            <template #body="{ data }">
              <span class="font-bold text-gray-600 dark:text-gray-400">{{ data.questionNumber }}</span>
            </template>
          </Column>

          <Column field="cost" header="Cost *" style="width: 120px">
            <template #body="{ data, index }">
              <div class="flex flex-col gap-1">
                <InputNumber
                  :model-value="data.cost"
                  @update:model-value="(value) => onCellUpdate(index, 'cost', value)"
                  @blur="onCellBlur(index, 'cost')"
                  class="w-full"
                  :inputClass="['w-full text-center', hasFieldError(index, 'cost') ? 'p-invalid' : '']"
                  :invalid="hasFieldError(index, 'cost')"
                  :disabled="inputsDisabled"
                />
                <small v-if="hasFieldError(index, 'cost')" class="text-red-500 text-xs">
                  {{ getFieldError(index, "cost") }}
                </small>
              </div>
            </template>
          </Column>

          <Column field="text" header="Question *" style="min-width: 300px">
            <template #body="{ data, index }">
              <div class="flex flex-col gap-1">
                <Textarea
                  :model-value="data.text"
                  @update:model-value="(value) => onCellUpdate(index, 'text', value)"
                  @blur="onCellBlur(index, 'text')"
                  class="w-full"
                  :class="{ 'p-invalid': hasFieldError(index, 'text') }"
                  placeholder="Enter question"
                  rows="3"
                  autoResize
                  :invalid="hasFieldError(index, 'text')"
                  :disabled="inputsDisabled"
                />
                <small v-if="hasFieldError(index, 'text')" class="text-red-500 text-xs">
                  {{ getFieldError(index, "text") }}
                </small>
              </div>
            </template>
          </Column>

          <Column field="answer" header="Answer *" style="min-width: 200px">
            <template #body="{ data, index }">
              <div class="flex flex-col gap-1">
                <Textarea
                  :model-value="data.answer"
                  @update:model-value="(value) => onCellUpdate(index, 'answer', value)"
                  @blur="onCellBlur(index, 'answer')"
                  class="w-full"
                  :class="{ 'p-invalid': hasFieldError(index, 'answer') }"
                  placeholder="Enter answer"
                  rows="3"
                  autoResize
                  :invalid="hasFieldError(index, 'answer')"
                  :disabled="inputsDisabled"
                />
                <small v-if="hasFieldError(index, 'answer')" class="text-red-500 text-xs">
                  {{ getFieldError(index, "answer") }}
                </small>
              </div>
            </template>
          </Column>

          <Column field="comment" header="Comment" style="min-width: 200px">
            <template #body="{ data }">
              <Textarea
                v-model="data.comment"
                class="w-full"
                placeholder="Optional comment"
                rows="3"
                autoResize
                :disabled="inputsDisabled"
              />
            </template>
          </Column>

          <Column v-if="!readOnly" style="width: 60px" class="text-center">
            <template #body="{ index }">
              <Button
                type="button"
                icon="pi pi-trash"
                severity="danger"
                text
                rounded
                aria-label="Remove question"
                :disabled="!canRemoveQuestion || isBusy"
                @click="removeQuestion(index)"
              />
            </template>
          </Column>
        </DataTable>

        <div class="flex items-center justify-between mt-4">
          <Button
            v-if="!readOnly"
            type="button"
            label="Add Question"
            icon="pi pi-plus"
            outlined
            :disabled="!canAddQuestion || isBusy"
            @click="addQuestion"
          />
          <span class="ml-auto text-sm text-gray-500 dark:text-gray-400">
            {{ questions.length }} / {{ TOPIC_LIMITS.questionsPerTopicMax }} questions
          </span>
        </div>
      </div>

      <!-- Authors Card -->
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-md p-6">
        <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100 flex items-center gap-2 mb-6">
          <i class="pi pi-users text-blue-500"></i>
          Authors
        </h2>

        <div v-if="!readOnly" class="flex gap-2 mb-4">
          <div class="flex-1 flex flex-col gap-1">
            <label for="newAuthorUsername" class="sr-only">Username</label>
            <InputText
              id="newAuthorUsername"
              v-model="newAuthorUsername"
              placeholder="Username"
              class="w-full"
              :invalid="!!addAuthorError"
              :disabled="isBusy"
              @keydown.enter.prevent="addCoAuthor"
            />
            <Message v-if="addAuthorError" severity="error" variant="simple">{{ addAuthorError }}</Message>
          </div>
          <Button
            type="button"
            label="Add"
            icon="pi pi-plus"
            outlined
            class="self-start"
            :disabled="isBusy"
            @click="addCoAuthor"
          />
        </div>

        <DataTable :value="authorRows" dataKey="username" stripedRows>
          <Column header="Username">
            <template #body="{ data }">
              <div class="flex flex-col gap-1">
                <span
                  :tabindex="authorErrors[data.coAuthorIndex] ? -1 : undefined"
                  :aria-invalid="authorErrors[data.coAuthorIndex] ? 'true' : undefined"
                >
                  {{ data.username }}
                </span>
                <small v-if="authorErrors[data.coAuthorIndex]" class="text-red-500 text-xs">
                  {{ authorErrors[data.coAuthorIndex] }}
                </small>
              </div>
            </template>
          </Column>

          <Column header="Role">
            <template #body="{ data }">
              <div class="flex gap-2">
                <Tag v-if="data.isOwner" value="Owner" severity="contrast" />
                <Tag v-if="data.isAuthor" value="Author" severity="info" />
              </div>
            </template>
          </Column>

          <Column v-if="!readOnly" style="width: 60px" class="text-center">
            <template #body="{ data }">
              <Button
                v-if="!data.isOwner"
                type="button"
                icon="pi pi-trash"
                severity="danger"
                text
                rounded
                aria-label="Remove author"
                :disabled="isBusy"
                @click="removeCoAuthor(data.coAuthorIndex)"
              />
            </template>
          </Column>
        </DataTable>
      </div>

      <!-- Status Messages -->
      <div v-if="formStatus === 'loading'" class="mb-4">
        <Message severity="info">{{ isEditMode ? "Updating" : "Creating" }} the topic, please wait.</Message>
      </div>
      <div v-if="formStatus === 'error' && generalError" class="mb-4">
        <Message severity="error">{{ generalError }}</Message>
      </div>

      <!-- Action Buttons -->
      <div class="flex items-center gap-4 pt-4">
        <!-- The span shows the tooltip, since a disabled button gets no mouse events -->
        <span v-if="isEditMode" v-tooltip.top="{ value: PLAYED_DELETE_HINT, disabled: !readOnly }">
          <Button
            type="button"
            label="Delete Topic"
            icon="pi pi-trash"
            severity="danger"
            outlined
            :disabled="readOnly || isBusy"
            :loading="isDeleting"
            @click="handleDelete"
          />
        </span>
        <div class="ml-auto flex gap-4">
          <RouterLink :to="{ name: 'topics' }" custom v-slot="{ navigate }">
            <Button
              type="button"
              :label="readOnly ? 'Back' : 'Cancel'"
              severity="secondary"
              outlined
              :icon="readOnly ? 'pi pi-arrow-left' : 'pi pi-times'"
              @click="navigate"
              :disabled="isBusy"
            />
          </RouterLink>
          <Button
            v-if="!readOnly"
            type="submit"
            :label="isEditMode ? 'Save Changes' : 'Create Topic'"
            icon="pi pi-check"
            :disabled="isBusy"
            :loading="isFormProcessing"
          />
        </div>
      </div>
    </VeeForm>
  </main>
</template>

<style scoped></style>
