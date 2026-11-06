<script setup>
import PasswordField from "@/components/PasswordField.vue";
import { eager } from "@/includes/validation.js";
import { authService } from "@/services/auth-service.js";
import { useAuthStore } from "@/stores/auth.js";
import { toFormFailure } from "@/utils/error.js";
import { focusFirstInvalid } from "@/utils/focus.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import Card from "primevue/card";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import { ref } from "vue";
import { useRouter } from "vue-router";

const authStore = useAuthStore();
const notify = useNotify();
const router = useRouter();

const PASSWORD_FIELDS = ["currentPassword", "newPassword"];

const isChangingPassword = ref(false);
const isLoggingOutEverywhere = ref(false);
const changePasswordForm = ref(null);
const changePasswordError = ref("");

const onLogoutEverywhere = async () => {
  isLoggingOutEverywhere.value = true;

  try {
    const result = await authService.logoutEverywhere();

    if (result.success) {
      authStore.logout();
      notify.success("Logged out everywhere", "You have been logged out on all devices.");
      await router.push({ name: "home" });
    } else {
      notify.error("Logout Failed", result.message);
    }
  } finally {
    isLoggingOutEverywhere.value = false;
  }
};

const onChangePasswordSubmit = async (values, { resetForm, setErrors }) => {
  isChangingPassword.value = true;
  changePasswordError.value = "";

  try {
    const result = await authService.changePassword({
      currentPassword: values.currentPassword,
      newPassword: values.newPassword,
    });

    if (result.success) {
      notify.success("Password changed", "Your password has been changed.");
      resetForm();
    } else {
      const failure = toFormFailure(result, PASSWORD_FIELDS);
      changePasswordError.value = failure.banner;
      setErrors(failure.fieldErrors);
      await focusFirstInvalid(changePasswordForm.value?.$el);
    }
  } catch (error) {
    console.error("Error changing password:", error);
    changePasswordError.value = "An unexpected error occurred. Please try again.";
  } finally {
    isChangingPassword.value = false;
  }
};

const onChangePasswordInvalidSubmit = () => focusFirstInvalid(changePasswordForm.value?.$el);
</script>

<template>
  <main class="container mx-auto px-4 py-8 max-w-2xl">
    <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-8">Profile</h1>

    <!-- Basic Information -->
    <Card class="mb-8">
      <template #title>Basic Information</template>
      <template #content>
        <div class="flex flex-col gap-4">
          <div class="flex items-center gap-4">
            <label for="profileUsername" class="font-semibold w-40">Username</label>
            <InputText id="profileUsername" :model-value="authStore.userName" class="flex-auto" disabled />
          </div>
          <div class="flex items-center gap-4">
            <label for="profileEmail" class="font-semibold w-40">Email</label>
            <InputText id="profileEmail" :model-value="authStore.userEmail" class="flex-auto" disabled />
          </div>
          <div class="flex items-center gap-4">
            <label for="profileFullName" class="font-semibold w-40">Full Name</label>
            <InputText id="profileFullName" :model-value="authStore.userFullName" class="flex-auto" disabled />
          </div>
        </div>
      </template>
    </Card>

    <!-- Change Password -->
    <Card>
      <template #title>Change Password</template>
      <template #content>
        <VeeForm
          ref="changePasswordForm"
          @submit="onChangePasswordSubmit"
          @invalid-submit="onChangePasswordInvalidSubmit"
        >
          <!-- Current Password Field -->
          <div class="flex flex-col gap-1 mb-4">
            <div class="flex items-center gap-4">
              <label for="currentPassword" class="font-semibold w-40">Current password</label>
              <VeeField name="currentPassword" label="Current password" rules="required|min:8|max:64" v-slot="slot">
                <PasswordField
                  v-bind="eager(slot)"
                  id="currentPassword"
                  class="flex-auto"
                  autocomplete="current-password"
                />
              </VeeField>
            </div>
            <ErrorMessage name="currentPassword" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <!-- New Password Field -->
          <div class="flex flex-col gap-1 mb-4">
            <div class="flex items-center gap-4">
              <label for="newPassword" class="font-semibold w-40">New password</label>
              <VeeField
                name="newPassword"
                label="New password"
                rules="required|min:8|max:64|different:@currentPassword"
                v-slot="slot"
              >
                <PasswordField v-bind="eager(slot)" id="newPassword" class="flex-auto" autocomplete="new-password" />
              </VeeField>
            </div>
            <ErrorMessage name="newPassword" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <!-- Repeat New Password Field -->
          <div class="flex flex-col gap-1 mb-8">
            <div class="flex items-center gap-4">
              <label for="repeatNewPassword" class="font-semibold w-40">Repeat new password</label>
              <VeeField
                name="repeatNewPassword"
                label="Repeat new password"
                rules="required|confirmed:@newPassword"
                v-slot="slot"
              >
                <PasswordField
                  v-bind="eager(slot)"
                  id="repeatNewPassword"
                  class="flex-auto"
                  autocomplete="new-password"
                />
              </VeeField>
            </div>
            <ErrorMessage name="repeatNewPassword" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <Message v-if="changePasswordError" severity="error" class="mb-4">{{ changePasswordError }}</Message>

          <!-- Action Buttons -->
          <div class="flex justify-end gap-2">
            <Button type="submit" label="Change Password" :disabled="isChangingPassword" />
          </div>
        </VeeForm>
      </template>
    </Card>

    <!-- Sessions -->
    <Card class="mt-8">
      <template #title>Sessions</template>
      <template #content>
        <div class="flex items-center justify-between gap-4">
          <p>Log out on every device where you are logged in, including this one.</p>
          <Button
            label="Log Out Everywhere"
            severity="danger"
            outlined
            :disabled="isLoggingOutEverywhere"
            @click="onLogoutEverywhere"
          />
        </div>
      </template>
    </Card>
  </main>
</template>
