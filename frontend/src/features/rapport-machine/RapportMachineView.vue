<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { RefreshCw, Search } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import ErrorState from '@/components/ui/ErrorState.vue';
import EmptyState from '@/components/ui/EmptyState.vue';
import { rapportMachineService } from './rapportMachine.service';
import type {
  ImportExecutionSummary,
  RapportMachineFilters,
  RapportMachinePage
} from './rapportMachine.types';

const createFilters = (): RapportMachineFilters => ({
  dateDebut: '',
  dateFin: '',
  numeroOF: '',
  codeArticle: '',
  codePoste: '',
  processus: '',
  lotImport: '',
  page: 1,
  pageSize: 25,
  sortBy: 'date',
  sortDirection: 'desc'
});

const filters = reactive<RapportMachineFilters>(createFilters());
const result = ref<RapportMachinePage | null>(null);
const loading = ref(false);
const errorMessage = ref('');
let requestSequence = 0;

const totalPages = computed(() =>
  result.value ? Math.max(1, Math.ceil(result.value.total / result.value.pageSize)) : 1
);

const formatNumber = (value: number): string =>
  new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 4 }).format(value);

const formatDate = (value: string | null): string => {
  if (!value) return '—';
  return new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short' }).format(new Date(`${value.slice(0, 10)}T00:00:00`));
};

const formatDateTime = (value: string | null): string => {
  if (!value) return '—';
  return new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
};

const shortBatch = (value: string): string => value.slice(0, 8).toUpperCase();

const load = async (): Promise<void> => {
  const sequence = ++requestSequence;
  loading.value = true;
  errorMessage.value = '';
  try {
    const data = await rapportMachineService.search({ ...filters });
    if (sequence === requestSequence) result.value = data;
  } catch (error) {
    if (sequence === requestSequence) {
      errorMessage.value =
        error instanceof Error ? error.message : 'Le rapport machine n’a pas pu être chargé depuis l’API.';
    }
  } finally {
    if (sequence === requestSequence) loading.value = false;
  }
};

const search = (): void => {
  filters.page = 1;
  void load();
};

const reset = (): void => {
  Object.assign(filters, createFilters());
  void load();
};

const changePage = (page: number): void => {
  if (page < 1 || page > totalPages.value || page === filters.page) return;
  filters.page = page;
  void load();
};

const statusClass = (summary: ImportExecutionSummary | null): string => {
  if (!summary) return 'border-slate-200 bg-slate-100 text-slate-600';
  if (summary.statut === 'Fichier identique') return 'border-amber-200 bg-amber-50 text-amber-800';
  if (summary.statut.includes('Échec')) return 'border-rose-200 bg-rose-50 text-rose-800';
  return 'border-emerald-200 bg-emerald-50 text-emerald-800';
};

onMounted(() => {
  void load();
});
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Rapport machine YRJMCH"
      description="Consulter les quantités totales et les rebuts par poste, sans agréger les processus."
      :breadcrumb="['États X3', 'Rapport machine']"
    >
      <template #actions>
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-lg bg-blue-900 px-4 py-2 text-xs font-black uppercase text-white hover:bg-blue-800 disabled:opacity-50"
          :disabled="loading"
          @click="load"
        >
          <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />
          Actualiser
        </button>
      </template>
    </PageHeader>

    <section class="rounded-2xl border border-slate-100 bg-white p-5 shadow-sm">
      <form class="grid gap-3 md:grid-cols-3 xl:grid-cols-6" @submit.prevent="search">
        <label class="text-xs font-bold text-slate-600">
          Date début
          <input v-model="filters.dateDebut" type="date" class="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2" />
        </label>
        <label class="text-xs font-bold text-slate-600">
          Date fin
          <input v-model="filters.dateFin" type="date" class="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2" />
        </label>
        <label class="text-xs font-bold text-slate-600">
          Numéro OF
          <input v-model.trim="filters.numeroOF" class="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2" />
        </label>
        <label class="text-xs font-bold text-slate-600">
          Article
          <input v-model.trim="filters.codeArticle" class="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2" />
        </label>
        <label class="text-xs font-bold text-slate-600">
          Poste
          <input v-model.trim="filters.codePoste" class="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2" />
        </label>
        <label class="text-xs font-bold text-slate-600">
          Processus
          <select v-model="filters.processus" class="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2">
            <option value="">Tous</option>
            <option value="CTRL">CTRL</option>
            <option value="SCI">SCI</option>
            <option value="EMR">EMR</option>
            <option value="TRN">TRN</option>
            <option value="Inconnue">Inconnue</option>
          </select>
        </label>
        <label class="text-xs font-bold text-slate-600 md:col-span-2">
          Lot d’import
          <input
            v-model.trim="filters.lotImport"
            placeholder="Identifiant complet du lot"
            class="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2"
          />
        </label>
        <label class="text-xs font-bold text-slate-600">
          Trier par
          <select v-model="filters.sortBy" class="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2">
            <option value="date">Date</option>
            <option value="numeroof">Numéro OF</option>
            <option value="article">Article</option>
            <option value="poste">Poste</option>
            <option value="processus">Processus</option>
            <option value="quantitetotale">Quantité totale</option>
            <option value="quantiterebut">Quantité rebut</option>
            <option value="quantitenette">Quantité nette</option>
            <option value="lotimport">Lot d’import</option>
          </select>
        </label>
        <label class="text-xs font-bold text-slate-600">
          Sens
          <select v-model="filters.sortDirection" class="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2">
            <option value="desc">Décroissant</option>
            <option value="asc">Croissant</option>
          </select>
        </label>
        <div class="flex items-end gap-2 md:col-span-2">
          <button type="submit" class="inline-flex items-center gap-2 rounded-lg bg-slate-900 px-4 py-2 text-xs font-black uppercase text-white">
            <Search class="h-4 w-4" />
            Rechercher
          </button>
          <button type="button" class="rounded-lg border border-slate-200 px-4 py-2 text-xs font-black uppercase text-slate-700" @click="reset">
            Réinitialiser
          </button>
        </div>
      </form>
    </section>

    <section v-if="result" class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-8" aria-label="Indicateurs">
      <div
        v-for="item in [
          ['Lignes', result.kpis.nombreLignes],
          ['OF', result.kpis.nombreOF],
          ['CTRL', result.kpis.ctrl],
          ['SCI', result.kpis.sci],
          ['EMR', result.kpis.emr],
          ['TRN', result.kpis.trn],
          ['Inconnues', result.kpis.inconnues],
          ['Rejetées', result.kpis.lignesRejetees]
        ]"
        :key="String(item[0])"
        class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm"
      >
        <p class="text-[10px] font-black uppercase tracking-wider text-slate-400">{{ item[0] }}</p>
        <p class="mt-1 text-xl font-black text-slate-900">{{ Number(item[1]).toLocaleString('fr-FR') }}</p>
      </div>
    </section>

    <section v-if="result" class="grid gap-4 lg:grid-cols-2">
      <article
        v-for="tracking in [
          { title: 'Dernière tentative d’import', value: result.imports.derniereTentative },
          { title: 'Dernier import réussi', value: result.imports.dernierImportReussi }
        ]"
        :key="tracking.title"
        class="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm"
      >
        <div class="flex flex-wrap items-center justify-between gap-2">
          <h2 class="text-xs font-black uppercase tracking-wider text-slate-700">{{ tracking.title }}</h2>
          <span class="rounded-full border px-2.5 py-1 text-[10px] font-black" :class="statusClass(tracking.value)">
            {{ tracking.value?.statut ?? 'Non disponible' }}
          </span>
        </div>
        <p v-if="tracking.value?.message" class="mt-3 text-xs font-semibold text-slate-600">{{ tracking.value.message }}</p>
        <dl class="mt-3 grid grid-cols-2 gap-2 text-xs">
          <div><dt class="text-slate-400">Fichier</dt><dd class="font-bold text-slate-800">{{ tracking.value?.nomFichier ?? '—' }}</dd></div>
          <div><dt class="text-slate-400">Terminé le</dt><dd class="font-bold text-slate-800">{{ formatDateTime(tracking.value?.dateFinUtc ?? null) }}</dd></div>
          <div><dt class="text-slate-400">Lignes valides</dt><dd class="font-bold text-slate-800">{{ tracking.value?.lignesValides ?? '—' }}</dd></div>
          <div><dt class="text-slate-400">Lignes rejetées</dt><dd class="font-bold text-slate-800">{{ tracking.value?.lignesRejetees ?? '—' }}</dd></div>
        </dl>
      </article>
    </section>

    <LoadingState v-if="loading && !result" variant="table" message="Chargement du rapport machine" />
    <ErrorState
      v-else-if="errorMessage"
      title="Rapport machine indisponible"
      :message="errorMessage"
      @retry="load"
    />
    <EmptyState
      v-else-if="result && result.items.length === 0"
      title="Aucune ligne YRJMCH"
      message="Aucune donnée ne correspond aux filtres sélectionnés."
    />

    <section v-else-if="result" class="overflow-hidden rounded-2xl border border-slate-100 bg-white shadow-sm">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-slate-100 text-left text-xs">
          <thead class="bg-slate-50 text-[10px] font-black uppercase tracking-wider text-slate-500">
            <tr>
              <th class="px-3 py-3">Date</th><th class="px-3 py-3">OF</th><th class="px-3 py-3">Article</th>
              <th class="px-3 py-3">Désignation</th><th class="px-3 py-3">Poste</th><th class="px-3 py-3">Opération</th>
              <th class="px-3 py-3">Équipe</th><th class="px-3 py-3">Processus</th>
              <th class="px-3 py-3 text-right">Quantité totale</th><th class="px-3 py-3 text-right">Quantité rebut</th>
              <th class="px-3 py-3 text-right">Quantité nette</th><th class="px-3 py-3">Lot d’import</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 text-slate-700">
            <tr v-for="row in result.items" :key="row.id" class="hover:bg-slate-50">
              <td class="whitespace-nowrap px-3 py-3">{{ formatDate(row.dateProduction) }}</td>
              <td class="whitespace-nowrap px-3 py-3 font-bold">{{ row.numeroOF }}</td>
              <td class="whitespace-nowrap px-3 py-3">{{ row.codeArticle }}</td>
              <td class="max-w-56 truncate px-3 py-3" :title="row.designationArticle ?? ''">{{ row.designationArticle ?? '—' }}</td>
              <td class="whitespace-nowrap px-3 py-3">{{ row.codePoste }}</td>
              <td class="whitespace-nowrap px-3 py-3">{{ row.numeroOperation }}</td>
              <td class="whitespace-nowrap px-3 py-3">{{ row.equipe ?? '—' }}</td>
              <td class="whitespace-nowrap px-3 py-3"><span class="rounded-full bg-blue-50 px-2 py-1 font-black text-blue-900">{{ row.processus }}</span></td>
              <td class="whitespace-nowrap px-3 py-3 text-right tabular-nums">{{ formatNumber(row.quantiteTotale) }}</td>
              <td class="whitespace-nowrap px-3 py-3 text-right tabular-nums">{{ formatNumber(row.quantiteRebut) }}</td>
              <td class="whitespace-nowrap px-3 py-3 text-right font-black tabular-nums">{{ formatNumber(row.quantiteNette) }}</td>
              <td class="whitespace-nowrap px-3 py-3 font-mono" :title="row.importBatchId">{{ shortBatch(row.importBatchId) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <footer class="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 px-4 py-3 text-xs text-slate-600">
        <span>{{ result.total.toLocaleString('fr-FR') }} ligne(s) — page {{ filters.page }} / {{ totalPages }}</span>
        <div class="flex gap-2">
          <button class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:opacity-40" :disabled="filters.page <= 1" @click="changePage(filters.page - 1)">Précédent</button>
          <button class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:opacity-40" :disabled="filters.page >= totalPages" @click="changePage(filters.page + 1)">Suivant</button>
        </div>
      </footer>
    </section>
  </div>
</template>
