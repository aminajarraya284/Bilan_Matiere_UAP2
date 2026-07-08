<script setup lang="ts">
import { AlertCircle } from 'lucide-vue-next';
defineProps<{
  title?: string;
  message?: string;
}>();
defineEmits<{
  (e: 'retry'): void;
}>();
</script>

<template>
  <div
    class="p-6 border border-rose-100 rounded-xl bg-rose-50/30 text-rose-800 flex flex-col md:flex-row gap-4 items-center md:items-start"
  >
    <div class="w-10 h-10 rounded-full bg-rose-100 flex items-center justify-center text-rose-600 shrink-0">
      <AlertCircle class="w-5 h-5" />
    </div>
    <div class="flex-1 text-center md:text-left space-y-1">
      <h4 class="text-sm font-bold">{{ title || 'Une erreur est survenue' }}</h4>
      <p class="text-xs text-rose-600 leading-relaxed">
        {{ message || 'Impossible de charger les données. Veuillez vérifier votre connexion et réessayer.' }}
      </p>
      <div class="pt-2">
        <button
          @click="$emit('retry')"
          class="bg-rose-600 hover:bg-rose-700 text-white font-semibold text-xs py-1.5 px-3 rounded-lg transition-colors cursor-pointer"
        >
          Réessayer
        </button>
      </div>
    </div>
  </div>
</template>
