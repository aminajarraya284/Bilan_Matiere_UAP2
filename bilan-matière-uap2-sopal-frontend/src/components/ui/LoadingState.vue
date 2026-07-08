<script setup lang="ts">
import { Loader2 } from 'lucide-vue-next';
defineProps<{
  message?: string;
}>();
</script>

<template>
  <div class="flex flex-col items-center justify-center p-8 min-h-[200px] text-slate-500">
    <Loader2 class="w-8 h-8 animate-spin text-blue-900 mb-2" />
    <span class="text-xs font-semibold uppercase tracking-wider">{{
      message || 'Chargement des données en cours...'
    }}</span>
  </div>
</template>
