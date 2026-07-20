<script setup lang="ts">
import { Loader2 } from 'lucide-vue-next';

withDefaults(
  defineProps<{
    label: string;
    kind?: 'import' | 'sync';
  }>(),
  {
    kind: 'import'
  }
);
</script>

<template>
  <div class="motion-reveal rounded-lg border border-primary-100 bg-primary-50 p-4" role="status" aria-live="polite">
    <div class="flex items-center gap-3">
      <Loader2 class="motion-spinner h-4 w-4 shrink-0 text-primary-700" aria-hidden="true" />
      <div class="min-w-0 flex-1">
        <p class="text-sm font-semibold text-primary-900">{{ label }}</p>
        <div class="motion-progress__bar mt-2"></div>
      </div>
    </div>
    <ol class="mt-3 grid grid-cols-3 gap-2 text-xs font-medium text-[var(--color-text-secondary)]">
      <li class="motion-progress__step">1. Préparation</li>
      <li class="motion-progress__step">2. {{ kind === 'sync' ? 'Synchronisation' : 'Lecture' }}</li>
      <li class="motion-progress__step">3. Finalisation</li>
    </ol>
  </div>
</template>
