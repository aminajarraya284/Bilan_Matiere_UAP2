<script setup lang="ts">
defineProps<{
  id: string;
  label: string;
  hint?: string;
  error?: string;
  required?: boolean;
}>();
</script>

<template>
  <div class="space-y-1.5">
    <label
      :for="id"
      class="block text-[12px] font-black text-slate-700 uppercase tracking-wide flex items-center gap-1"
    >
      {{ label }}
      <span v-if="required" class="text-rose-500">*</span>
    </label>
    <div>
      <slot :id="id" :is-error="!!error"></slot>
    </div>
    <span v-if="hint" class="block text-[11px] font-semibold text-slate-400 leading-snug">
      {{ hint }}
    </span>
    <span v-if="error" class="block text-[10px] font-bold text-rose-600 leading-none mt-1">
      {{ error }}
    </span>
  </div>
</template>
