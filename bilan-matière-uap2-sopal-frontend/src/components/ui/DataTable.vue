<script setup lang="ts">
import SkeletonLoader from './SkeletonLoader.vue';

defineProps<{
  title?: string;
  loading?: boolean;
}>();
</script>

<template>
  <section class="app-table-shell flex flex-col" :aria-busy="loading || undefined">
    <div
      v-if="title || $slots.header"
      class="flex shrink-0 flex-col items-start justify-between gap-3 border-b border-[var(--color-border)] bg-white px-4 py-4 sm:flex-row sm:items-center sm:px-5"
    >
      <h3 v-if="title" class="card-title flex items-center gap-2">
        <slot name="title-icon" />
        {{ title }}
      </h3>
      <slot name="header" />
    </div>

    <div class="table-wrapper relative overflow-x-auto">
      <div v-if="loading" class="absolute inset-0 z-10 bg-white p-4">
        <span class="sr-only">Chargement…</span>
        <SkeletonLoader variant="table" />
      </div>
      <slot />
    </div>

    <div
      v-if="$slots.footer"
      class="flex shrink-0 items-center justify-between border-t border-[var(--color-border)] bg-[var(--color-surface-soft)] px-4 py-3 sm:px-5"
    >
      <slot name="footer" />
    </div>
  </section>
</template>
