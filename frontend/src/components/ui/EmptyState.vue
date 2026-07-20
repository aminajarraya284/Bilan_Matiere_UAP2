<script setup lang="ts">
import { Inbox } from 'lucide-vue-next';
defineProps<{ title?: string; message?: string }>();
</script>

<template>
  <div
    class="flex flex-col items-center justify-center rounded-xl border border-dashed border-slate-300 bg-slate-50/70 p-10 text-slate-500"
  >
    <Inbox class="mb-3 h-10 w-10 text-slate-400" aria-hidden="true" />
    <h4 class="mb-1 text-base font-semibold text-slate-700">{{ title || 'Aucune donnée disponible' }}</h4>
    <p class="max-w-md text-center text-sm leading-relaxed text-slate-500">
      {{ message || "Il n'y a aucun enregistrement correspondant à vos critères de recherche." }}
    </p>
  </div>
</template>
