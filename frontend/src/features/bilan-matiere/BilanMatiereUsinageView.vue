<script setup lang="ts">
import PageHeader from '@/components/ui/PageHeader.vue';
import { ArrowRight, Factory, PackageCheck } from 'lucide-vue-next';

const focusPoints = [
  'Suivi des pièces réceptionnées en usinage 2 par OF.',
  'Contrôle des écarts de poids et des non-réceptions.',
  'Vue consolidée pour appuyer les validations de bilan.'
];
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Bilan matière Usinage 2"
      description="Pilotage des flux de matière et suivi des ordres de fabrication usinés."
    />

    <div class="grid grid-cols-1 lg:grid-cols-[1.2fr_0.8fr] gap-4">
      <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
        <div class="flex items-center gap-3">
          <div class="rounded-lg bg-blue-50 p-2 text-blue-700">
            <Factory class="h-5 w-5" />
          </div>
          <div>
            <h3 class="text-sm font-bold text-slate-900">Vue opérationnelle</h3>
            <p class="text-xs text-slate-500">Synthèse dédiée au parcours matière côté usinage.</p>
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div class="rounded-lg bg-slate-50 p-3">
            <p class="text-[10px] font-bold uppercase tracking-wider text-slate-400">Pièces reçues</p>
            <p class="mt-1 text-lg font-black text-slate-900">18 OF</p>
          </div>
          <div class="rounded-lg bg-slate-50 p-3">
            <p class="text-[10px] font-bold uppercase tracking-wider text-slate-400">Écart constaté</p>
            <p class="mt-1 text-lg font-black text-slate-900">1,2 %</p>
          </div>
        </div>
      </div>

      <div class="rounded-xl border border-slate-200 bg-slate-900 p-5 text-white shadow-sm space-y-3">
        <div class="flex items-center gap-2">
          <PackageCheck class="h-5 w-5 text-blue-300" />
          <h3 class="text-sm font-bold">Points de suivi</h3>
        </div>
        <ul class="space-y-2 text-sm text-slate-300">
          <li v-for="point in focusPoints" :key="point" class="flex gap-2">
            <span class="mt-1 h-1.5 w-1.5 rounded-full bg-blue-400"></span>
            <span>{{ point }}</span>
          </li>
        </ul>
      </div>
    </div>

    <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <div class="flex items-center justify-between gap-3">
        <div>
          <h3 class="text-sm font-bold text-slate-900">Navigation métier</h3>
          <p class="text-xs text-slate-500">Retour au bilan matière général pour poursuivre l’analyse.</p>
        </div>
        <router-link
          to="/bilan-matiere"
          class="inline-flex items-center gap-2 rounded-lg bg-blue-700 px-3 py-2 text-xs font-semibold text-white transition hover:bg-blue-800"
        >
          Ouvrir le bilan général
          <ArrowRight class="h-4 w-4" />
        </router-link>
      </div>
    </div>
  </div>
</template>
