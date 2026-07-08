<script setup lang="ts">
import PageHeader from '@/components/ui/PageHeader.vue';
import { FileBarChart2, Layers3, TrendingUp } from 'lucide-vue-next';

const cards = [
  {
    title: 'OF suivis',
    value: '42',
    detail: 'Ordres de fabrication intégrés au bilan UAP2.'
  },
  {
    title: 'Matières concernées',
    value: '6',
    detail: 'Types d’alliages et composants analysés.'
  },
  {
    title: 'Tendance',
    value: '+3,4 %',
    detail: 'Évolution du rendement sur la période.'
  }
];
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Bilan UAP2 (par OF)"
      description="Analyse détaillée du bilan matière ventilé par ordre de fabrication."
    />

    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div v-for="card in cards" :key="card.title" class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div class="flex items-center gap-2 text-blue-700">
          <TrendingUp v-if="card.title === 'Tendance'" class="h-4 w-4" />
          <Layers3 v-else-if="card.title === 'Matières concernées'" class="h-4 w-4" />
          <FileBarChart2 v-else class="h-4 w-4" />
          <h3 class="text-sm font-bold text-slate-900">{{ card.title }}</h3>
        </div>
        <p class="mt-3 text-2xl font-black text-slate-900">{{ card.value }}</p>
        <p class="mt-1 text-xs text-slate-500">{{ card.detail }}</p>
      </div>
    </div>

    <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <div class="flex items-center gap-2 text-slate-700">
        <Layers3 class="h-4 w-4" />
        <h3 class="text-sm font-bold">Présentation de la vue OF</h3>
      </div>
      <p class="mt-2 text-sm text-slate-500">
        Cette page sert de point d’entrée pour l’analyse par ordre de fabrication, avec un découpage prêt à enrichir par les données métier réelles.
      </p>
    </div>
  </div>
</template>
