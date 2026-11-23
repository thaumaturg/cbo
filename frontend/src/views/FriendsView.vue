<script setup>
import { useAddFriend } from "@/composables/use-add-friend.js";
import { friendsService } from "@/services/friends-service.js";
import { focusFirstInvalid } from "@/utils/focus.js";
import { useNotify } from "@/utils/notify.js";
import { byUsername } from "@/utils/username.js";
import Button from "primevue/button";
import Column from "primevue/column";
import DataTable from "primevue/datatable";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import { useConfirm } from "primevue/useconfirm";
import { computed, onMounted, ref } from "vue";

const notify = useNotify();
const confirm = useConfirm();

const { username, fieldError, banner, isAdding, add } = useAddFriend();
const addForm = ref(null);

const friends = ref([]);
const filterText = ref("");
const isLoading = ref(false);
const loadError = ref(null);

const filteredFriends = computed(() => {
  const query = filterText.value.trim().toLowerCase();
  return friends.value.filter((friend) => friend.username.toLowerCase().includes(query));
});

const fetchFriends = async () => {
  isLoading.value = true;
  loadError.value = null;

  try {
    const result = await friendsService.getFriends();
    if (result.success) {
      friends.value = result.data;
    } else {
      loadError.value = result.message;
    }
  } catch (error) {
    loadError.value = "Failed to load your friends. Please try again.";
    console.error("Error fetching friends:", error);
  } finally {
    isLoading.value = false;
  }
};

const handleAddFriend = async () => {
  const friend = await add();

  if (friend) {
    friends.value = [...friends.value, friend].sort(byUsername);
  } else {
    // The input is disabled while adding, so focus it only once it is enabled again
    await focusFirstInvalid(addForm.value);
  }
};

const handleRemoveFriend = (friend) => {
  confirm.require({
    message: `Are you sure you want to remove "${friend.username}" from your friend list?`,
    header: "Remove Friend",
    icon: "pi pi-exclamation-triangle",
    rejectProps: { label: "Cancel", severity: "secondary", outlined: true },
    acceptProps: { label: "Remove", severity: "danger" },
    accept: async () => {
      try {
        const result = await friendsService.removeFriend(friend.accountId);

        if (result.success) {
          friends.value = friends.value.filter((f) => f.accountId !== friend.accountId);
          notify.success("Friend removed", `"${friend.username}" was removed from your friend list.`);
        } else {
          notify.error("Remove Failed", result.message);
          // A 404 means they were already removed, so show the list as the server has it
          await fetchFriends();
        }
      } catch (error) {
        notify.error("Remove Failed", "An unexpected error occurred. Please try again.");
        console.error("Error removing friend:", error);
      }
    },
  });
};

onMounted(fetchFriends);
</script>

<template>
  <main class="container mx-auto px-4 py-8 max-w-2xl">
    <h1 class="text-3xl mb-6">Friends</h1>

    <!-- Add Friend -->
    <div ref="addForm" class="flex flex-col gap-4 mb-6">
      <Message v-if="banner" severity="error">{{ banner }}</Message>

      <div class="flex gap-2">
        <div class="flex-1 flex flex-col gap-1">
          <label for="newFriendUsername" class="sr-only">Username</label>
          <InputText
            id="newFriendUsername"
            v-model="username"
            placeholder="Add a friend by exact username"
            class="w-full"
            :invalid="!!fieldError"
            :disabled="isAdding"
            @keyup.enter="handleAddFriend"
          />
          <Message v-if="fieldError" severity="error" variant="simple">{{ fieldError }}</Message>
        </div>
        <Button
          label="Add"
          severity="success"
          class="self-start"
          :loading="isAdding"
          :disabled="!username.trim() || isAdding"
          @click="handleAddFriend"
        />
      </div>
    </div>

    <Message v-if="loadError" severity="error" class="mb-4">{{ loadError }}</Message>

    <InputText
      v-if="friends.length > 0"
      v-model="filterText"
      placeholder="Filter by username"
      aria-label="Filter by username"
      class="w-64 mb-4"
    />

    <!-- Loading State -->
    <div v-if="isLoading && friends.length === 0" class="text-center py-8">
      <i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i>
      <p class="text-gray-500 dark:text-gray-400 mt-2">Loading friends...</p>
    </div>

    <DataTable
      v-else-if="friends.length > 0"
      :value="filteredFriends"
      dataKey="accountId"
      size="small"
      stripedRows
      :loading="isLoading"
    >
      <Column field="username" header="Username" />
      <Column header="Added">
        <template #body="{ data }">{{ new Date(data.addedAt).toLocaleDateString() }}</template>
      </Column>
      <Column style="width: 4rem" class="text-center">
        <template #body="{ data }">
          <Button
            icon="pi pi-trash"
            severity="danger"
            text
            rounded
            :aria-label="`Remove ${data.username}`"
            @click="handleRemoveFriend(data)"
          />
        </template>
      </Column>
      <template #empty>
        <p class="text-gray-500 dark:text-gray-400">No friends match "{{ filterText.trim() }}"</p>
      </template>
    </DataTable>

    <!-- Empty State -->
    <div v-else-if="!loadError" class="text-center py-8">
      <i class="pi pi-users text-4xl text-gray-400 mb-2 block"></i>
      <p class="text-gray-500 dark:text-gray-400">No friends yet</p>
    </div>
  </main>
</template>
