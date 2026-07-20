<script setup lang="ts">
import { LockKeyhole } from 'lucide-vue-next';

defineProps<{
  id: string;
  label: string;
  hint?: string;
  error?: string;
  required?: boolean;
  automatic?: boolean;
}>();
</script>

<template>
  <div class="space-y-1.5" :class="{ 'motion-auto-field': automatic }">
    <div class="flex min-h-5 items-center justify-between gap-2">
      <label :for="id" class="field-label flex items-center gap-1">
        {{ label }}
        <span v-if="required" class="text-rose-600" aria-hidden="true">*</span>
        <span v-if="required" class="sr-only">(obligatoire)</span>
      </label>
      <span
        v-if="automatic"
        class="inline-flex items-center gap-1 rounded-full border border-primary-100 bg-primary-50 px-2 py-0.5 text-xs font-semibold text-primary-900"
      >
        <LockKeyhole class="h-3 w-3" aria-hidden="true" />
        Automatique
      </span>
    </div>
    <div>
      <slot :id="id" :is-error="!!error" :described-by="error ? `${id}-error` : hint ? `${id}-hint` : undefined" />
    </div>
    <span v-if="hint" :id="`${id}-hint`" class="block text-xs leading-relaxed text-slate-500">{{ hint }}</span>
    <span
      v-if="error"
      :id="`${id}-error`"
      class="mt-1 block text-xs font-semibold leading-relaxed text-rose-700"
      role="alert"
    >
      {{ error }}
    </span>
  </div>
</template>
