<script setup lang="ts">
import { Info, CheckCircle2, AlertTriangle, AlertOctagon } from 'lucide-vue-next';

withDefaults(
  defineProps<{
    type?: 'info' | 'success' | 'warning' | 'danger';
    title?: string;
    message?: string;
  }>(),
  {
    type: 'info'
  }
);
</script>

<template>
  <div
    class="flex items-start gap-3.5 p-4 rounded-xl border text-xs"
    :class="{
      'bg-blue-50/50 border-blue-100 text-blue-900': type === 'info',
      'bg-emerald-50/50 border-emerald-100 text-emerald-900': type === 'success',
      'bg-amber-50/50 border-amber-100 text-amber-900': type === 'warning',
      'bg-rose-50/50 border-rose-100 text-rose-900': type === 'danger'
    }"
  >
    <!-- Icon -->
    <div class="shrink-0 mt-0.5">
      <Info v-if="type === 'info'" class="w-4 h-4 text-blue-600" />
      <CheckCircle2 v-else-if="type === 'success'" class="w-4 h-4 text-emerald-600" />
      <AlertTriangle v-else-if="type === 'warning'" class="w-4 h-4 text-amber-600" />
      <AlertOctagon v-else-if="type === 'danger'" class="w-4 h-4 text-rose-600" />
    </div>

    <!-- Content -->
    <div class="space-y-0.5 leading-relaxed">
      <h5 v-if="title" class="font-bold tracking-tight">{{ title }}</h5>
      <p
        class="font-medium text-slate-600 text-[11px]"
        :class="{
          'text-blue-700': type === 'info',
          'text-emerald-700': type === 'success',
          'text-amber-700': type === 'warning',
          'text-rose-700': type === 'danger'
        }"
      >
        <slot>{{ message }}</slot>
      </p>
    </div>
  </div>
</template>
