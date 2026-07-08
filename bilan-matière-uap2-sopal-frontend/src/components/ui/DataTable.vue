<script setup lang="ts">
// Generic data table wrapper to style all our lists consistently
defineProps<{
  title?: string;
  loading?: boolean;
}>();
</script>

<template>
  <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col">
    <!-- Header -->
    <div
      v-if="title || $slots.header"
      class="px-6 py-4 border-b border-slate-50 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2 bg-slate-50/20 shrink-0"
    >
      <h3 v-if="title" class="text-xs font-bold text-slate-700 uppercase tracking-wider flex items-center gap-2">
        <slot name="title-icon"></slot>
        {{ title }}
      </h3>
      <slot name="header"></slot>
    </div>

    <!-- Table content container -->
    <div class="overflow-x-auto">
      <slot></slot>
    </div>

    <!-- Footer / Pagination -->
    <div
      v-if="$slots.footer"
      class="px-6 py-3 border-t border-slate-50 bg-slate-50/10 flex items-center justify-between shrink-0"
    >
      <slot name="footer"></slot>
    </div>
  </div>
</template>
