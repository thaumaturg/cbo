<script setup>
import TournamentForm from "@/components/TournamentForm.vue";
import Button from "primevue/button";
import Dialog from "primevue/dialog";

defineProps({
  visible: {
    type: Boolean,
    required: true,
  },
});

const emit = defineEmits(["update:visible", "tournament-created"]);

const closeDialog = () => {
  emit("update:visible", false);
};

const onSaved = (tournament) => {
  emit("tournament-created", tournament);
};
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="closeDialog"
    modal
    header="Create New Tournament"
    :draggable="false"
    :style="{ width: '50rem' }"
  >
    <TournamentForm :tournament="null" @saved="onSaved">
      <template #actions="{ submitting }">
        <div class="flex justify-end gap-2">
          <Button type="button" label="Cancel" severity="secondary" @click="closeDialog" :disabled="submitting" />
          <Button type="submit" label="Create Tournament" :disabled="submitting" />
        </div>
      </template>
    </TournamentForm>
  </Dialog>
</template>

<style scoped></style>
