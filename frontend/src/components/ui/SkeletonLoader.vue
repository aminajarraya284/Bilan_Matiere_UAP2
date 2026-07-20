<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'content' | 'cards' | 'table';
    rows?: number;
  }>(),
  {
    variant: 'content',
    rows: 5
  }
);
</script>

<template>
  <div class="space-y-4" aria-hidden="true">
    <div v-if="variant === 'content' || variant === 'cards'" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <div v-for="index in 4" :key="`card-${index}`" class="surface-card space-y-4 p-5">
        <div class="flex items-center justify-between gap-4">
          <span class="skeleton-block h-3 w-24"></span>
          <span class="skeleton-block h-9 w-9"></span>
        </div>
        <span class="skeleton-block block h-8 w-28"></span>
        <span class="skeleton-block block h-3 w-20"></span>
      </div>
    </div>

    <div v-if="variant === 'content' || variant === 'table'" class="app-table-shell p-4">
      <div class="mb-4 grid grid-cols-4 gap-4 border-b border-[var(--color-border)] pb-4">
        <span v-for="index in 4" :key="`head-${index}`" class="skeleton-block h-3"></span>
      </div>
      <div
        v-for="row in rows"
        :key="`row-${row}`"
        class="grid grid-cols-4 gap-4 border-b border-slate-100 py-4 last:border-0"
      >
        <span v-for="cell in 4" :key="`cell-${cell}`" class="skeleton-block h-3"></span>
      </div>
    </div>
  </div>
</template>
