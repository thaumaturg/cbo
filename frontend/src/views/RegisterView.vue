<script setup>
import PasswordField from "@/components/PasswordField.vue";
import { LIMITS } from "@/domain/limits.js";
import { eager } from "@/includes/validation.js";
import { useAuthStore } from "@/stores/auth.js";
import { toFormFailure } from "@/utils/error.js";
import { useNotify } from "@/utils/notify.js";
import { keepRedirect, safeRedirect } from "@/utils/redirect.js";
import Button from "primevue/button";
import Card from "primevue/card";
import InputText from "primevue/inputtext";
import Message from "primevue/message";
import { ref } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";

const FIELDS = ["email", "username", "fullName", "password"];

const {
  usernameLengthMin,
  usernameLengthMax,
  fullNameLengthMin,
  fullNameLengthMax,
  passwordLengthMin,
  passwordLengthMax,
} = LIMITS.accounts;

const USERNAME_RULES = `required|username_chars|min:${usernameLengthMin}|max:${usernameLengthMax}`;
const FULL_NAME_RULES = `latin_cyrillic_latvian|min:${fullNameLengthMin}|max:${fullNameLengthMax}`;
const PASSWORD_RULES = `required|min:${passwordLengthMin}|max:${passwordLengthMax}`;

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();
const notify = useNotify();

const banner = ref("");

const onSubmit = async (values, { setErrors }) => {
  banner.value = "";

  const result = await authStore.register({
    email: values.email,
    username: values.username,
    fullName: values.fullName,
    password: values.password,
  });

  if (result.success) {
    notify.success("Account created", `Welcome, ${authStore.userName}.`);
    await router.replace(safeRedirect(route.query.redirect));
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
        <h1 class="text-2xl font-bold">Register</h1>
      </template>
      <template #content>
        <VeeForm @submit="onSubmit" class="flex flex-col gap-4">
          <div class="flex flex-col gap-1">
            <label for="email" class="font-semibold">Email</label>
            <VeeField name="email" label="Email" rules="required|email" v-slot="slot">
              <InputText
                v-bind="eager(slot)"
                id="email"
                type="email"
                autocomplete="email"
                :disabled="authStore.isLoading"
                fluid
              />
            </VeeField>
            <ErrorMessage name="email" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <div class="flex flex-col gap-1">
            <label for="username" class="font-semibold">Username</label>
            <VeeField name="username" label="Username" :rules="USERNAME_RULES" v-slot="slot">
              <InputText
                v-bind="eager(slot)"
                id="username"
                autocomplete="username"
                autocapitalize="none"
                spellcheck="false"
                :disabled="authStore.isLoading"
                fluid
              />
            </VeeField>
            <ErrorMessage name="username" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <div class="flex flex-col gap-1">
            <label for="fullName" class="font-semibold">Full name (optional)</label>
            <VeeField name="fullName" label="Full name" :rules="FULL_NAME_RULES" v-slot="slot">
              <InputText v-bind="eager(slot)" id="fullName" autocomplete="name" :disabled="authStore.isLoading" fluid />
            </VeeField>
            <ErrorMessage name="fullName" v-slot="{ message }">
              <Message severity="error" variant="simple">{{ message }}</Message>
            </ErrorMessage>
          </div>

          <div class="flex flex-col gap-1">
            <label for="password" class="font-semibold">Password</label>
            <VeeField name="password" label="Password" :rules="PASSWORD_RULES" v-slot="slot">
              <PasswordField
                v-bind="eager(slot)"
                id="password"
                autocomplete="new-password"
                :disabled="authStore.isLoading"
              />
              <Message v-if="slot.errorMessage" severity="error" variant="simple">{{ slot.errorMessage }}</Message>
              <Message v-else severity="secondary" variant="simple" size="small">
                At least {{ passwordLengthMin }} characters.
              </Message>
            </VeeField>
          </div>

          <Message v-if="banner" severity="error">{{ banner }}</Message>

          <Button type="submit" label="Register" :loading="authStore.isLoading" fluid />
        </VeeForm>

        <p class="mt-4 text-center">
          Already have an account?
          <RouterLink :to="{ name: 'login', query: keepRedirect(route.query) }" class="font-semibold underline">
            Log in
          </RouterLink>
        </p>
      </template>
    </Card>
  </main>
</template>
