<script setup lang="ts">
import { Inbox } from 'lucide-vue-next';
defineProps<{
  title?: string;
  message?: string;
}>();
</script>

<template>
  <div
    class="flex flex-col items-center justify-center p-12 text-slate-400 border border-dashed border-slate-200 rounded-xl bg-slate-50/50"
  >
    <Inbox class="w-10 h-10 mb-3 text-slate-300" />
    <h4 class="text-sm font-bold text-slate-600 mb-1">{{ title || 'Aucune donnée disponible' }}</h4>
    <p class="text-xs text-slate-500 text-center max-w-md">
      {{ message || "Il n'y a aucun enregistrement correspondant à vos critères de recherche." }}
    </p>
  </div>
</template>
