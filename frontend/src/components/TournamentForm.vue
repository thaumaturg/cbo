<script setup>
import { LIMITS, TOURNAMENT_DEFAULTS } from "@/domain/limits.js";
import { eager } from "@/includes/validation.js";
import { tournamentService } from "@/services/tournament-service.js";
import { toFormFailure } from "@/utils/error.js";
import { focusFirstInvalid } from "@/utils/focus.js";
import Button from "primevue/button";
import InputNumber from "primevue/inputnumber";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import Select from "primevue/select";
import Textarea from "primevue/textarea";
import { computed, ref, watch } from "vue";

const props = defineProps({
  // The tournament to edit, or null to create one.
  tournament: {
    type: Object,
    default: null,
  },
  readOnly: {
    type: Boolean,
    default: false,
  },
  submitLabel: {
    type: String,
    default: null,
  },
});

const emit = defineEmits(["saved", "update:dirty"]);

const {
  supportedSizes,
  titleLengthMin,
  titleLengthMax,
  descriptionLengthMax,
  topicsPerParticipantFloor,
  topicsPerParticipantCeiling,
} = LIMITS.tournaments;

const TITLE_RULES = `required|min:${titleLengthMin}|max:${titleLengthMax}`;
const DESCRIPTION_RULES = `max:${descriptionLengthMax}`;
const TOPICS_RANGE = `min_value:${topicsPerParticipantFloor}|max_value:${topicsPerParticipantCeiling}`;
const TOPICS_MAX_RULES = `required|${TOPICS_RANGE}`;
const TOPICS_MIN_RULES = `${TOPICS_MAX_RULES}|lte_field:@topicsPerParticipantMax`;

const toFormValues = (tournament) => ({
  title: tournament?.title || "",
  description: tournament?.description || "",
  playersPerTournament: tournament?.playersPerTournament ?? TOURNAMENT_DEFAULTS.playersPerTournament,
  topicsPerParticipantMax: tournament?.topicsPerParticipantMax ?? TOURNAMENT_DEFAULTS.topicsPerParticipantMax,
  topicsPerParticipantMin: tournament?.topicsPerParticipantMin ?? TOURNAMENT_DEFAULTS.topicsPerParticipantMin,
});

const initialValues = toFormValues(props.tournament);

const FORM_FIELDS = Object.keys(initialValues);

const isCreate = computed(() => !props.tournament);
const resolvedSubmitLabel = computed(
  () => props.submitLabel ?? (isCreate.value ? "Create Tournament" : "Save Changes"),
);

const formStatus = ref("idle"); // idle | loading | error
const generalError = ref(null);
const veeFormRef = ref(null);

const isFormProcessing = computed(() => formStatus.value === "loading");
const dirty = computed(() => veeFormRef.value?.meta.dirty ?? false);

watch(dirty, (value) => emit("update:dirty", value));

const reset = () => {
  formStatus.value = "idle";
  generalError.value = null;
  veeFormRef.value?.resetForm({ values: toFormValues(props.tournament) });
};

// A new or reloaded tournament replaces whatever the form holds.
watch(() => props.tournament, reset);

const onSubmit = async (values, { setErrors }) => {
  if (props.readOnly) {
    return;
  }

  formStatus.value = "loading";
  generalError.value = null;

  try {
    const tournamentData = {
      title: values.title,
      description: values.description || null,
      playersPerTournament: values.playersPerTournament,
      topicsPerParticipantMax: values.topicsPerParticipantMax,
      topicsPerParticipantMin: values.topicsPerParticipantMin,
    };

    const result = isCreate.value
      ? await tournamentService.createTournament(tournamentData)
      : await tournamentService.updateTournament(props.tournament.id, tournamentData);

    if (result.success) {
      formStatus.value = "idle";
      if (!isCreate.value) {
        // The saved values are the new baseline, so the form is clean even if the host keeps its prop.
        veeFormRef.value?.resetForm({ values: toFormValues(result.data) });
      }
      emit("saved", result.data);
    } else {
      const failure = toFormFailure(result, FORM_FIELDS);
      formStatus.value = "error";
      generalError.value = failure.banner;
      setErrors(failure.fieldErrors);
      await focusFirstInvalid(veeFormRef.value?.$el);
    }
  } catch {
    formStatus.value = "error";
    generalError.value = "An unexpected error occurred. Please try again.";
  }
};

const onInvalidSubmit = () => focusFirstInvalid(veeFormRef.value?.$el);

defineExpose({ reset, dirty });
</script>

<template>
  <VeeForm ref="veeFormRef" :initial-values="initialValues" @submit="onSubmit" @invalid-submit="onInvalidSubmit">
    <!-- Basic Information -->
    <div class="mb-6">
      <h3 class="text-lg font-semibold mb-4">Basic Information</h3>

      <!-- Title -->
      <div class="flex flex-col gap-1 mb-4">
        <div class="flex items-center gap-4">
          <label for="title" class="font-semibold w-40">Title *</label>
          <VeeField name="title" label="Title" :rules="TITLE_RULES" v-slot="slot">
            <InputText v-bind="eager(slot)" id="title" class="flex-auto" :disabled="readOnly" />
          </VeeField>
        </div>
        <ErrorMessage name="title" v-slot="{ message }">
          <Message severity="error" variant="simple">{{ message }}</Message>
        </ErrorMessage>
      </div>

      <!-- Description -->
      <div class="flex flex-col gap-1 mb-4">
        <div class="flex items-start gap-4">
          <label for="description" class="font-semibold w-40 pt-2">Description</label>
          <VeeField name="description" label="Description" :rules="DESCRIPTION_RULES" v-slot="slot">
            <Textarea v-bind="eager(slot)" id="description" class="flex-auto" rows="3" :disabled="readOnly" />
          </VeeField>
        </div>
        <ErrorMessage name="description" v-slot="{ message }">
          <Message severity="error" variant="simple">{{ message }}</Message>
        </ErrorMessage>
      </div>
    </div>

    <!-- Tournament Settings -->
    <div class="mb-6">
      <h3 class="text-lg font-semibold mb-4">Tournament Settings</h3>

      <div class="grid grid-cols-2 gap-4">
        <!-- Tournament Size -->
        <div class="flex flex-col gap-1">
          <label for="playersPerTournament" class="font-semibold text-sm">Tournament Size</label>
          <VeeField name="playersPerTournament" label="Tournament size" v-slot="slot">
            <Select
              v-bind="eager(slot)"
              labelId="playersPerTournament"
              :options="supportedSizes"
              :disabled="readOnly || supportedSizes.length === 1"
            />
          </VeeField>
          <ErrorMessage name="playersPerTournament" v-slot="{ message }">
            <Message severity="error" variant="simple">{{ message }}</Message>
          </ErrorMessage>
        </div>

        <!-- Topics Per Participant Min -->
        <div class="flex flex-col gap-1">
          <label for="topicsPerParticipantMin" class="font-semibold text-sm">Topics Per Participant Min</label>
          <VeeField
            name="topicsPerParticipantMin"
            label="Minimum topics per participant"
            :rules="TOPICS_MIN_RULES"
            v-slot="slot"
          >
            <InputNumber
              v-bind="eager(slot)"
              inputId="topicsPerParticipantMin"
              :min="topicsPerParticipantFloor"
              :max="topicsPerParticipantCeiling"
              showButtons
              :disabled="readOnly"
            />
          </VeeField>
          <Message severity="secondary" variant="simple" size="small">
            For information only. Participants aren't required to bring this many.
          </Message>
          <ErrorMessage name="topicsPerParticipantMin" v-slot="{ message }">
            <Message severity="error" variant="simple">{{ message }}</Message>
          </ErrorMessage>
        </div>

        <!-- Topics Per Participant Max -->
        <div class="flex flex-col gap-1">
          <label for="topicsPerParticipantMax" class="font-semibold text-sm">Topics Per Participant Max</label>
          <VeeField
            name="topicsPerParticipantMax"
            label="Maximum topics per participant"
            :rules="TOPICS_MAX_RULES"
            v-slot="slot"
          >
            <InputNumber
              v-bind="eager(slot)"
              inputId="topicsPerParticipantMax"
              :min="topicsPerParticipantFloor"
              :max="topicsPerParticipantCeiling"
              showButtons
              :disabled="readOnly"
            />
          </VeeField>
          <ErrorMessage name="topicsPerParticipantMax" v-slot="{ message }">
            <Message severity="error" variant="simple">{{ message }}</Message>
          </ErrorMessage>
        </div>
      </div>
    </div>

    <!-- Status Messages -->
    <div class="mb-4" v-if="formStatus === 'loading'">
      <Message severity="info">{{ isCreate ? "Creating" : "Updating" }} the tournament, please wait.</Message>
    </div>
    <div class="mb-4" v-if="formStatus === 'error' && generalError">
      <Message severity="error">{{ generalError }}</Message>
    </div>

    <!-- Action Buttons: hosts lay out their own, hide submit when readOnly, and keep it type="submit". -->
    <slot name="actions" v-bind="{ submitting: isFormProcessing, dirty, reset, readOnly }">
      <div v-if="!readOnly" class="flex justify-end gap-2">
        <Button type="submit" :label="resolvedSubmitLabel" :disabled="isFormProcessing" />
      </div>
    </slot>
  </VeeForm>
</template>
