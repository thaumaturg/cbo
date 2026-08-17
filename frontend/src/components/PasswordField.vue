<script setup>
import IconField from "primevue/iconfield";
import InputIcon from "primevue/inputicon";
import InputPassword from "primevue/inputpassword";
import { computed, ref, useAttrs } from "vue";

defineOptions({ inheritAttrs: false });

const attrs = useAttrs();

const inputAttrs = computed(() => {
  const rest = { ...attrs };
  delete rest.class;
  return rest;
});

const mask = ref(true);
</script>

<template>
  <IconField :class="attrs.class">
    <InputPassword v-model:mask="mask" fluid v-bind="inputAttrs" />
    <InputIcon :class="[mask ? 'pi pi-eye' : 'pi pi-eye-slash', 'cursor-pointer']" @click="mask = !mask" />
  </IconField>
</template>
