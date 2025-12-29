<script setup>
import { ref, computed, watch } from "vue";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import Button from "primevue/button";
import Message from "primevue/message";
import { topicAuthorsService } from "@/services/topic-authors-service.js";

const props = defineProps({
  visible: {
    type: Boolean,
    required: true,
  },
  topic: {
    type: Object,
    required: false,
    default: null,
  },
});

const emit = defineEmits(["update:visible"]);

const newUsername = ref("");

const authors = ref([]);
const isLoadingAuthors = ref(false);
const isAddingAuthor = ref(false);
const addError = ref(null);

const roleOrder = {
  Owner: 0,
  Author: 1,
};

const sortedAuthors = computed(() => {
  return [...authors.value].sort((a, b) => {
    // Sort by owner first, then alphabetically
    const aRole = a.isOwner ? "Owner" : "Author";
    const bRole = b.isOwner ? "Owner" : "Author";
    const roleComparison = roleOrder[aRole] - roleOrder[bRole];
    if (roleComparison !== 0) return roleComparison;
    return a.username.localeCompare(b.username);
  });
});

const closeDialog = () => {
  emit("update:visible", false);
  resetForm();
};

const resetForm = () => {
  newUsername.value = "";
  addError.value = null;
};

const fetchAuthors = async () => {
  if (!props.topic) return;

  isLoadingAuthors.value = true;
  addError.value = null;

  try {
    const result = await topicAuthorsService.getAllAuthors(props.topic.id);
    if (result.success) {
      authors.value = result.data;
    } else {
      addError.value = result.error;
    }
  } catch (error) {
    addError.value = "Failed to load authors. Please try again.";
    console.error("Error fetching authors:", error);
  } finally {
    isLoadingAuthors.value = false;
  }
};

const handleAddAuthor = async () => {
  if (!props.topic) return;
  if (!newUsername.value.trim()) {
    addError.value = "Username is required.";
    return;
  }

  isAddingAuthor.value = true;
  addError.value = null;

  try {
    const result = await topicAuthorsService.createAuthor(props.topic.id, {
      username: newUsername.value.trim(),
    });

    if (result.success) {
      authors.value.push(result.data);
      resetForm();
    } else {
      if (typeof result.error === "string") {
        addError.value = result.error;
      } else if (result.error?.title) {
        addError.value = result.error.title;
      } else {
        addError.value = "Failed to add author. Please try again.";
      }
    }
  } catch (error) {
    addError.value = "An unexpected error occurred. Please try again.";
    console.error("Error adding author:", error);
  } finally {
    isAddingAuthor.value = false;
  }
};

const handleDeleteAuthor = async (author) => {
  if (!props.topic) return;

  if (author.isOwner) {
    return;
  }

  if (!confirm(`Are you sure you want to remove "${author.username}" from this topic?`)) {
    return;
  }

  addError.value = null;

  try {
    const result = await topicAuthorsService.deleteAuthor(props.topic.id, author.id);

    if (result.success) {
      const index = authors.value.findIndex((a) => a.id === author.id);
      if (index > -1) {
        authors.value.splice(index, 1);
      }
    } else {
      if (typeof result.error === "string") {
        addError.value = result.error;
      } else if (result.error?.title) {
        addError.value = result.error.title;
      } else {
        addError.value = "Failed to remove author. Please try again.";
      }
    }
  } catch (error) {
    addError.value = "An unexpected error occurred. Please try again.";
    console.error("Error deleting author:", error);
  }
};

const getRoleBadges = (author) => {
  const badges = [];
  if (author.isOwner) {
    badges.push({ label: "Owner", class: "bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200" });
  }
  if (author.isAuthor) {
    badges.push({ label: "Author", class: "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200" });
  }
  return badges;
};

watch(
  () => props.visible,
  (newValue) => {
    if (newValue && props.topic) {
      fetchAuthors();
    } else {
      resetForm();
    }
  }
);
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="closeDialog"
    modal
    :header="`Authors - ${topic?.title || 'Topic'}`"
    :draggable="false"
    :style="{ width: '40rem' }"
  >
    <!-- Add Author Section -->
    <div class="mb-6 p-4 border border-gray-200 dark:border-gray-700 rounded-lg">
      <h3 class="text-lg font-semibold mb-4">Add Author</h3>

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
            :disabled="isAddingAuthor"
            @keyup.enter="handleAddAuthor"
          />
        </div>
        <Button
          label="Add"
          severity="success"
          @click="handleAddAuthor"
          :loading="isAddingAuthor"
          :disabled="!newUsername.trim() || isAddingAuthor"
        />
      </div>
    </div>

    <!-- Authors List -->
    <div>
      <h3 class="text-lg font-semibold mb-4">Author List</h3>

      <!-- Loading State -->
      <div v-if="isLoadingAuthors" class="text-center py-8">
        <i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i>
        <p class="text-gray-500 dark:text-gray-400 mt-2">Loading authors...</p>
      </div>

      <!-- Authors List -->
      <div v-else-if="sortedAuthors.length > 0" class="space-y-2">
        <div
          v-for="author in sortedAuthors"
          :key="author.id"
          class="flex items-center justify-between p-3 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
        >
          <div class="flex items-center gap-3 flex-1">
            <span class="font-medium text-gray-900 dark:text-gray-100">
              {{ author.username }}
            </span>
            <span
              v-for="badge in getRoleBadges(author)"
              :key="badge.label"
              class="px-2 py-1 text-xs font-semibold rounded-full"
              :class="badge.class"
            >
              {{ badge.label }}
            </span>
          </div>

          <!-- Delete Button (Only for non-Owner authors) -->
          <Button
            v-if="!author.isOwner"
            icon="pi pi-trash"
            severity="danger"
            outlined
            rounded
            size="small"
            @click="handleDeleteAuthor(author)"
            v-tooltip.bottom="'Remove Author'"
            class="p-2"
            aria-label="Remove Author"
          />
        </div>
      </div>

      <!-- Empty State -->
      <div v-else class="text-center py-8">
        <i class="pi pi-users text-4xl text-gray-400 mb-2 block"></i>
        <p class="text-gray-500 dark:text-gray-400">No authors yet</p>
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
