<script setup>
import TournamentForm from "@/components/TournamentForm.vue";
import Button from "primevue/button";
import Dialog from "primevue/dialog";

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

const closeDialog = () => {
  emit("update:visible", false);
};

const onSaved = (tournament) => {
  emit(props.mode === "create" ? "tournament-created" : "tournament-updated", tournament);
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
    <TournamentForm :tournament="mode === 'edit' ? tournament : null" @saved="onSaved">
      <template #actions="{ submitting }">
        <div class="flex justify-end gap-2">
          <Button type="button" label="Cancel" severity="secondary" @click="closeDialog" :disabled="submitting" />
          <Button
            type="submit"
            :label="mode === 'create' ? 'Create Tournament' : 'Save Changes'"
            :disabled="submitting"
          />
        </div>
      </template>
    </TournamentForm>
  </Dialog>
</template>

<style scoped></style>
