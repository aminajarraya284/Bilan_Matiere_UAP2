<script setup lang="ts">
defineProps<{
  title: string;
  description?: string;
  breadcrumb?: string[];
}>();
</script>

<template>
  <header class="page-header flex flex-col gap-5 border-b pb-5 lg:flex-row lg:items-end lg:justify-between">
    <div class="min-w-0 space-y-2">
      <nav
        v-if="breadcrumb?.length"
        class="page-header__breadcrumb flex flex-wrap items-center gap-1.5 text-xs"
        aria-label="Fil d’Ariane"
      >
        <template v-for="(item, index) in breadcrumb" :key="`${item}-${index}`">
          <span v-if="index" aria-hidden="true">/</span>
          <span :class="index === breadcrumb.length - 1 ? 'font-semibold text-slate-700' : ''">{{ item }}</span>
        </template>
      </nav>
      <h1 class="page-title">{{ title }}</h1>
      <p v-if="description" class="page-description">{{ description }}</p>
    </div>

    <div class="flex shrink-0 flex-wrap items-center gap-2 sm:gap-3">
      <slot name="actions" />
    </div>
  </header>
</template>

<style scoped>
.page-header {
  border-color: var(--color-border);
}

.page-header__breadcrumb {
  color: var(--color-text-muted);
}
</style>
