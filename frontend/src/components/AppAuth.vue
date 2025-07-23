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
  <Dialog v-model:visible="authModalStore.isOpen" modal header="Authentication" :draggable="false">
    <Tabs value="0">
      <TabList>
        <Tab value="0">Login</Tab>
        <Tab value="1">Register</Tab>
      </TabList>
      <TabPanels>
        <TabPanel value="0">
          <form>
            <div class="flex items-center gap-4 mb-4">
              <label for="loginEmail" class="font-semibold w-24">Email</label>
              <InputText id="loginEmail" class="flex-auto" autocomplete="off" />
            </div>
            <div class="flex items-center gap-4 mb-8">
              <label for="loginPassword" class="font-semibold w-24">Password</label>
              <InputText id="loginPassword" type="password" class="flex-auto" autocomplete="off" />
            </div>
            <div class="flex justify-end gap-2">
              <Button type="submit" label="Login" @click="toggleAuthModal"></Button>
            </div>
          </form>
        </TabPanel>
        <TabPanel value="1">
          <form>
            <div class="flex items-center gap-4 mb-4">
              <label for="registerEmail" class="font-semibold w-24">Email</label>
              <InputText id="registerEmail" class="flex-auto" autocomplete="off" />
            </div>
            <div class="flex items-center gap-4 mb-4">
              <label for="registerUsername" class="font-semibold w-24">Username</label>
              <InputText id="registerUsername" class="flex-auto" autocomplete="off" />
            </div>
            <div class="flex items-center gap-4 mb-4">
              <label for="registerFullName" class="font-semibold w-24">Full Name</label>
              <InputText id="registerFullName" class="flex-auto" autocomplete="off" />
            </div>
            <div class="flex items-center gap-4 mb-4">
              <label for="registerPassword" class="font-semibold w-24">Password</label>
              <InputText id="registerPassword" type="password" class="flex-auto" autocomplete="off" />
            </div>
            <div class="flex items-center gap-4 mb-8">
              <label for="registerRepeatPassword" class="font-semibold w-24">Repeat password</label>
              <InputText id="registerRepeatPassword" type="password" class="flex-auto" autocomplete="off" />
            </div>
            <div class="flex justify-end gap-2">
              <Button type="submit" label="Register" @click="toggleAuthModal"></Button>
            </div>
          </form>
        </TabPanel>
      </TabPanels>
    </Tabs>
  </Dialog>
</template>

<style scoped></style>
