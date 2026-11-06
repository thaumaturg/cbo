<script setup>
import PasswordField from "@/components/PasswordField.vue";
import { eager } from "@/includes/validation.js";
import { useAuthStore } from "@/stores/auth.js";
import { toFormFailure } from "@/utils/error.js";
import { keepRedirect, safeRedirect } from "@/utils/redirect.js";
import Button from "primevue/button";
import Card from "primevue/card";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import { computed, ref } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";

const FIELDS = ["usernameOrEmail", "password"];

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();

const banner = ref("");
// Wrong credentials never say which field was wrong, so both turn red with no message under them
const credentialsRejected = ref(false);
const sessionExpired = computed(() => route.query.sessionExpired === "true");

const onSubmit = async (values, { setErrors }) => {
  banner.value = "";
  credentialsRejected.value = false;

  const result = await authStore.login({
    usernameOrEmail: values.usernameOrEmail,
    password: values.password,
  });

  if (result.success) {
    await router.replace(safeRedirect(route.query.redirect));
    return;
  }

  if (result.status === 401) {
    credentialsRejected.value = true;
    banner.value = result.error;
    return;
  }

  const failure = toFormFailure(result, FIELDS);
  setErrors(failure.fieldErrors);
  banner.value = failure.banner;
};
</script>

<template>
  <main class="container mx-auto px-4 py-8 max-w-md">
    <Card>
      <template #title>
        <h1 class="text-2xl font-bold">Log in</h1>
      </template>
      <template #content>
        <Message v-if="sessionExpired" severity="info" class="mb-4">Your session has expired. Log in again.</Message>

        <VeeForm @submit="onSubmit" class="flex flex-col gap-4">
          <div class="flex flex-col gap-1">
            <label for="usernameOrEmail" class="font-semibold">Username or email</label>
            <VeeField name="usernameOrEmail" label="Username or email" rules="required" v-slot="slot">
              <InputText
                v-bind="eager(slot)"
                :invalid="!!slot.errorMessage || credentialsRejected"
                @update:model-value="credentialsRejected = false"
                id="usernameOrEmail"
                autocomplete="username"
                autocapitalize="none"
                spellcheck="false"
                :disabled="authStore.isLoading"
                fluid
              />
            </VeeField>
            <ErrorMessage name="usernameOrEmail" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <div class="flex flex-col gap-1">
            <label for="password" class="font-semibold">Password</label>
            <VeeField name="password" label="Password" rules="required" v-slot="slot">
              <PasswordField
                v-bind="eager(slot)"
                :invalid="!!slot.errorMessage || credentialsRejected"
                @update:model-value="credentialsRejected = false"
                id="password"
                autocomplete="current-password"
                :disabled="authStore.isLoading"
              />
            </VeeField>
            <ErrorMessage name="password" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <Message v-if="banner" severity="error">{{ banner }}</Message>

          <Button type="submit" label="Log in" :loading="authStore.isLoading" fluid />
        </VeeForm>

        <p class="mt-4 text-center">
          No account?
          <RouterLink :to="{ name: 'register', query: keepRedirect(route.query) }" class="font-semibold underline">
            Register
          </RouterLink>
        </p>
      </template>
    </Card>
  </main>
</template>
