<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { Download, Eye, Search } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { archivageService } from './archivage.service';
import type { ArchiveListItem } from './archivage.types';

const items = ref<ArchiveListItem[]>([]);
const loading = ref(true);
const error = ref('');
const page = ref(1);
const totalPages = ref(1);
const periode = ref('');
const statut = ref('');

const charger = async () => {
  loading.value = true;
  error.value = '';
  try {
    const result = await archivageService.rechercher({ page: page.value, pageSize: 20, periode: periode.value || undefined, statut: statut.value || undefined });
    items.value = result.items;
    totalPages.value = result.totalPages;
  } catch {
    error.value = "Impossible de charger l'historique des périodes.";
  } finally {
    loading.value = false;
  }
};

const rechercher = () => { page.value = 1; void charger(); };
const changerPage = (delta: number) => { page.value += delta; void charger(); };
const formatDate = (value: string) => new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short', timeStyle: 'short', timeZone: 'UTC' }).format(new Date(value));

onMounted(charger);
</script>

<template>
  <div class="space-y-6">
    <PageHeader title="Historique des périodes" description="Archives mensuelles figées et consultables en lecture seule." />
    <section class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <form class="flex flex-wrap items-end gap-3" @submit.prevent="rechercher">
        <label class="grid gap-1 text-xs font-bold text-slate-600">Période<input v-model="periode" type="month" class="rounded-lg border border-slate-200 px-3 py-2" /></label>
        <label class="grid gap-1 text-xs font-bold text-slate-600">Statut<select v-model="statut" class="rounded-lg border border-slate-200 px-3 py-2"><option value="">Tous</option><option value="OK">OK</option><option value="Anomalie">Anomalie</option><option value="DonneesManquantes">Données manquantes</option></select></label>
        <button class="inline-flex items-center gap-2 rounded-lg bg-blue-900 px-4 py-2 text-xs font-bold text-white"><Search class="h-4 w-4" />Rechercher</button>
      </form>
    </section>
    <div v-if="error" class="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800" role="alert">{{ error }}</div>
    <LoadingState v-else-if="loading" />
    <section v-else class="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
      <table class="w-full text-left text-sm">
        <thead class="bg-slate-50 text-xs uppercase text-slate-500"><tr><th class="p-3">Période</th><th class="p-3">Enregistrée le</th><th class="p-3">Par</th><th class="p-3">Statut</th><th class="p-3">Anomalies</th><th class="p-3">Données manquantes</th><th class="p-3">Version</th><th class="p-3">Actions</th></tr></thead>
        <tbody><tr v-for="item in items" :key="item.id" class="border-t border-slate-100"><td class="p-3 font-bold">{{ item.periode }}</td><td class="p-3">{{ formatDate(item.dateEnregistrementUtc) }}</td><td class="p-3">{{ item.enregistrePar }}</td><td class="p-3">{{ item.statutGlobal }}</td><td class="p-3">{{ item.nombreAnomalies }}</td><td class="p-3">{{ item.nombreDonneesManquantes }}</td><td class="p-3">{{ item.versionCalcul }}</td><td class="p-3"><div class="flex gap-2"><router-link :to="`/historique-periodes/${item.periode}`" class="inline-flex items-center gap-1 text-blue-700"><Eye class="h-4 w-4" />Voir</router-link><button class="inline-flex items-center gap-1 text-emerald-700" @click="archivageService.telechargerRapport(item.periode)"><Download class="h-4 w-4" />Rapport</button></div></td></tr></tbody>
      </table>
      <p v-if="items.length === 0" class="p-8 text-center text-sm text-slate-500">Aucune période enregistrée.</p>
      <footer v-if="items.length" class="flex items-center justify-between border-t p-3 text-xs"><button :disabled="page <= 1" @click="changerPage(-1)">Précédent</button><span>Page {{ page }} / {{ totalPages }}</span><button :disabled="page >= totalPages" @click="changerPage(1)">Suivant</button></footer>
    </section>
  </div>
</template>

