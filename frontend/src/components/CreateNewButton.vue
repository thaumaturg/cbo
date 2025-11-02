<script setup>
defineProps({
  entityType: {
    type: String,
    required: true,
  },
});

defineEmits(["create"]);
</script>

<template>
  <button
    @click="$emit('create')"
    class="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900"
  >
    <i class="pi pi-plus text-sm"></i>
    <span>Create New {{ entityType }}</span>
  </button>
</template>
