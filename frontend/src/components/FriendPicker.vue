<script setup>
import AddFriendDialog from "@/components/AddFriendDialog.vue";
import { friendsService } from "@/services/friends-service.js";
import { byUsername, sameUsername } from "@/utils/username.js";
import AutoComplete from "primevue/autocomplete";
import Button from "primevue/button";
import Message from "primevue/message";
import { computed, onMounted, ref, watch } from "vue";

// Without v-model the box clears after each pick, for parents that add a row on `pick`.
// With v-model (null when nothing is picked) the box keeps the pick until the parent clears it.
const props = defineProps({
  modelValue: {
    type: Object,
    default: undefined,
  },
  // People already added, whom the picker doesn't suggest; matched ignoring case
  excludeUsernames: {
    type: Array,
    default: () => [],
  },
  disabled: {
    type: Boolean,
    default: false,
  },
  invalid: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue", "pick"]);

const friends = ref([]);
// AutoComplete holds the typed text, or the friend object once one is picked
const boxValue = ref(props.modelValue ?? "");
const filteredFriends = ref([]);
const isLoading = ref(false);
const loadError = ref(null);
const isAddDialogVisible = ref(false);

const hasModel = computed(() => props.modelValue !== undefined);
const isFriend = (value) => value !== null && typeof value === "object";
const isExcluded = (friend) => props.excludeUsernames.some((username) => sameUsername(username, friend.username));

const availableFriends = computed(() => friends.value.filter((friend) => !isExcluded(friend)));

const emptyMessage = computed(() => {
  if (isLoading.value) return "Loading your friends…";
  if (friends.value.length === 0) return "No friends yet";
  if (availableFriends.value.length === 0) return "All your friends are already added";
  return "No matching friends found";
});

// Loads every friend once and filters them here; the list is small and isn't paged (ADR-0009)
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

const searchFriends = (event) => {
  const query = event.query.toLowerCase();
  filteredFriends.value = availableFriends.value.filter((friend) => friend.username.toLowerCase().includes(query));
};

const pick = (friend) => {
  if (hasModel.value) {
    emit("update:modelValue", friend);
  } else {
    boxValue.value = "";
  }
  emit("pick", friend);
};

const pickFriend = (event) => {
  if (isFriend(event.value)) pick(event.value);
};

// Someone already added isn't picked again, though they stay on the friend list
const handleFriendAdded = (friend) => {
  friends.value = [...friends.value, friend].sort(byUsername);
  if (!isExcluded(friend)) pick(friend);
};

watch(
  () => props.modelValue,
  (friend) => {
    if (hasModel.value) boxValue.value = friend ?? "";
  },
);

// Typing over a pick, or clearing the box, unpicks the friend
watch(boxValue, (value) => {
  if (hasModel.value && !isFriend(value) && props.modelValue !== null) emit("update:modelValue", null);
});

onMounted(fetchFriends);
</script>

<template>
  <div class="flex flex-col gap-1">
    <div class="flex items-center gap-2">
      <AutoComplete
        v-model="boxValue"
        :suggestions="filteredFriends"
        optionLabel="username"
        placeholder="Pick from your friends…"
        aria-label="Pick from your friends"
        class="flex-1"
        :invalid="invalid"
        :disabled="disabled"
        dropdown
        forceSelection
        @complete="searchFriends"
        @option-select="pickFriend"
      >
        <template #empty>
          <div class="p-2 text-gray-500 dark:text-gray-400">{{ emptyMessage }}</div>
        </template>
      </AutoComplete>
      <Button
        type="button"
        label="Add a friend"
        icon="pi pi-user-plus"
        link
        class="shrink-0"
        :disabled="disabled"
        @click="isAddDialogVisible = true"
      />
    </div>
    <Message v-if="loadError" severity="error" variant="simple" size="small">{{ loadError }}</Message>

    <AddFriendDialog v-model:visible="isAddDialogVisible" @friend-added="handleFriendAdded" />
  </div>
</template>
