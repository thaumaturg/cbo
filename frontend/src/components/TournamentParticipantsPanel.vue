<script setup>
import { tournamentParticipantsService } from "@/services/tournament-participants-service.js";
import { toFormFailure } from "@/utils/error.js";
import { focusFirstInvalid } from "@/utils/focus.js";
import { useNotify } from "@/utils/notify.js";
import { roleTagSeverity } from "@/utils/tournament.js";
import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import Select from "primevue/select";
import Tag from "primevue/tag";
import { useConfirm } from "primevue/useconfirm";
import { computed, ref, watch } from "vue";

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

const notify = useNotify();
const confirm = useConfirm();

const newUsername = ref("");
const newRole = ref("Player");
const roleOptions = [
  { label: "Organizer", value: "Organizer" },
  { label: "Player", value: "Player" },
];

const participants = ref([]);
const isLoading = ref(false);
const isAdding = ref(false);
const isSavingSeeding = ref(false);
const isChangingRole = ref(false);
// Select keeps the picked role even when the change fails, so its key changes to show the saved role again
const roleSelectsVersion = ref(0);
const loadError = ref(null);
const addError = ref(null);
const addFieldErrors = ref({});
const addForm = ref(null);

const roleOrder = {
  Creator: 0,
  Organizer: 1,
};

const staff = computed(() => {
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

const canReorderPlayers = computed(() => !props.readOnly && players.value.length > 1);
const isRoleSelectDisabled = computed(() => isChangingRole.value || isSavingSeeding.value);

const organizerCount = computed(() => participants.value.filter((p) => p.role === "Organizer").length);

const topicsLabel = (participant) => `${participant.topicsCount} / ${props.tournament.topicsPerParticipantMax}`;

const resetAddForm = () => {
  newUsername.value = "";
  newRole.value = "Player";
  addError.value = null;
  addFieldErrors.value = {};
};

const fetchParticipants = async () => {
  isLoading.value = true;
  loadError.value = null;

  try {
    const result = await tournamentParticipantsService.getAllParticipants(props.tournament.id);
    if (result.success) {
      participants.value = result.data;
    } else {
      loadError.value = result.message;
    }
  } catch (error) {
    loadError.value = "Failed to load participants. Please try again.";
    console.error("Error fetching participants:", error);
  } finally {
    isLoading.value = false;
  }
};

const handleAddParticipant = async () => {
  if (!newUsername.value.trim()) {
    addError.value = null;
    addFieldErrors.value = { username: "Username is required." };
    return;
  }

  isAdding.value = true;
  addError.value = null;
  addFieldErrors.value = {};

  try {
    const result = await tournamentParticipantsService.createParticipant(props.tournament.id, {
      username: newUsername.value.trim(),
      role: newRole.value,
    });

    if (result.success) {
      participants.value.push(result.data);
      resetAddForm();
    } else {
      const failure = toFormFailure(result, ["username", "role"]);
      addError.value = failure.banner || null;
      addFieldErrors.value = failure.fieldErrors;
    }
  } catch (error) {
    addError.value = "An unexpected error occurred. Please try again.";
    console.error("Error adding participant:", error);
  } finally {
    isAdding.value = false;
  }

  // The inputs are disabled while adding, so focus them only once they are enabled again
  await focusFirstInvalid(addForm.value);
};

const handleRemoveParticipant = (participant) => {
  if (participant.role === "Creator") return;

  confirm.require({
    message: `Are you sure you want to remove "${participant.username}" from this tournament?`,
    header: "Remove Participant",
    icon: "pi pi-exclamation-triangle",
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    acceptProps: { label: "Remove", severity: "danger" },
    accept: async () => {
      try {
        const result = await tournamentParticipantsService.deleteParticipant(props.tournament.id, participant.id);

        if (result.success) {
          notify.success("Participant removed", `"${participant.username}" was removed from the tournament.`);
          await fetchParticipants();
        } else {
          notify.error("Remove Failed", result.message);
        }
      } catch (error) {
        notify.error("Remove Failed", "An unexpected error occurred. Please try again.");
        console.error("Error removing participant:", error);
      }
    },
  });
};

const handleRoleChange = async (participant, role) => {
  if (role === participant.role || isRoleSelectDisabled.value) return;

  isChangingRole.value = true;

  try {
    const result = await tournamentParticipantsService.updateParticipant(props.tournament.id, participant.id, { role });

    if (result.success) {
      const article = role === "Organizer" ? "an" : "a";
      notify.success("Role changed", `"${participant.username}" was made ${article} ${role}.`);
    } else {
      notify.error("Role Change Failed", result.message);
    }
  } catch (error) {
    notify.error("Role Change Failed", "An unexpected error occurred. Please try again.");
    console.error("Error changing participant role:", error);
  } finally {
    // Seeds follow a role change, and a failed change shows the old role again
    await fetchParticipants();
    roleSelectsVersion.value++;
    isChangingRole.value = false;
  }
};

const handleRowReorder = async (event) => {
  if (isSavingSeeding.value || isChangingRole.value) return;

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
      notify.error("Seeding Update Failed", result.message);
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

// Starting the tournament changes what can be managed, so reload with the stage too
watch(() => [props.tournament.id, props.tournament.currentStage], fetchParticipants, { immediate: true });

watch(
  () => props.readOnly,
  (readOnly) => {
    if (readOnly) resetAddForm();
  },
);

watch([newUsername, newRole], () => {
  addFieldErrors.value = {};
});
</script>

<template>
  <div class="flex flex-col gap-6">
    <!-- Add Participant -->
    <div v-if="!readOnly" ref="addForm" class="flex flex-col gap-4">
      <Message v-if="addError" severity="error">{{ addError }}</Message>

      <div class="flex gap-2">
        <div class="flex-1 flex flex-col gap-1">
          <label for="newParticipantUsername" class="sr-only">Username</label>
          <InputText
            id="newParticipantUsername"
            v-model="newUsername"
            placeholder="Username"
            class="w-full"
            :invalid="!!addFieldErrors.username"
            :disabled="isAdding"
            @keyup.enter="handleAddParticipant"
          />
          <Message v-if="addFieldErrors.username" severity="error" variant="simple">
            {{ addFieldErrors.username }}
          </Message>
        </div>
        <div class="w-40 flex flex-col gap-1">
          <Select
            v-model="newRole"
            :options="roleOptions"
            optionLabel="label"
            optionValue="value"
            aria-label="Role"
            class="w-full"
            :invalid="!!addFieldErrors.role"
            :disabled="isAdding"
          />
          <Message v-if="addFieldErrors.role" severity="error" variant="simple">
            {{ addFieldErrors.role }}
          </Message>
        </div>
        <Button
          label="Add"
          severity="success"
          class="self-start"
          :loading="isAdding"
          :disabled="!newUsername.trim() || isAdding"
          @click="handleAddParticipant"
        />
      </div>
    </div>

    <Message v-if="loadError" severity="error">{{ loadError }}</Message>

    <h3 class="text-lg font-semibold">
      <template v-if="loadError || (isLoading && participants.length === 0)">Participants</template>
      <template v-else>
        Participants: {{ organizerCount }} Organizers, {{ players.length }} of
        {{ tournament.playersPerTournament }} Players
      </template>
    </h3>

    <!-- Loading State -->
    <div v-if="isLoading && participants.length === 0" class="text-center py-8">
      <i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i>
      <p class="text-gray-500 dark:text-gray-400 mt-2">Loading participants...</p>
    </div>

    <template v-else-if="participants.length > 0">
      <!-- Staff -->
      <div v-if="staff.length > 0">
        <h4 class="font-semibold text-gray-900 dark:text-gray-100 mb-2">Staff</h4>
        <DataTable :value="staff" dataKey="id" size="small" stripedRows :loading="isLoading || isChangingRole">
          <Column field="username" header="Username" />
          <Column header="Role">
            <template #body="{ data }">
              <Select
                v-if="!readOnly && data.role !== 'Creator'"
                :key="`${data.id}-${roleSelectsVersion}`"
                :modelValue="data.role"
                :options="roleOptions"
                optionLabel="label"
                optionValue="value"
                :aria-label="`Role of ${data.username}`"
                size="small"
                class="w-36"
                :disabled="isRoleSelectDisabled"
                @update:modelValue="(role) => handleRoleChange(data, role)"
              />
              <Tag v-else :value="data.role" :severity="roleTagSeverity(data.role)" />
            </template>
          </Column>
          <Column header="Topics">
            <template #body="{ data }">{{ topicsLabel(data) }}</template>
          </Column>
          <Column v-if="!readOnly" style="width: 4rem" class="text-center">
            <template #body="{ data }">
              <Button
                v-if="data.role !== 'Creator'"
                icon="pi pi-trash"
                severity="danger"
                text
                rounded
                aria-label="Remove participant"
                @click="handleRemoveParticipant(data)"
              />
            </template>
          </Column>
        </DataTable>
      </div>

      <!-- Players (Seeding) -->
      <div v-if="players.length > 0">
        <div class="flex items-center justify-between mb-2">
          <h4 class="font-semibold text-gray-900 dark:text-gray-100">Players</h4>
          <span v-if="canReorderPlayers" class="text-xs text-gray-500 dark:text-gray-400">
            Drag rows by the handle to change bracket seeding.
          </span>
        </div>

        <DataTable
          :value="players"
          dataKey="id"
          size="small"
          stripedRows
          :loading="isLoading || isSavingSeeding || isChangingRole"
          @row-reorder="handleRowReorder"
        >
          <Column v-if="canReorderPlayers" rowReorder style="width: 3rem" />
          <Column field="seed" header="Seed" style="width: 4rem" />
          <Column field="username" header="Player" />
          <Column v-if="!readOnly" header="Role">
            <template #body="{ data }">
              <Select
                :key="`${data.id}-${roleSelectsVersion}`"
                :modelValue="data.role"
                :options="roleOptions"
                optionLabel="label"
                optionValue="value"
                :aria-label="`Role of ${data.username}`"
                size="small"
                class="w-36"
                :disabled="isRoleSelectDisabled"
                @update:modelValue="(role) => handleRoleChange(data, role)"
              />
            </template>
          </Column>
          <Column header="Topics">
            <template #body="{ data }">{{ topicsLabel(data) }}</template>
          </Column>
          <Column v-if="!readOnly" style="width: 4rem" class="text-center">
            <template #body="{ data }">
              <Button
                icon="pi pi-trash"
                severity="danger"
                text
                rounded
                aria-label="Remove participant"
                @click="handleRemoveParticipant(data)"
              />
            </template>
          </Column>
        </DataTable>
      </div>
    </template>

    <!-- Empty State -->
    <div v-else-if="!loadError" class="text-center py-8">
      <i class="pi pi-users text-4xl text-gray-400 mb-2 block"></i>
      <p class="text-gray-500 dark:text-gray-400">No participants yet</p>
    </div>
  </div>
</template>
