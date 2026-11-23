<script setup>
import { useAddFriend } from "@/composables/use-add-friend.js";
import { focusFirstInvalid } from "@/utils/focus.js";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import { ref, watch } from "vue";

const props = defineProps({
  visible: {
    type: Boolean,
    required: true,
  },
});

const emit = defineEmits(["update:visible", "friend-added"]);

const { username, fieldError, banner, isAdding, add, reset } = useAddFriend();
const addForm = ref(null);

const closeDialog = () => {
  emit("update:visible", false);
};

const handleAdd = async () => {
  const friend = await add();

  if (friend) {
    emit("friend-added", friend);
    closeDialog();
  } else {
    // The input is disabled while adding, so focus it only once it is enabled again
    await focusFirstInvalid(addForm.value);
  }
};

watch(
  () => props.visible,
  (visible) => {
    if (visible) reset();
  },
);
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="closeDialog"
    modal
    header="Add a friend"
    :draggable="false"
    :closable="!isAdding"
    :closeOnEscape="!isAdding"
    :style="{ width: '28rem' }"
  >
    <div ref="addForm" class="flex flex-col gap-4">
      <Message v-if="banner" severity="error">{{ banner }}</Message>

      <div class="flex flex-col gap-1">
        <label for="addFriendUsername" class="font-semibold">Username</label>
        <InputText
          id="addFriendUsername"
          v-model="username"
          placeholder="Exact username"
          class="w-full"
          autofocus
          :invalid="!!fieldError"
          :disabled="isAdding"
          @keyup.enter="handleAdd"
        />
        <Message v-if="fieldError" severity="error" variant="simple">{{ fieldError }}</Message>
      </div>
    </div>

    <template #footer>
      <Button type="button" label="Cancel" severity="secondary" :disabled="isAdding" @click="closeDialog" />
      <Button
        type="button"
        label="Add"
        severity="success"
        :loading="isAdding"
        :disabled="!username.trim() || isAdding"
        @click="handleAdd"
      />
    </template>
  </Dialog>
</template>
