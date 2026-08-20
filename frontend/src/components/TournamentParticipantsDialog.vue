<script setup>
import { tournamentParticipantsService } from "@/services/tournament-participants-service.js";
import { extractErrorMessage } from "@/utils/error.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import Select from "primevue/select";
import { useConfirm } from "primevue/useconfirm";
import { computed, ref, watch } from "vue";

const props = defineProps({
  visible: {
    type: Boolean,
    required: true,
  },
  tournament: {
    type: Object,
    required: false,
    default: null,
  },
});

const emit = defineEmits(["update:visible"]);

const notify = useNotify();
const confirm = useConfirm();

const newUsername = ref("");
const newRole = ref("Player");
const roleOptions = [
  { label: "Organizer", value: "Organizer" },
  { label: "Player", value: "Player" },
];

const participants = ref([]);
const isLoadingParticipants = ref(false);
const isAddingParticipant = ref(false);
const isSavingSeeding = ref(false);
const addError = ref(null);

const roleOrder = {
  Creator: 0,
  Organizer: 1,
};

const isCreator = computed(() => {
  return props.tournament?.currentUserRole === "Creator";
});

const staffParticipants = computed(() => {
  return participants.value
    .filter((p) => p.role !== "Player")
    .sort((a, b) => {
      const roleComparison = roleOrder[a.role] - roleOrder[b.role];
      if (roleComparison !== 0) return roleComparison;
      return a.username.localeCompare(b.username);
    });
});

const players = computed(() => {
  return participants.value
    .filter((p) => p.role === "Player")
    .sort((a, b) => {
      if (a.seed !== b.seed) return (a.seed ?? Infinity) - (b.seed ?? Infinity);
      return a.username.localeCompare(b.username);
    });
});

const canReorderPlayers = computed(() => {
  return isCreator.value && props.tournament?.currentStage === "Preparations" && players.value.length > 1;
});

const organizerCount = computed(() => {
  return participants.value.filter((p) => p.role === "Organizer").length;
});

const playerCount = computed(() => {
  return participants.value.filter((p) => p.role === "Player").length;
});

const requiredPlayers = computed(() => {
  return props.tournament?.playersPerTournament || 0;
});

const requiredTopics = computed(() => {
  return props.tournament?.topicsPerParticipantMin || 0;
});

const closeDialog = () => {
  emit("update:visible", false);
  resetForm();
};

const resetForm = () => {
  newUsername.value = "";
  newRole.value = "Player";
  addError.value = null;
};

const fetchParticipants = async () => {
  if (!props.tournament) return;

  isLoadingParticipants.value = true;
  addError.value = null;

  try {
    const result = await tournamentParticipantsService.getAllParticipants(props.tournament.id);
    if (result.success) {
      participants.value = result.data;
    } else {
      addError.value = result.error;
    }
  } catch (error) {
    addError.value = "Failed to load participants. Please try again.";
    console.error("Error fetching participants:", error);
  } finally {
    isLoadingParticipants.value = false;
  }
};

const handleAddParticipant = async () => {
  if (!props.tournament) return;
  if (!newUsername.value.trim()) {
    addError.value = "Username is required.";
    return;
  }

  isAddingParticipant.value = true;
  addError.value = null;

  try {
    const result = await tournamentParticipantsService.createParticipant(props.tournament.id, {
      username: newUsername.value.trim(),
      role: newRole.value,
    });

    if (result.success) {
      participants.value.push(result.data);
      resetForm();
    } else {
      addError.value = extractErrorMessage(result.error, "Failed to add participant. Please try again.");
    }
  } catch (error) {
    addError.value = "An unexpected error occurred. Please try again.";
    console.error("Error adding participant:", error);
  } finally {
    isAddingParticipant.value = false;
  }
};

const handleDeleteParticipant = (participant) => {
  if (!props.tournament) return;

  if (participant.role === "Creator") {
    return;
  }

  confirm.require({
    message: `Are you sure you want to remove "${participant.username}" from this tournament?`,
    header: "Remove Participant",
    icon: "pi pi-exclamation-triangle",
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    acceptProps: { label: "Remove", severity: "danger" },
    accept: async () => {
      addError.value = null;

      try {
        const result = await tournamentParticipantsService.deleteParticipant(props.tournament.id, participant.id);

        if (result.success) {
          notify.success("Participant Removed", `"${participant.username}" removed from tournament`);
          await fetchParticipants();
        } else {
          notify.error(
            "Remove Failed",
            extractErrorMessage(result.error, "Failed to remove participant. Please try again."),
          );
        }
      } catch (error) {
        notify.error("Remove Failed", "An unexpected error occurred. Please try again.");
        console.error("Error deleting participant:", error);
      }
    },
  });
};

const handleRowReorder = async (event) => {
  if (!props.tournament || isSavingSeeding.value) return;

  const orderedIds = event.value.map((p) => p.id);

  // Optimistically apply the new order; participants share object refs with event.value
  event.value.forEach((player, index) => {
    player.seed = index + 1;
  });

  isSavingSeeding.value = true;

  try {
    const result = await tournamentParticipantsService.updateSeeding(props.tournament.id, orderedIds);

    if (result.success) {
      const playersById = new Map(result.data.map((p) => [p.id, p]));
      participants.value = participants.value.map((p) => playersById.get(p.id) ?? p);
    } else {
      notify.error(
        "Seeding Update Failed",
        extractErrorMessage(result.error, "Failed to update seeding. Please try again."),
      );
      await fetchParticipants();
    }
  } catch (error) {
    notify.error("Seeding Update Failed", "An unexpected error occurred. Please try again.");
    console.error("Error updating seeding:", error);
    await fetchParticipants();
  } finally {
    isSavingSeeding.value = false;
  }
};

const getRoleBadgeClass = (role) => {
  switch (role) {
    case "Creator":
      return "bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200";
    case "Organizer":
      return "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200";
    case "Player":
      return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200";
    default:
      return "bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200";
  }
};

// Watch for dialog visibility changes to fetch participants
watch(
  () => props.visible,
  (newValue) => {
    if (newValue && props.tournament) {
      fetchParticipants();
    } else {
      resetForm();
    }
  },
);
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="closeDialog"
    modal
    :header="`Participants - ${tournament?.title || 'Tournament'}`"
    :draggable="false"
    :style="{ width: '40rem' }"
  >
    <!-- Add Participant Section (Only for Creator) -->
    <div v-if="isCreator" class="mb-6 p-4 border border-gray-200 dark:border-gray-700 rounded-lg">
      <h3 class="text-lg font-semibold mb-4">Add Participant</h3>

      <!-- Error Message -->
      <div v-if="addError" class="mb-4">
        <Message severity="error">{{ addError }}</Message>
      </div>

      <div class="flex gap-2">
        <div class="flex-1">
          <InputText
            v-model="newUsername"
            placeholder="Username"
            class="w-full"
            :disabled="isAddingParticipant"
            @keyup.enter="handleAddParticipant"
          />
        </div>
        <div class="w-40">
          <Select
            v-model="newRole"
            :options="roleOptions"
            optionLabel="label"
            optionValue="value"
            class="w-full"
            :disabled="isAddingParticipant"
          />
        </div>
        <Button
          label="Add"
          severity="success"
          @click="handleAddParticipant"
          :loading="isAddingParticipant"
          :disabled="!newUsername.trim() || isAddingParticipant"
        />
      </div>
    </div>

    <!-- Error Message for non-creators -->
    <div v-if="!isCreator && addError" class="mb-4">
      <Message severity="error">{{ addError }}</Message>
    </div>

    <!-- Participants List -->
    <div>
      <h3 v-if="isLoadingParticipants" class="text-lg font-semibold mb-4">Participant List</h3>
      <h3 v-else class="text-lg font-semibold mb-4">
        Participant List: {{ organizerCount }} Organizers, {{ playerCount }} out of {{ requiredPlayers }} Players
      </h3>

      <!-- Loading State -->
      <div v-if="isLoadingParticipants" class="text-center py-8">
        <i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i>
        <p class="text-gray-500 dark:text-gray-400 mt-2">Loading participants...</p>
      </div>

      <!-- Participants List -->
      <div v-else-if="participants.length > 0">
        <!-- Creator & Organizers -->
        <div v-if="staffParticipants.length > 0" class="space-y-2 mb-6">
          <div
            v-for="participant in staffParticipants"
            :key="participant.id"
            class="flex items-center justify-between p-3 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
          >
            <div class="flex items-center gap-3 flex-1">
              <span class="font-medium text-gray-900 dark:text-gray-100">
                {{ participant.username }}
              </span>
              <span class="px-2 py-1 text-xs font-semibold rounded-full" :class="getRoleBadgeClass(participant.role)">
                {{ participant.role }}
              </span>
              <span class="text-sm text-gray-500 dark:text-gray-400"> Topics: {{ participant.topicsCount }} </span>
            </div>

            <!-- Delete Button (Only for non-Creator participants and only if current user is Creator) -->
            <Button
              v-if="isCreator && participant.role !== 'Creator'"
              icon="pi pi-trash"
              severity="danger"
              outlined
              rounded
              size="small"
              @click="handleDeleteParticipant(participant)"
              v-tooltip.bottom="'Remove Participant'"
              class="p-2"
              aria-label="Remove Participant"
            />
          </div>
        </div>

        <!-- Players (Seeding) -->
        <div v-if="players.length > 0">
          <div class="flex items-center justify-between mb-2">
            <h4 class="font-semibold text-gray-900 dark:text-gray-100">Players</h4>
            <span v-if="canReorderPlayers" class="text-xs text-gray-500 dark:text-gray-400">
              Drag rows to change bracket seeding
            </span>
          </div>

          <DataTable
            :value="players"
            dataKey="id"
            size="small"
            stripedRows
            responsiveLayout="scroll"
            :loading="isSavingSeeding"
            @row-reorder="handleRowReorder"
          >
            <Column v-if="canReorderPlayers" rowReorder style="width: 3rem" />

            <Column header="Seed" style="width: 4rem" class="text-center">
              <template #body="{ data }">
                <span class="font-semibold text-gray-700 dark:text-gray-300">{{ data.seed }}</span>
              </template>
            </Column>

            <Column field="username" header="Player" style="min-width: 150px">
              <template #body="{ data }">
                <span class="font-medium text-gray-800 dark:text-gray-200">{{ data.username }}</span>
              </template>
            </Column>

            <Column header="Topics" style="width: 7rem" class="text-center">
              <template #body="{ data }">
                <span class="text-sm text-gray-500 dark:text-gray-400">
                  {{ data.topicsCount }} of {{ requiredTopics }}
                </span>
              </template>
            </Column>

            <Column v-if="isCreator" style="width: 4rem" class="text-center">
              <template #body="{ data }">
                <Button
                  icon="pi pi-trash"
                  severity="danger"
                  outlined
                  rounded
                  size="small"
                  @click="handleDeleteParticipant(data)"
                  v-tooltip.bottom="'Remove Participant'"
                  class="p-2"
                  aria-label="Remove Participant"
                />
              </template>
            </Column>
          </DataTable>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else class="text-center py-8">
        <i class="pi pi-users text-4xl text-gray-400 mb-2 block"></i>
        <p class="text-gray-500 dark:text-gray-400">No participants yet</p>
      </div>
    </div>

    <!-- Close Button -->
    <template #footer>
      <div class="flex justify-end">
        <Button label="Close" severity="secondary" @click="closeDialog" />
      </div>
    </template>
  </Dialog>
</template>

<style scoped></style>
