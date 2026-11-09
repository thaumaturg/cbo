<script setup>
import TournamentForm from "@/components/TournamentForm.vue";
import { tournamentService } from "@/services/tournament-service.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import { useConfirm } from "primevue/useconfirm";
import { computed, nextTick, ref } from "vue";
import { useRouter } from "vue-router";

const props = defineProps({
  tournament: {
    type: Object,
    required: true,
  },
  readOnly: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["updated", "update:dirty"]);

const CREATOR_DELETE_HINT = "Only the creator can delete this tournament.";

const router = useRouter();
const notify = useNotify();
const confirm = useConfirm();

const formRef = ref(null);
const isDeleting = ref(false);

// The Creator can delete in every stage, even when the settings are read-only
const isCreator = computed(() => props.tournament.currentUserRole === "Creator");

const reset = () => formRef.value?.reset();

const onSaved = (tournament) => {
  emit("updated", tournament);
  notify.success("Tournament updated", `"${tournament.title}" was updated.`);
};

const handleDelete = () => {
  const { id, title } = props.tournament;

  confirm.require({
    message: `Are you sure you want to delete "${title}"?`,
    header: "Delete Tournament",
    icon: "pi pi-exclamation-triangle",
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    acceptProps: { label: "Delete", severity: "danger" },
    accept: async () => {
      isDeleting.value = true;
      const result = await tournamentService.deleteTournament(id);
      isDeleting.value = false;

      if (result.success) {
        // Unsaved edits of a deleted tournament aren't worth a leave prompt. The view hears the
        // form is clean only after the next render, so wait for it before leaving.
        reset();
        await nextTick();
        notify.success("Tournament deleted", `"${title}" was deleted.`);
        router.push({ name: "tournaments" });
      } else {
        notify.error("Delete Failed", result.message);
      }
    },
  });
};

defineExpose({ reset });
</script>

<template>
  <TournamentForm
    ref="formRef"
    :tournament="tournament"
    :read-only="readOnly"
    submit-label="Save"
    @saved="onSaved"
    @update:dirty="emit('update:dirty', $event)"
  >
    <template #actions="{ submitting, dirty }">
      <div class="flex items-center gap-2">
        <!-- The span shows the tooltip, since a disabled button gets no mouse events -->
        <span v-tooltip.top="{ value: CREATOR_DELETE_HINT, disabled: isCreator }">
          <Button
            type="button"
            label="Delete Tournament"
            icon="pi pi-trash"
            severity="danger"
            outlined
            :disabled="!isCreator || submitting || isDeleting"
            :loading="isDeleting"
            @click="handleDelete"
          />
        </span>
        <div v-if="!readOnly" class="ml-auto flex gap-2">
          <Button
            type="button"
            label="Reset"
            icon="pi pi-refresh"
            severity="secondary"
            outlined
            :disabled="!dirty || submitting || isDeleting"
            @click="reset"
          />
          <Button
            type="submit"
            label="Save"
            icon="pi pi-check"
            :disabled="!dirty || submitting || isDeleting"
            :loading="submitting"
          />
        </div>
      </div>
    </template>
  </TournamentForm>
</template>
