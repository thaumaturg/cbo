<script setup>
import { useAuthModalStore } from "@/stores/auth-modal.js";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import Button from "primevue/button";
import Password from "primevue/password";
import Message from "primevue/message";

import Tabs from "primevue/tabs";
import TabList from "primevue/tablist";
import Tab from "primevue/tab";
import TabPanels from "primevue/tabpanels";
import TabPanel from "primevue/tabpanel";

import { Form as VeeForm, Field as VeeField, ErrorMessage, defineRule, configure } from "vee-validate";
import { required, email, min, max, regex, confirmed } from "@vee-validate/rules";
import { ref } from "vue";

defineRule("required", required);
defineRule("email", email);
defineRule("min", min);
defineRule("max", max);
defineRule("regex", regex);
defineRule("confirmed", confirmed);
defineRule("password_chars", (value) => {
  if (!value) return true;
  return /^[A-Za-z0-9!@#$%^&*]+$/.test(value);
});
defineRule("username_chars", (value) => {
  if (!value) return true;
  return /^[A-Za-z0-9_]+$/.test(value);
});
defineRule("latin_cyrillic_latvian", (value) => {
  if (!value) return true;
  const pattern = /^[A-Za-zĀ-žЁёА-Яа-я\s]+$/u;
  return pattern.test(value) || "Only English, Russian, and Latvian letters are allowed";
});

configure({
  generateMessage: (ctx) => {
    const rule = ctx.rule?.name;
    switch (rule) {
      case "required":
        return "Required";
      case "email":
        return "Must be a valid email";
      case "min":
        return `Must be at least ${ctx.rule?.params[0]} characters`;
      case "max":
        return `Must be at most ${ctx.rule?.params[0]} characters`;
      case "username_chars":
        return "Latin letters, numbers, and underscores";
      case "password_chars":
        return "Latin letters, numbers, !@#$%^&*";
      case "confirmed":
        return "Passwords do not match";
      default:
        return "Field is invalid";
    }
  },
  validateOnBlur: true,
  validateOnChange: true,
  validateOnInput: false,
  validateOnModelUpdate: false,
});

const authModalStore = useAuthModalStore();

const toggleAuthModal = () => {
  authModalStore.toggle();
};

const loginStatus = ref("idle");
const registerStatus = ref("idle");

const onLoginSubmit = (values) => {
  console.log("Login submit", values);
  loginStatus.value = "loading";
  // simulate async processing
  setTimeout(() => {
    loginStatus.value = "success";
    setTimeout(() => {
      loginStatus.value = "idle";
      toggleAuthModal();
    }, 1000);
  }, 1500);
};

const onRegisterSubmit = (values) => {
  console.log("Register submit", values);
  registerStatus.value = "loading";
  // simulate async processing
  setTimeout(() => {
    registerStatus.value = "success";
    setTimeout(() => {
      registerStatus.value = "idle";
      toggleAuthModal();
    }, 1000);
  }, 1500);
};
</script>

<template>
  <Dialog v-model:visible="authModalStore.isOpen" modal header="Authentication" :draggable="false">
    <Tabs value="0">
      <TabList>
        <Tab value="0">Login</Tab>
        <Tab value="1">Register</Tab>
      </TabList>
      <TabPanels>
        <TabPanel value="0">
          <VeeForm @submit="onLoginSubmit">
            <div class="flex flex-col gap-1 mb-4">
              <div class="flex items-center gap-4">
                <label for="loginEmail" class="font-semibold w-24">Email</label>
                <VeeField
                  name="loginEmail"
                  id="loginEmail"
                  label="Email"
                  :rules="'required|email|min:6|max:254'"
                  v-slot="{ field }"
                >
                  <InputText v-bind="field" id="loginEmail" class="flex-auto" autocomplete="off" />
                </VeeField>
              </div>
              <ErrorMessage name="loginEmail" v-slot="{ message }">
                <Message severity="error">{{ message }}</Message>
              </ErrorMessage>
            </div>
            <div class="flex flex-col gap-1 mb-8">
              <div class="flex items-center gap-4">
                <label for="loginPassword" class="font-semibold w-24">Password</label>
                <VeeField
                  name="loginPassword"
                  id="loginPassword"
                  label="Password"
                  :rules="'required|password_chars|min:8|max:64'"
                  v-slot="{ field }"
                >
                  <Password
                    v-bind="field"
                    id="loginPassword"
                    class="flex-auto"
                    :feedback="false"
                    toggleMask
                    autocomplete="off"
                  />
                </VeeField>
              </div>
              <ErrorMessage name="loginPassword" v-slot="{ message }">
                <Message severity="error">{{ message }}</Message>
              </ErrorMessage>
            </div>
            <div class="mb-4" v-if="loginStatus === 'loading'">
              <Message severity="info">Logging in, please wait...</Message>
            </div>
            <div class="mb-4" v-if="loginStatus === 'success'">
              <Message severity="success">Login successful!</Message>
            </div>
            <div class="flex justify-end gap-2">
              <Button type="submit" label="Login" :disabled="loginStatus !== 'idle'"></Button>
            </div>
          </VeeForm>
        </TabPanel>
        <TabPanel value="1">
          <VeeForm @submit="onRegisterSubmit">
            <div class="flex flex-col gap-1 mb-4">
              <div class="flex items-center gap-4">
                <label for="registerEmail" class="font-semibold w-24">Email</label>
                <VeeField
                  name="registerEmail"
                  id="registerEmail"
                  label="Email"
                  :rules="'required|email|min:6|max:254'"
                  v-slot="{ field }"
                >
                  <InputText v-bind="field" id="registerEmail" class="flex-auto" autocomplete="off" />
                </VeeField>
              </div>
              <ErrorMessage name="registerEmail" v-slot="{ message }">
                <Message severity="error">{{ message }}</Message>
              </ErrorMessage>
            </div>
            <div class="flex flex-col gap-1 mb-4">
              <div class="flex items-center gap-4">
                <label for="registerUsername" class="font-semibold w-24">Username</label>
                <VeeField
                  name="registerUsername"
                  id="registerUsername"
                  label="Username"
                  :rules="'required|username_chars|min:6|max:16'"
                  v-slot="{ field }"
                >
                  <InputText v-bind="field" id="registerUsername" class="flex-auto" autocomplete="off" />
                </VeeField>
              </div>
              <ErrorMessage name="registerUsername" v-slot="{ message }">
                <Message severity="error">{{ message }}</Message>
              </ErrorMessage>
            </div>
            <div class="flex flex-col gap-1 mb-4">
              <div class="flex items-center gap-4">
                <label for="registerFullName" class="font-semibold w-24">Full Name</label>
                <VeeField
                  name="registerFullName"
                  id="registerFullName"
                  label="Full Name"
                  :rules="'latin_cyrillic_latvian|min:2|max:64'"
                  v-slot="{ field }"
                >
                  <InputText v-bind="field" id="registerFullName" class="flex-auto" autocomplete="off" />
                </VeeField>
              </div>
              <ErrorMessage name="registerFullName" v-slot="{ message }">
                <Message severity="error">{{ message }}</Message>
              </ErrorMessage>
            </div>
            <div class="flex flex-col gap-1 mb-4">
              <div class="flex items-center gap-4">
                <label for="registerPassword" class="font-semibold w-24">Password</label>
                <VeeField
                  name="registerPassword"
                  id="registerPassword"
                  label="Password"
                  :rules="'required|password_chars|min:8|max:64'"
                  v-slot="{ field }"
                >
                  <Password
                    v-bind="field"
                    id="registerPassword"
                    class="flex-auto"
                    :feedback="false"
                    toggleMask
                    autocomplete="off"
                  />
                </VeeField>
              </div>
              <ErrorMessage name="registerPassword" v-slot="{ message }">
                <Message severity="error">{{ message }}</Message>
              </ErrorMessage>
            </div>
            <div class="flex flex-col gap-1 mb-8">
              <div class="flex items-center gap-4">
                <label for="registerRepeatPassword" class="font-semibold w-24">Repeat password</label>
                <VeeField
                  name="registerRepeatPassword"
                  id="registerRepeatPassword"
                  label="Repeat password"
                  :rules="'required|password_chars|confirmed:@registerPassword'"
                  v-slot="{ field }"
                >
                  <Password
                    v-bind="field"
                    id="registerRepeatPassword"
                    class="flex-auto"
                    :feedback="false"
                    toggleMask
                    autocomplete="off"
                  />
                </VeeField>
              </div>
              <ErrorMessage name="registerRepeatPassword" v-slot="{ message }">
                <Message severity="error">{{ message }}</Message>
              </ErrorMessage>
            </div>
            <div class="mb-4" v-if="registerStatus === 'loading'">
              <Message severity="info">Registering account, please wait...</Message>
            </div>
            <div class="mb-4" v-if="registerStatus === 'success'">
              <Message severity="success">Registration successful!</Message>
            </div>
            <div class="flex justify-end gap-2">
              <Button type="submit" label="Register" :disabled="registerStatus !== 'idle'"></Button>
            </div>
          </VeeForm>
        </TabPanel>
      </TabPanels>
    </Tabs>
  </Dialog>
</template>

<style scoped></style>
