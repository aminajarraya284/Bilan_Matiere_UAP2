<script setup lang="ts">
withDefaults(
  defineProps<{
    title: string;
    description?: string;
    level?: 2 | 3 | 4;
  }>(),
  {
    level: 3
  }
);
</script>

<template>
  <div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
    <div class="flex min-w-0 items-stretch gap-3">
      <span class="w-1 shrink-0 rounded-full bg-primary-700" aria-hidden="true"></span>
      <div class="min-w-0 space-y-1">
        <component :is="`h${level}`" class="section-title">{{ title }}</component>
        <p v-if="description" class="section-description">{{ description }}</p>
      </div>
    </div>
    <div v-if="$slots.action" class="flex shrink-0 flex-wrap items-center gap-2">
      <slot name="action" />
    </div>
  </div>
</template>
