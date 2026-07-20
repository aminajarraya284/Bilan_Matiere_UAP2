<script setup lang="ts">
import SkeletonLoader from './SkeletonLoader.vue';

withDefaults(defineProps<{ message?: string; variant?: 'content' | 'cards' | 'table' }>(), {
  variant: 'content'
});
</script>

<template>
  <div class="min-h-[200px] p-2" role="status" aria-live="polite">
    <span class="sr-only">{{ message || 'Chargement des données en cours…' }}</span>
    <SkeletonLoader :variant="variant" />
  </div>
</template>
