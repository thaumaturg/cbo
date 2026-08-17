<script setup>
import { authService } from "@/services/auth-service.js";
import { useAuthStore } from "@/stores/auth.js";
import { useNotify } from "@/utils/notify.js";
import Button from "primevue/button";
import Card from "primevue/card";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import Password from "primevue/password";
import Toast from "primevue/toast";
import { ref } from "vue";

const authStore = useAuthStore();
const notify = useNotify();

const isChangingPassword = ref(false);

const onChangePasswordSubmit = async (values, { resetForm }) => {
  isChangingPassword.value = true;

  try {
    const result = await authService.changePassword({
      currentPassword: values.currentPassword,
      newPassword: values.newPassword,
    });

    if (result.success) {
      notify.success("Password Changed", "Your password has been updated.");
      resetForm();
    } else {
      notify.error("Password Change Failed", result.error);
    }
  } catch (error) {
    console.error("Error changing password:", error);
    notify.error("Password Change Failed", "An unexpected error occurred. Please try again.");
  } finally {
    isChangingPassword.value = false;
  }
};
</script>

<template>
  <Toast />

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
        <VeeForm @submit="onChangePasswordSubmit">
          <!-- Current Password Field -->
          <div class="flex flex-col gap-1 mb-4">
            <div class="flex items-center gap-4">
              <label for="currentPassword" class="font-semibold w-40">Current password</label>
              <VeeField
                name="currentPassword"
                label="Current password"
                rules="required|min:8|max:64"
                v-slot="{ field, value }"
              >
                <Password
                  v-bind="field"
                  :model-value="value"
                  inputId="currentPassword"
                  class="flex-auto"
                  fluid
                  :feedback="false"
                  toggleMask
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
                v-slot="{ field, value }"
              >
                <Password
                  v-bind="field"
                  :model-value="value"
                  inputId="newPassword"
                  class="flex-auto"
                  fluid
                  :feedback="false"
                  toggleMask
                  autocomplete="new-password"
                />
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
                v-slot="{ field, value }"
              >
                <Password
                  v-bind="field"
                  :model-value="value"
                  inputId="repeatNewPassword"
                  class="flex-auto"
                  fluid
                  :feedback="false"
                  toggleMask
                  autocomplete="new-password"
                />
              </VeeField>
            </div>
            <ErrorMessage name="repeatNewPassword" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <!-- Action Buttons -->
          <div class="flex justify-end gap-2">
            <Button type="submit" label="Change Password" :disabled="isChangingPassword" />
          </div>
        </VeeForm>
      </template>
    </Card>
  </main>
</template>
