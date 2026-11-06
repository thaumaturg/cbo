<script setup>
import { eager } from "@/includes/validation.js";
import { topicService } from "@/services/topic-service.js";
import { toFormErrors, toFormFailure } from "@/utils/error.js";
import { focusFirstInvalid } from "@/utils/focus.js";
import { useNotify } from "@/utils/notify.js";
import { parseClipboardTable } from "@/utils/clipboard-parser.js";
import Button from "primevue/button";
import Checkbox from "primevue/checkbox";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import InputNumber from "primevue/inputnumber";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import Textarea from "primevue/textarea";
import { computed, onMounted, ref } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";

const router = useRouter();
const route = useRoute();
const notify = useNotify();

const isEditMode = computed(() => route.params.id && route.params.id !== "new");
const topicId = computed(() => (isEditMode.value ? route.params.id : null));
const pageTitle = computed(() => (isEditMode.value ? "Edit Topic" : "Create New Topic"));

const formData = ref({
  title: "",
  description: "",
  isAuthor: true,
});

const formStatus = ref("idle"); // idle | loading | error
const generalError = ref(null);
const isLoading = ref(false);
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

const questions = ref(Array.from({ length: DEFAULT_QUESTION_COUNT }, (_, index) => createEmptyQuestion(index + 1)));

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

const hasFieldError = (index, field) => {
  return !!getFieldError(index, field);
};

const fetchTopicData = async () => {
  if (!isEditMode.value) return;

  isLoading.value = true;
  try {
    const result = await topicService.getTopicById(topicId.value);
    if (result.success) {
      const topic = result.data;
      formData.value = {
        title: topic.title,
        description: topic.description || "",
        isAuthor: topic.isAuthor,
      };

      if (topic.questions && topic.questions.length > 0) {
        questions.value = topic.questions.map((q) => ({
          id: q.id,
          questionNumber: q.questionNumber,
          cost: q.cost,
          text: q.text,
          answer: q.answer,
          comment: q.comment || "",
        }));
      }
    } else {
      notify.error("Load Failed", result.message);
      router.push("/");
    }
  } catch (error) {
    console.error("Error fetching topic:", error);
    notify.error("Load Failed", "Could not load the topic. Please try again.");
    router.push("/");
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  fetchTopicData();
});

const handlePaste = (event) => {
  if (!event.clipboardData) return;

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
    };

    const result = isEditMode.value
      ? await topicService.updateTopic(topicId.value, topicData)
      : await topicService.createTopic(topicData);

    if (result.success) {
      formStatus.value = "idle";
      const verb = isEditMode.value ? "updated" : "created";
      notify.success(`Topic ${verb}`, `"${topicData.title}" was ${verb}.`);
      router.push("/");
    } else {
      const questionFields = Object.keys(result.errors ?? {}).filter((field) => SERVER_QUESTION_FIELD.test(field));
      formStatus.value = "error";
      generalError.value = toFormFailure(result, ["title", ...questionFields]).banner;
      setErrors(toFormErrors(result.errors, ["title"]));
      questionErrors.value = toQuestionErrors(result.errors);
      await focusFirstInvalid(formRef.value?.$el);
    }
  } catch {
    formStatus.value = "error";
    generalError.value = "An unexpected error occurred. Please try again.";
  }
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
              <InputText v-bind="eager(slot)" id="title" class="w-full" placeholder="Enter topic title" />
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
            />
          </div>

          <!-- Checkbox -->
          <div class="flex items-center gap-3 md:col-span-2">
            <Checkbox v-model="formData.isAuthor" inputId="isAuthor" :binary="true" />
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

        <div class="mb-4 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
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
                />
                <small v-if="hasFieldError(index, 'answer')" class="text-red-500 text-xs">
                  {{ getFieldError(index, "answer") }}
                </small>
              </div>
            </template>
          </Column>

          <Column field="comment" header="Comment" style="min-width: 200px">
            <template #body="{ data }">
              <Textarea v-model="data.comment" class="w-full" placeholder="Optional comment" rows="3" autoResize />
            </template>
          </Column>

          <Column style="width: 60px" class="text-center">
            <template #body="{ index }">
              <Button
                type="button"
                icon="pi pi-trash"
                severity="danger"
                text
                rounded
                aria-label="Remove question"
                :disabled="!canRemoveQuestion || isFormProcessing"
                @click="removeQuestion(index)"
              />
            </template>
          </Column>
        </DataTable>

        <div class="flex items-center justify-between mt-4">
          <Button
            type="button"
            label="Add Question"
            icon="pi pi-plus"
            outlined
            :disabled="!canAddQuestion || isFormProcessing"
            @click="addQuestion"
          />
          <span class="text-sm text-gray-500 dark:text-gray-400">
            {{ questions.length }} / {{ TOPIC_LIMITS.questionsPerTopicMax }} questions
          </span>
        </div>
      </div>

      <!-- Status Messages -->
      <div v-if="formStatus === 'loading'" class="mb-4">
        <Message severity="info">{{ isEditMode ? "Updating" : "Creating" }} the topic, please wait.</Message>
      </div>
      <div v-if="formStatus === 'error' && generalError" class="mb-4">
        <Message severity="error">{{ generalError }}</Message>
      </div>

      <!-- Action Buttons -->
      <div class="flex justify-end gap-4 pt-4">
        <RouterLink to="/" custom v-slot="{ navigate }">
          <Button
            type="button"
            label="Cancel"
            severity="secondary"
            outlined
            icon="pi pi-times"
            @click="navigate"
            :disabled="isFormProcessing"
          />
        </RouterLink>
        <Button
          type="submit"
          :label="isEditMode ? 'Save Changes' : 'Create Topic'"
          icon="pi pi-check"
          :disabled="isFormProcessing"
          :loading="isFormProcessing"
        />
      </div>
    </VeeForm>
  </main>
</template>

<style scoped></style>
