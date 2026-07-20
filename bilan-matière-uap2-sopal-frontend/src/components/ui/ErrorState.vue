<script setup lang="ts">
import { AlertCircle } from 'lucide-vue-next';
import AppButton from './AppButton.vue';
defineProps<{ title?: string; message?: string }>();
defineEmits<{ (e: 'retry'): void }>();
</script>

<template>
  <div
    class="flex flex-col items-center gap-4 rounded-xl border border-rose-200 bg-rose-50/50 p-6 text-rose-900 md:flex-row md:items-start"
  >
    <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-rose-100 text-rose-700">
      <AlertCircle class="h-5 w-5" aria-hidden="true" />
    </div>
    <div class="flex-1 space-y-1 text-center md:text-left">
      <h4 class="text-base font-semibold text-rose-950">{{ title || 'Une erreur est survenue' }}</h4>
      <p class="text-sm leading-relaxed text-rose-800">
        {{ message || 'Impossible de charger les données. Veuillez vérifier votre connexion et réessayer.' }}
      </p>
      <div class="pt-2">
        <AppButton variant="danger" size="sm" @click="$emit('retry')">Réessayer</AppButton>
      </div>
    </div>
  </div>
</template>
