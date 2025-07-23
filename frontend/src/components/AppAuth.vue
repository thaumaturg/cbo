<script setup>
import useAuthModalStore from "@/stores/auth-modal.js";
import Dialog from "primevue/dialog";
import InputText from "primevue/inputtext";
import Button from "primevue/button";

import Tabs from "primevue/tabs";
import TabList from "primevue/tablist";
import Tab from "primevue/tab";
import TabPanels from "primevue/tabpanels";
import TabPanel from "primevue/tabpanel";

const authModalStore = useAuthModalStore();

const toggleAuthModal = () => {
  authModalStore.toggle();
};
</script>

<template>
  <Dialog v-model:visible="authModalStore.isOpen" modal header="Edit Profile">
    <Tabs value="0">
      <TabList>
        <Tab value="0">Login</Tab>
        <Tab value="1">Register</Tab>
      </TabList>
      <TabPanels>
        <TabPanel value="0">
          <div class="flex items-center gap-4 mb-4">
            <label for="username" class="font-semibold w-24">Username</label>
            <InputText id="username" class="flex-auto" autocomplete="off" />
          </div>
          <div class="flex items-center gap-4 mb-8">
            <label for="email" class="font-semibold w-24">Email</label>
            <InputText id="email" class="flex-auto" autocomplete="off" />
          </div>
          <div class="flex justify-end gap-2">
            <Button type="button" label="Login" @click="toggleAuthModal"></Button>
          </div>
        </TabPanel>
        <TabPanel value="1">
          <div class="flex items-center gap-4 mb-4">
            <label for="email" class="font-semibold w-24">Email</label>
            <InputText id="email" class="flex-auto" autocomplete="off" />
          </div>
          <div class="flex items-center gap-4 mb-4">
            <label for="username" class="font-semibold w-24">Username</label>
            <InputText id="username" class="flex-auto" autocomplete="off" />
          </div>
          <div class="flex items-center gap-4 mb-4">
            <label for="password" class="font-semibold w-24">Password</label>
            <InputText id="password" class="flex-auto" autocomplete="off" />
          </div>
          <div class="flex items-center gap-4 mb-4">
            <label for="repeatPassword" class="font-semibold w-24">Repeat password</label>
            <InputText id="repeatPassword" class="flex-auto" autocomplete="off" />
          </div>
          <div class="flex items-center gap-4 mb-8">
            <label for="name" class="font-semibold w-24">Name</label>
            <InputText id="name" class="flex-auto" autocomplete="off" />
          </div>
          <div class="flex justify-end gap-2">
            <Button type="button" label="Register" @click="toggleAuthModal"></Button>
          </div>
        </TabPanel>
      </TabPanels>
    </Tabs>
  </Dialog>
</template>

<style scoped></style>
