<script setup>
import { tournamentService } from "@/services/tournament-service.js";
import { useNotify } from "@/utils/notify.js";
import { rollBackWipes, stageTagSeverity, TOURNAMENT_STAGES } from "@/utils/tournament.js";
import Button from "primevue/button";
import { useConfirm } from "primevue/useconfirm";
import { computed, ref } from "vue";

const props = defineProps({
  tournament: {
    type: Object,
    required: true,
  },
});

const emit = defineEmits(["rolled-back"]);

const CREATOR_ROLL_BACK_HINT = "Only the creator can roll the tournament back.";

const notify = useNotify();
const confirm = useConfirm();

// The stage being rolled back to, while the request runs
const rollingBackTo = ref(null);

const isCreator = computed(() => props.tournament.currentUserRole === "Creator");
const currentIndex = computed(() => TOURNAMENT_STAGES.indexOf(props.tournament.currentStage));

// Only an earlier stage can be selected; the current and later ones do nothing
const canRollBackTo = (index) => isCreator.value && index < currentIndex.value;

const handleSelect = (stage, index) => {
  if (!canRollBackTo(index) || rollingBackTo.value) {
    return;
  }

  const { id, title } = props.tournament;

  confirm.require({
    message: `Roll "${title}" back to ${stage}? ${rollBackWipes(stage)}`,
    header: `Roll back to ${stage}`,
    icon: "pi pi-exclamation-triangle",
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    acceptProps: { label: "Roll back", severity: "danger" },
    accept: async () => {
      rollingBackTo.value = stage;
      try {
        const result = await tournamentService.rollBack(id, stage);

        if (result.success) {
          notify.success("Tournament rolled back", `"${title}" is back in ${stage}.`);
          emit("rolled-back", result.data);
        } else {
          notify.error("Roll Back Failed", result.message);
        }
      } catch (error) {
        console.error("Error rolling the tournament back:", error);
        notify.error("Roll Back Failed", "An unexpected error occurred. Please try again.");
      } finally {
        rollingBackTo.value = null;
      }
    },
  });
};
</script>

<template>
  <section class="mb-6">
    <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-1">Stage</h2>
    <p class="text-sm text-gray-600 dark:text-gray-400 mb-3">
      Select an earlier stage to roll the tournament back to it.
    </p>
    <!-- The span shows the tooltip, since a disabled button gets no mouse events -->
    <span v-tooltip.top="{ value: CREATOR_ROLL_BACK_HINT, disabled: isCreator }" class="inline-flex">
      <ol class="flex flex-wrap items-center gap-2">
        <li v-for="(stage, index) in TOURNAMENT_STAGES" :key="stage" class="flex items-center gap-2">
          <i v-if="index > 0" class="pi pi-angle-right text-gray-400" aria-hidden="true"></i>
          <Button
            type="button"
            :label="stage"
            size="small"
            :severity="index === currentIndex ? stageTagSeverity(stage) : 'secondary'"
            :outlined="index !== currentIndex"
            :class="{ 'pointer-events-none': index === currentIndex }"
            :aria-current="index === currentIndex ? 'step' : undefined"
            :disabled="index !== currentIndex && (!canRollBackTo(index) || rollingBackTo !== null)"
            :loading="rollingBackTo === stage"
            @click="handleSelect(stage, index)"
          />
        </li>
      </ol>
    </span>
  </section>
</template>
