<script setup>
import { ref, computed, watch, nextTick } from "vue";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import Textarea from "primevue/textarea";
import Button from "primevue/button";
import Message from "primevue/message";
import DatePicker from "primevue/datepicker";
import InputNumber from "primevue/inputnumber";
import { Form as VeeForm, Field as VeeField, ErrorMessage, defineRule, configure } from "vee-validate";
import { required, min, max, min_value, max_value } from "@vee-validate/rules";
import { tournamentService } from "@/services/tournament-service.js";

defineRule("required", required);
defineRule("min", min);
defineRule("max", max);
defineRule("min_value", min_value);
defineRule("max_value", max_value);

configure({
  generateMessage: (ctx) => {
    const rule = ctx.rule?.name;
    switch (rule) {
      case "required":
        return "Required";
      case "min":
        return `Must be at least ${ctx.rule?.params[0]} characters`;
      case "max":
        return `Must be at most ${ctx.rule?.params[0]} characters`;
      case "min_value":
        return `Must be at least ${ctx.rule?.params[0]}`;
      case "max_value":
        return `Must be at most ${ctx.rule?.params[0]}`;
      default:
        return "Field is invalid";
    }
  },
  validateOnBlur: true,
  validateOnChange: true,
  validateOnInput: false,
  validateOnModelUpdate: false,
});

const props = defineProps({
  visible: {
    type: Boolean,
    required: true,
  },
  mode: {
    type: String,
    default: "create",
    validator: (val) => ["create", "edit"].includes(val),
  },
  tournament: {
    type: Object,
    default: null,
  },
});

const emit = defineEmits(["update:visible", "tournament-created", "tournament-updated"]);

const DEFAULT_VALUES = {
  participantsPerMatch: 4,
  participantsPerTournament: 12,
  questionsCostMax: 50,
  questionsCostMin: -50,
  questionsPerTopicMax: 5,
  questionsPerTopicMin: 5,
  topicsAuthorsMax: 5,
  topicsPerParticipantMax: 10,
  topicsPerParticipantMin: 6,
  topicsPerMatch: 4,
};

const formData = ref({
  title: "",
  description: "",
  plannedStart: null,
  participantsPerMatch: DEFAULT_VALUES.participantsPerMatch,
  participantsPerTournament: DEFAULT_VALUES.participantsPerTournament,
  questionsCostMax: DEFAULT_VALUES.questionsCostMax,
  questionsCostMin: DEFAULT_VALUES.questionsCostMin,
  questionsPerTopicMax: DEFAULT_VALUES.questionsPerTopicMax,
  questionsPerTopicMin: DEFAULT_VALUES.questionsPerTopicMin,
  topicsAuthorsMax: DEFAULT_VALUES.topicsAuthorsMax,
  topicsPerParticipantMax: DEFAULT_VALUES.topicsPerParticipantMax,
  topicsPerParticipantMin: DEFAULT_VALUES.topicsPerParticipantMin,
  topicsPerMatch: DEFAULT_VALUES.topicsPerMatch,
});

const formStatus = ref("idle"); // idle | loading | success | error
const generalError = ref(null);
const veeFormRef = ref(null);

const isFormProcessing = computed(() => formStatus.value === "loading");

watch(
  () => [props.visible, props.mode, props.tournament],
  async ([visible, mode, tournament]) => {
    if (visible && mode === "edit" && tournament) {
      const values = {
        title: tournament.title || "",
        description: tournament.description || "",
        plannedStart: tournament.plannedStart ? new Date(tournament.plannedStart) : null,
        participantsPerMatch: tournament.participantsPerMatch ?? DEFAULT_VALUES.participantsPerMatch,
        participantsPerTournament: tournament.participantsPerTournament ?? DEFAULT_VALUES.participantsPerTournament,
        questionsCostMax: tournament.questionsCostMax ?? DEFAULT_VALUES.questionsCostMax,
        questionsCostMin: tournament.questionsCostMin ?? DEFAULT_VALUES.questionsCostMin,
        questionsPerTopicMax: tournament.questionsPerTopicMax ?? DEFAULT_VALUES.questionsPerTopicMax,
        questionsPerTopicMin: tournament.questionsPerTopicMin ?? DEFAULT_VALUES.questionsPerTopicMin,
        topicsAuthorsMax: tournament.topicsAuthorsMax ?? DEFAULT_VALUES.topicsAuthorsMax,
        topicsPerParticipantMax: tournament.topicsPerParticipantMax ?? DEFAULT_VALUES.topicsPerParticipantMax,
        topicsPerParticipantMin: tournament.topicsPerParticipantMin ?? DEFAULT_VALUES.topicsPerParticipantMin,
        topicsPerMatch: tournament.topicsPerMatch ?? DEFAULT_VALUES.topicsPerMatch,
      };

      formData.value = values;

      await nextTick();

      if (veeFormRef.value) {
        veeFormRef.value.setValues(values);
      }
    }
  },
  { immediate: true }
);

const closeDialog = () => {
  emit("update:visible", false);
};

const resetForm = () => {
  const defaultValues = {
    title: "",
    description: "",
    plannedStart: null,
    participantsPerMatch: DEFAULT_VALUES.participantsPerMatch,
    participantsPerTournament: DEFAULT_VALUES.participantsPerTournament,
    questionsCostMax: DEFAULT_VALUES.questionsCostMax,
    questionsCostMin: DEFAULT_VALUES.questionsCostMin,
    questionsPerTopicMax: DEFAULT_VALUES.questionsPerTopicMax,
    questionsPerTopicMin: DEFAULT_VALUES.questionsPerTopicMin,
    topicsAuthorsMax: DEFAULT_VALUES.topicsAuthorsMax,
    topicsPerParticipantMax: DEFAULT_VALUES.topicsPerParticipantMax,
    topicsPerParticipantMin: DEFAULT_VALUES.topicsPerParticipantMin,
    topicsPerMatch: DEFAULT_VALUES.topicsPerMatch,
  };

  formData.value = defaultValues;
  formStatus.value = "idle";
  generalError.value = null;

  if (veeFormRef.value) {
    veeFormRef.value.resetForm({ values: defaultValues });
  }
};

const onSubmit = async (values) => {
  formStatus.value = "loading";
  generalError.value = null;

  try {
    const tournamentData = {
      title: values.title,
      description: values.description || null,
      plannedStart: values.plannedStart.toISOString(),
      participantsPerMatch: formData.value.participantsPerMatch,
      participantsPerTournament: formData.value.participantsPerTournament,
      questionsCostMax: formData.value.questionsCostMax,
      questionsCostMin: formData.value.questionsCostMin,
      questionsPerTopicMax: formData.value.questionsPerTopicMax,
      questionsPerTopicMin: formData.value.questionsPerTopicMin,
      topicsAuthorsMax: formData.value.topicsAuthorsMax,
      topicsPerParticipantMax: formData.value.topicsPerParticipantMax,
      topicsPerParticipantMin: formData.value.topicsPerParticipantMin,
      topicsPerMatch: formData.value.topicsPerMatch,
    };

    const result =
      props.mode === "create"
        ? await tournamentService.createTournament(tournamentData)
        : await tournamentService.updateTournament(props.tournament.id, tournamentData);

    if (result.success) {
      formStatus.value = "success";

      if (props.mode === "create") {
        emit("tournament-created", result.data);
      } else {
        emit("tournament-updated", result.data);
      }

      setTimeout(() => {
        resetForm();
      }, 500);
    } else {
      formStatus.value = "error";
      generalError.value = result.error;

      setTimeout(() => {
        formStatus.value = "idle";
      }, 5000);
    }
  } catch {
    formStatus.value = "error";
    generalError.value = "An unexpected error occurred. Please try again.";
    setTimeout(() => {
      formStatus.value = "idle";
    }, 5000);
  }
};
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="closeDialog"
    modal
    :header="mode === 'create' ? 'Create New Tournament' : 'Edit Tournament'"
    :draggable="false"
    :style="{ width: '50rem' }"
  >
    <VeeForm ref="veeFormRef" @submit="onSubmit">
      <!-- Basic Information -->
      <div class="mb-6">
        <h3 class="text-lg font-semibold mb-4">Basic Information</h3>

        <!-- Title -->
        <div class="flex flex-col gap-1 mb-4">
          <div class="flex items-center gap-4">
            <label for="title" class="font-semibold w-40">Title *</label>
            <VeeField name="title" :rules="'required|min:3|max:100'" v-slot="{ field }">
              <InputText v-bind="field" id="title" class="flex-auto" />
            </VeeField>
          </div>
          <ErrorMessage name="title" v-slot="{ message }">
            <Message severity="error">{{ message }}</Message>
          </ErrorMessage>
        </div>

        <!-- Description -->
        <div class="flex flex-col gap-1 mb-4">
          <div class="flex items-start gap-4">
            <label for="description" class="font-semibold w-40 pt-2">Description</label>
            <VeeField name="description" :rules="'max:500'" v-slot="{ field }">
              <Textarea v-bind="field" id="description" class="flex-auto" rows="3" />
            </VeeField>
          </div>
          <ErrorMessage name="description" v-slot="{ message }">
            <Message severity="error">{{ message }}</Message>
          </ErrorMessage>
        </div>

        <!-- Planned Start Date -->
        <div class="flex flex-col gap-1 mb-4">
          <div class="flex items-center gap-4">
            <label for="plannedStart" class="font-semibold w-40">Planned Start *</label>
            <VeeField name="plannedStart" :rules="'required'" v-slot="{ field, handleChange }">
              <DatePicker
                :modelValue="field.value"
                @update:modelValue="handleChange"
                inputId="plannedStart"
                showTime
                hourFormat="24"
                class="flex-auto"
                :minDate="new Date()"
              />
            </VeeField>
          </div>
          <ErrorMessage name="plannedStart" v-slot="{ message }">
            <Message severity="error">{{ message }}</Message>
          </ErrorMessage>
        </div>
      </div>

      <!-- Tournament Settings -->
      <div class="mb-6">
        <h3 class="text-lg font-semibold mb-4">Tournament Settings</h3>

        <div class="grid grid-cols-2 gap-4">
          <!-- Participants Per Match -->
          <div class="flex flex-col gap-1">
            <label for="participantsPerMatch" class="font-semibold text-sm">Participants Per Match</label>
            <InputNumber
              v-model="formData.participantsPerMatch"
              inputId="participantsPerMatch"
              :min="2"
              :max="10"
              showButtons
            />
          </div>

          <!-- Participants Per Tournament -->
          <div class="flex flex-col gap-1">
            <label for="participantsPerTournament" class="font-semibold text-sm">Participants Per Tournament</label>
            <InputNumber
              v-model="formData.participantsPerTournament"
              inputId="participantsPerTournament"
              :min="2"
              :max="1000"
              showButtons
            />
          </div>

          <!-- Questions Cost Min -->
          <div class="flex flex-col gap-1">
            <label for="questionsCostMin" class="font-semibold text-sm">Questions Cost Min</label>
            <InputNumber
              v-model="formData.questionsCostMin"
              inputId="questionsCostMin"
              :min="1"
              :max="1000"
              showButtons
            />
          </div>

          <!-- Questions Cost Max -->
          <div class="flex flex-col gap-1">
            <label for="questionsCostMax" class="font-semibold text-sm">Questions Cost Max</label>
            <InputNumber
              v-model="formData.questionsCostMax"
              inputId="questionsCostMax"
              :min="1"
              :max="1000"
              showButtons
            />
          </div>

          <!-- Questions Per Topic Min -->
          <div class="flex flex-col gap-1">
            <label for="questionsPerTopicMin" class="font-semibold text-sm">Questions Per Topic Min</label>
            <InputNumber
              v-model="formData.questionsPerTopicMin"
              inputId="questionsPerTopicMin"
              :min="1"
              :max="100"
              showButtons
            />
          </div>

          <!-- Questions Per Topic Max -->
          <div class="flex flex-col gap-1">
            <label for="questionsPerTopicMax" class="font-semibold text-sm">Questions Per Topic Max</label>
            <InputNumber
              v-model="formData.questionsPerTopicMax"
              inputId="questionsPerTopicMax"
              :min="1"
              :max="100"
              showButtons
            />
          </div>

          <!-- Topics Authors Max -->
          <div class="flex flex-col gap-1">
            <label for="topicsAuthorsMax" class="font-semibold text-sm">Topics Authors Max</label>
            <InputNumber
              v-model="formData.topicsAuthorsMax"
              inputId="topicsAuthorsMax"
              :min="1"
              :max="10"
              showButtons
            />
          </div>

          <!-- Topics Per Participant Min -->
          <div class="flex flex-col gap-1">
            <label for="topicsPerParticipantMin" class="font-semibold text-sm">Topics Per Participant Min</label>
            <InputNumber
              v-model="formData.topicsPerParticipantMin"
              inputId="topicsPerParticipantMin"
              :min="0"
              :max="20"
              showButtons
            />
          </div>

          <!-- Topics Per Participant Max -->
          <div class="flex flex-col gap-1">
            <label for="topicsPerParticipantMax" class="font-semibold text-sm">Topics Per Participant Max</label>
            <InputNumber
              v-model="formData.topicsPerParticipantMax"
              inputId="topicsPerParticipantMax"
              :min="0"
              :max="20"
              showButtons
            />
          </div>

          <!-- Topics Per Match -->
          <div class="flex flex-col gap-1">
            <label for="topicsPerMatch" class="font-semibold text-sm">Topics Per Match</label>
            <InputNumber v-model="formData.topicsPerMatch" inputId="topicsPerMatch" :min="1" :max="10" showButtons />
          </div>
        </div>
      </div>

      <!-- Status Messages -->
      <div class="mb-4" v-if="formStatus === 'loading'">
        <Message severity="info">{{ mode === "create" ? "Creating" : "Updating" }} tournament, please wait...</Message>
      </div>
      <div class="mb-4" v-if="formStatus === 'success'">
        <Message severity="success">Tournament {{ mode === "create" ? "created" : "updated" }} successfully!</Message>
      </div>
      <div class="mb-4" v-if="formStatus === 'error' && generalError">
        <Message severity="error">{{ generalError }}</Message>
      </div>

      <!-- Action Buttons -->
      <div class="flex justify-end gap-2">
        <Button type="button" label="Cancel" severity="secondary" @click="closeDialog" :disabled="isFormProcessing" />
        <Button type="submit" :label="mode === 'create' ? 'Create Tournament' : 'Save Changes'" :disabled="isFormProcessing" />
      </div>
    </VeeForm>
  </Dialog>
</template>

<style scoped></style>
