<script setup lang="ts">
import { useAuthStore } from '@/features/auth/auth.store';
import { computed } from 'vue';

defineProps<{
  title: string;
  description?: string;
}>();

const authStore = useAuthStore();
const isAuthenticated = computed(() => authStore.isAuthenticated);
</script>

<template>
  <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-1 border-b border-slate-100">
    <div class="space-y-1">
      <h2 class="text-xl font-bold text-slate-800 tracking-tight font-display">{{ title }}</h2>
      <p v-if="description" class="text-xs text-slate-500 font-semibold leading-snug max-w-xl">
        {{ description }}
      </p>
    </div>
    <div class="flex items-center gap-3 shrink-0">
      <slot name="actions"></slot>
      <!-- Demo Data Banner -->
      <span
        v-if="isAuthenticated"
        class="inline-flex items-center gap-1.5 px-3 py-1 bg-amber-50 border border-amber-200/60 rounded-lg text-[10px] font-bold text-amber-700"
      >
        <span class="w-1.5 h-1.5 rounded-full bg-amber-500 animate-ping"></span>
        Données de démonstration — backend non connecté
      </span>
    </div>
  </div>
</template>
