<script setup lang="ts">
import StatusBadge from './StatusBadge.vue';

defineProps<{
  status: string;
  variant?: 'info' | 'success' | 'warning' | 'attention' | 'danger' | 'neutral' | 'closed';
}>();
</script>

<template>
  <StatusBadge :status="status" :variant="variant" />
</template>
