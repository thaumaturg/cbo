<script setup>
import { topicService } from "@/services/topic-service.js";
import Button from "primevue/button";
import Checkbox from "primevue/checkbox";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import InputNumber from "primevue/inputnumber";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import Textarea from "primevue/textarea";
import Toast from "primevue/toast";
import { useToast } from "primevue/usetoast";
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

const router = useRouter();
const route = useRoute();
const toast = useToast();

const isEditMode = computed(() => route.params.id && route.params.id !== "new");
const topicId = computed(() => (isEditMode.value ? parseInt(route.params.id) : null));
const pageTitle = computed(() => (isEditMode.value ? "Edit Topic" : "Create New Topic"));

const formData = ref({
  title: "",
  description: "",
  isPlayed: false,
  isAuthor: true,
});

const formStatus = ref("idle"); // idle | loading | success | error
const generalError = ref(null);
const isLoading = ref(false);
const veeFormRef = ref(null);
const questionErrors = ref({});

const questions = ref([
  { id: null, questionNumber: 1, costPositive: 10, costNegative: 10, text: "", answer: "", comment: "" },
  { id: null, questionNumber: 2, costPositive: 20, costNegative: 20, text: "", answer: "", comment: "" },
  { id: null, questionNumber: 3, costPositive: 30, costNegative: 30, text: "", answer: "", comment: "" },
  { id: null, questionNumber: 4, costPositive: 40, costNegative: 40, text: "", answer: "", comment: "" },
  { id: null, questionNumber: 5, costPositive: 50, costNegative: 50, text: "", answer: "", comment: "" },
]);

const isFormProcessing = computed(() => formStatus.value === "loading");

const validateCostField = (value, fieldName) => {
  if (value === null || value === undefined || value === "") {
    return `${fieldName} is required`;
  }
  if (value < -50) {
    return `${fieldName} must be at least -50`;
  }
  if (value > 50) {
    return `${fieldName} must be at most 50`;
  }
  return null;
};

const validateQuestion = (question) => {
  const errors = {};

  if (!question.text?.trim()) {
    errors.text = "Question is required";
  }
  if (!question.answer?.trim()) {
    errors.answer = "Answer is required";
  }

  const costPosError = validateCostField(question.costPositive, "Cost +");
  if (costPosError) errors.costPositive = costPosError;

  const costNegError = validateCostField(question.costNegative, "Cost -");
  if (costNegError) errors.costNegative = costNegError;

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
        isPlayed: topic.isPlayed,
        isAuthor: topic.isAuthor,
      };

      if (topic.questions && topic.questions.length > 0) {
        questions.value = topic.questions.map((q) => ({
          id: q.id,
          questionNumber: q.questionNumber,
          costPositive: q.costPositive,
          costNegative: q.costNegative,
          text: q.text,
          answer: q.answer,
          comment: q.comment || "",
        }));

        while (questions.value.length < 5) {
          const nextNum = questions.value.length + 1;
          questions.value.push({
            id: null,
            questionNumber: nextNum,
            costPositive: nextNum * 10,
            costNegative: nextNum * 10,
            text: "",
            answer: "",
            comment: "",
          });
        }
      }
    } else {
      toast.add({
        severity: "error",
        summary: "Error",
        detail: result.error || "Failed to load topic",
        life: 5000,
      });
      router.push("/");
    }
  } catch (error) {
    console.error("Error fetching topic:", error);
    toast.add({
      severity: "error",
      summary: "Error",
      detail: "An unexpected error occurred while loading the topic",
      life: 5000,
    });
    router.push("/");
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  fetchTopicData();
});

const handlePaste = (event) => {
  const clipboardData = event.clipboardData || window.clipboardData;
  const pastedData = clipboardData.getData("text");

  if (!pastedData) return;

  const rows = pastedData
    .split("\n")
    .map((row) => row.split("\t"))
    .filter((row) => row.some((cell) => cell.trim()));

  // Expected format: costPositive, costNegative, question, answer, comment (5 columns)
  // Or: question, answer, comment (3 columns minimum)
  if (rows.length === 0) return;

  const firstRowCols = rows[0].length;

  if (rows.length > 5) {
    toast.add({
      severity: "warn",
      summary: "Too Many Rows",
      detail: "Only the first 5 rows will be imported",
      life: 3000,
    });
  }

  const rowsToProcess = rows.slice(0, 5);

  if (firstRowCols !== 5 && firstRowCols !== 3) return;

  rowsToProcess.forEach((row, index) => {
    if (firstRowCols === 5) {
      // Full format: costPositive, costNegative, question, answer, comment
      questions.value[index] = {
        questionNumber: index + 1,
        costPositive: parseInt(row[0]) || (index + 1) * 10,
        costNegative: parseInt(row[1]) || (index + 1) * 10,
        text: row[2]?.trim() || "",
        answer: row[3]?.trim() || "",
        comment: row[4]?.trim() || "",
      };
    } else {
      // Minimal format: question, answer, comment
      questions.value[index] = {
        questionNumber: index + 1,
        costPositive: (index + 1) * 10,
        costNegative: (index + 1) * 10,
        text: row[0]?.trim() || "",
        answer: row[1]?.trim() || "",
        comment: row[2]?.trim() || "",
      };
    }
  });

  importSuccess(event);
};

const importSuccess = (event) => {
  toast.add({
    severity: "success",
    summary: "Data Imported",
    detail: `Imported from clipboard`,
    life: 3000,
  });

  event.preventDefault();
};

const onInvalidSubmit = () => {
  // Also validate questions when VeeValidate's validation fails
  validateAllQuestions();
};

const onSubmit = async (values) => {
  if (!validateAllQuestions()) {
    generalError.value = "Please fix the errors in the questions table.";
    return;
  }

  formStatus.value = "loading";
  generalError.value = null;

  try {
    const questionsToSend = questions.value
      .filter((q) => (isEditMode.value ? q.id : q.text.trim() && q.answer.trim()))
      .map((q) => ({
        ...(q.id && { id: q.id }),
        questionNumber: q.questionNumber,
        costPositive: q.costPositive,
        costNegative: q.costNegative,
        text: q.text.trim(),
        answer: q.answer.trim(),
        comment: q.comment?.trim() || null,
      }));

    const topicData = {
      title: values.title,
      description: formData.value.description?.trim() || null,
      isPlayed: formData.value.isPlayed,
      isAuthor: formData.value.isAuthor,
      questions: questionsToSend,
    };

    const result = isEditMode.value
      ? await topicService.updateTopic(topicId.value, topicData)
      : await topicService.createTopic(topicData);

    if (result.success) {
      formStatus.value = "success";
      setTimeout(() => {
        router.push("/");
      }, 1000);
    } else {
      formStatus.value = "error";
      generalError.value = result.error;
    }
  } catch {
    formStatus.value = "error";
    generalError.value = "An unexpected error occurred. Please try again.";
  }
};

const handleCancel = () => {
  router.push("/");
};
</script>

<template>
  <Toast />
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
    <VeeForm v-else ref="veeFormRef" @submit="onSubmit" @invalid-submit="onInvalidSubmit" class="space-y-8">
      <!-- Basic Information Card -->
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-md p-6">
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <!-- Title -->
          <div class="flex flex-col gap-2">
            <label for="title" class="font-semibold text-gray-700 dark:text-gray-300">Title *</label>
            <VeeField name="title" rules="required|min:3|max:100" v-model="formData.title" v-slot="{ field }">
              <InputText v-bind="field" id="title" class="w-full" placeholder="Enter topic title" />
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

          <!-- Checkboxes -->
          <div class="flex flex-wrap items-start gap-8 md:col-span-2">
            <div class="flex flex-col gap-2">
              <div class="flex items-center gap-3">
                <Checkbox v-model="formData.isPlayed" inputId="isPlayed" :binary="true" />
                <label for="isPlayed" class="cursor-pointer text-gray-700 dark:text-gray-300">
                  <span class="font-medium">Already Played</span>
                  <span class="block text-sm text-gray-500">Mark if this topic has already been used in a game</span>
                </label>
              </div>
              <div
                class="ml-8 p-2 bg-amber-50 dark:bg-amber-900/20 rounded border border-amber-300 dark:border-amber-700"
              >
                <p class="text-sm text-amber-700 dark:text-amber-300 flex items-center gap-2">
                  <i class="pi pi-exclamation-triangle"></i>
                  <span
                    ><strong>Warning:</strong> This will make the topic read-only. You won't be able to edit or delete
                    it later.</span
                  >
                </p>
              </div>
            </div>

            <div class="flex items-center gap-3 pt-0.5">
              <Checkbox v-model="formData.isAuthor" inputId="isAuthor" :binary="true" />
              <label for="isAuthor" class="cursor-pointer text-gray-700 dark:text-gray-300">
                <span class="font-medium">I am the Author</span>
                <span class="block text-sm text-gray-500">Check if you authored this topic's questions</span>
              </label>
            </div>
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
            <strong>Paste Format:</strong> Select cells in Google Sheets containing your questions (5 columns:
            CostPositive, CostNegative, Question, Answer, Comment) and paste here. Alternatively, use 3 columns:
            Question, Answer, Comment - costs will be auto-generated.
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

          <Column field="costPositive" header="Cost + *" style="width: 120px">
            <template #body="{ data, index }">
              <div class="flex flex-col gap-1">
                <InputNumber
                  v-model="data.costPositive"
                  class="w-full"
                  :inputClass="['w-full text-center', hasFieldError(index, 'costPositive') ? 'p-invalid' : '']"
                  :invalid="hasFieldError(index, 'costPositive')"
                />
                <small v-if="hasFieldError(index, 'costPositive')" class="text-red-500 text-xs">
                  {{ getFieldError(index, "costPositive") }}
                </small>
              </div>
            </template>
          </Column>

          <Column field="costNegative" header="Cost - *" style="width: 120px">
            <template #body="{ data, index }">
              <div class="flex flex-col gap-1">
                <InputNumber
                  v-model="data.costNegative"
                  class="w-full"
                  :inputClass="['w-full text-center', hasFieldError(index, 'costNegative') ? 'p-invalid' : '']"
                  :invalid="hasFieldError(index, 'costNegative')"
                />
                <small v-if="hasFieldError(index, 'costNegative')" class="text-red-500 text-xs">
                  {{ getFieldError(index, "costNegative") }}
                </small>
              </div>
            </template>
          </Column>

          <Column field="text" header="Question *" style="min-width: 300px">
            <template #body="{ data, index }">
              <div class="flex flex-col gap-1">
                <Textarea
                  v-model="data.text"
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
                  v-model="data.answer"
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
        </DataTable>
      </div>

      <!-- Status Messages -->
      <div v-if="formStatus === 'loading'" class="mb-4">
        <Message severity="info">{{ isEditMode ? "Updating" : "Creating" }} topic, please wait...</Message>
      </div>
      <div v-if="formStatus === 'success'" class="mb-4">
        <Message severity="success">Topic {{ isEditMode ? "updated" : "created" }} successfully!</Message>
      </div>
      <div v-if="formStatus === 'error' && generalError" class="mb-4">
        <Message severity="error">{{ generalError }}</Message>
      </div>

      <!-- Action Buttons -->
      <div class="flex justify-end gap-4 pt-4">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          outlined
          icon="pi pi-times"
          @click="handleCancel"
          :disabled="isFormProcessing"
        />
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

<style scoped>
:deep(.p-datatable .p-datatable-tbody > tr > td) {
  padding: 0.5rem;
  vertical-align: top;
}

:deep(.p-datatable .p-datatable-thead > tr > th) {
  padding: 0.75rem 0.5rem;
  font-weight: 600;
}

:deep(.p-inputtext) {
  width: 100%;
}

:deep(.p-inputnumber) {
  width: 100%;
}

:deep(.p-inputnumber-input) {
  text-align: center;
}
</style>
