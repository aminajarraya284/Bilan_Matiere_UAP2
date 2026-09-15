<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { storeToRefs } from 'pinia';
import { Download, Eye, FileText, RefreshCw, RotateCcw } from 'lucide-vue-next';
import DataSourceBadge from '@/components/ui/DataSourceBadge.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import PageHeader from '@/components/ui/PageHeader.vue';
import { useRapportsStore } from './rapports.store';
import type { RapportCellValue, RapportCode, RapportFilterKey } from './rapports.types';

const store = useRapportsStore();
const {
  catalogue,
  selectedCode,
  selectedReport,
  filters,
  preview,
  page,
  totalPages,
  loadingCatalogue,
  loadingPreview,
  exporting,
  error
} = storeToRefs(store);

const filterLabels: Record<RapportFilterKey, string> = {
  periode: 'Période',
  numeroOF: 'Numéro OF',
  date: 'Date',
  statut: 'Statut',
  severite: 'Sévérité',
  codeTypeMatiere: 'Type matière',
  dateDepuis: 'Date depuis',
  dateJusqua: "Date jusqu'à"
};

const requiredFilters: Partial<Record<RapportFilterKey, boolean>> = {
  periode: true,
  numeroOF: true,
  date: true
};

const activeFilters = computed(() => selectedReport.value?.filtres ?? []);
const missingRequiredFilter = computed(() =>
  activeFilters.value.find((key) => requiredFilters[key] && !filters.value[key].trim())
);
const canRun = computed(
  () => selectedReport.value?.disponible === true && missingRequiredFilter.value === undefined
);

const inputType = (key: RapportFilterKey): string => {
  if (key === 'periode') return 'month';
  if (key === 'date' || key === 'dateDepuis' || key === 'dateJusqua') return 'date';
  return 'text';
};

const formatCell = (value: RapportCellValue): string => {
  if (value === null || value === '') return 'Non renseigné';
  if (typeof value === 'boolean') return value ? 'Oui' : 'Non';
  if (typeof value === 'number') return new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 3 }).format(value);
  if (/^\d{4}-\d{2}-\d{2}T/.test(value)) {
    const date = new Date(value);
    if (!Number.isNaN(date.getTime())) return date.toLocaleString('fr-FR');
  }
  return value;
};

const chooseReport = async (code: RapportCode): Promise<void> => {
  store.select(code);
  if (canRun.value) await store.loadPreview();
};

const submit = async (): Promise<void> => {
  page.value = 1;
  await store.loadPreview();
};

onMounted(async () => {
  await store.loadCatalogue();
  if (canRun.value) await store.loadPreview();
});
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Rapports"
      description="Consulter et exporter les projections réelles des bilans, inventaires, imports et alertes."
    />
    <DataSourceBadge vertical="rapports" />

    <section v-if="error" class="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800" role="alert">
      <p class="font-bold">Le rapport n’a pas pu être chargé.</p>
      <p class="mt-1">{{ error }}</p>
    </section>

    <section class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm" aria-labelledby="catalogue-title">
      <div class="mb-4 flex items-center justify-between gap-3">
        <div>
          <h2 id="catalogue-title" class="text-sm font-black text-slate-900">Catalogue disponible</h2>
          <p class="mt-1 text-xs text-slate-500">Le catalogue est filtré par vos droits backend.</p>
        </div>
        <button
          class="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-bold text-slate-700 disabled:opacity-50"
          :disabled="loadingCatalogue"
          @click="store.loadCatalogue"
        >
          <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loadingCatalogue }" />
          Actualiser
        </button>
      </div>

      <LoadingState v-if="loadingCatalogue" />
      <div v-else-if="catalogue.length === 0" class="rounded-lg bg-slate-50 p-6 text-center text-sm text-slate-500" role="status">
        Aucun rapport n’est disponible pour votre rôle.
      </div>
      <div v-else class="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <button
          v-for="report in catalogue"
          :key="report.code"
          class="rounded-xl border p-4 text-left transition"
          :class="
            selectedCode === report.code
              ? 'border-blue-600 bg-blue-50 shadow-sm'
              : 'border-slate-200 bg-white hover:border-blue-300'
          "
          :disabled="!report.disponible"
          @click="chooseReport(report.code)"
        >
          <span class="text-[10px] font-black uppercase tracking-wider text-blue-700">{{ report.code }}</span>
          <h3 class="mt-2 text-sm font-black text-slate-900">{{ report.titre }}</h3>
          <p class="mt-2 text-xs leading-5 text-slate-500">{{ report.description }}</p>
          <div class="mt-3 flex items-center justify-between text-[10px] font-bold uppercase tracking-wide text-slate-500">
            <span>{{ report.module }}</span>
            <span>{{ report.formats.join(', ') }}</span>
          </div>
        </button>
      </div>
    </section>

    <section v-if="selectedReport" class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p class="text-[10px] font-black uppercase tracking-wider text-blue-700">{{ selectedReport.code }}</p>
          <h2 class="mt-1 text-lg font-black text-slate-900">{{ selectedReport.titre }}</h2>
          <p v-if="selectedReport.unitePrincipale" class="mt-1 text-xs font-semibold text-slate-500">
            Unité principale : {{ selectedReport.unitePrincipale }}
          </p>
        </div>
        <div class="flex flex-wrap gap-2">
          <button
            class="inline-flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs font-bold text-slate-700"
            @click="store.resetFilters"
          >
            <RotateCcw class="h-4 w-4" />
            Réinitialiser
          </button>
          <button
            class="inline-flex items-center gap-2 rounded-lg bg-blue-900 px-4 py-2 text-xs font-bold text-white disabled:cursor-not-allowed disabled:opacity-50"
            :disabled="!canRun || loadingPreview"
            @click="submit"
          >
            <Eye class="h-4 w-4" />
            {{ loadingPreview ? 'Chargement…' : 'Afficher' }}
          </button>
          <button
            class="inline-flex items-center gap-2 rounded-lg bg-emerald-700 px-4 py-2 text-xs font-bold text-white disabled:cursor-not-allowed disabled:opacity-50"
            :disabled="!canRun || exporting"
            @click="store.exportCsv"
          >
            <Download class="h-4 w-4" />
            {{ exporting ? 'Export…' : 'Exporter CSV' }}
          </button>
        </div>
      </div>

      <form class="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4" @submit.prevent="submit">
        <label v-for="key in activeFilters" :key="key" class="space-y-1.5">
          <span class="text-[10px] font-black uppercase tracking-wide text-slate-500">
            {{ filterLabels[key] }}<span v-if="requiredFilters[key]" class="text-rose-600"> *</span>
          </span>
          <select
            v-if="key === 'statut'"
            v-model="filters.statut"
            class="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm"
          >
            <option value="">Tous</option>
            <option value="Ouverte">Ouverte</option>
            <option value="Acquittee">Acquittée</option>
            <option value="Resolue">Résolue</option>
            <option value="Ignoree">Ignorée</option>
          </select>
          <select
            v-else-if="key === 'severite'"
            v-model="filters.severite"
            class="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm"
          >
            <option value="">Toutes</option>
            <option value="Information">Information</option>
            <option value="Avertissement">Avertissement</option>
            <option value="Erreur">Erreur</option>
          </select>
          <input
            v-else
            v-model="filters[key]"
            :type="inputType(key)"
            class="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm outline-none focus:border-blue-500"
            :placeholder="filterLabels[key]"
          />
        </label>
      </form>
      <p v-if="missingRequiredFilter" class="mt-3 text-xs font-semibold text-amber-700">
        Le filtre « {{ filterLabels[missingRequiredFilter] }} » est obligatoire.
      </p>
    </section>

    <LoadingState v-if="loadingPreview" />

    <section v-else-if="preview" class="space-y-4" aria-labelledby="preview-title">
      <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div>
            <div class="flex items-center gap-2">
              <FileText class="h-5 w-5 text-blue-700" />
              <h2 id="preview-title" class="text-base font-black text-slate-900">{{ preview.metadata.titre }}</h2>
            </div>
            <p class="mt-1 text-xs text-slate-500">
              Généré le {{ new Date(preview.metadata.genereLeUtc).toLocaleString('fr-FR') }}
              <span v-if="preview.metadata.generePar"> par {{ preview.metadata.generePar }}</span>
              <span v-if="preview.metadata.periode"> · {{ preview.metadata.periode }}</span>
            </p>
            <p v-if="preview.metadata.sources.length" class="mt-1 text-[11px] text-slate-500">
              Sources : {{ preview.metadata.sources.join(', ') }}
            </p>
          </div>
          <span
            class="rounded-full px-3 py-1 text-[10px] font-black uppercase tracking-wide"
            :class="preview.metadata.estPartiel ? 'bg-amber-100 text-amber-800' : 'bg-emerald-100 text-emerald-800'"
          >
            {{ preview.metadata.estPartiel ? 'Données partielles' : 'Projection disponible' }}
          </span>
        </div>

        <div v-if="preview.summary.length" class="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div v-for="item in preview.summary" :key="item.label" class="rounded-lg bg-slate-50 p-3">
            <p class="text-[10px] font-black uppercase tracking-wide text-slate-500">{{ item.label }}</p>
            <p class="mt-1 text-lg font-black text-slate-900">
              {{ formatCell(item.value) }} <span v-if="item.unite" class="text-xs text-slate-500">{{ item.unite }}</span>
            </p>
          </div>
        </div>

        <ul v-if="preview.metadata.avertissements.length" class="mt-4 rounded-lg bg-amber-50 p-4 text-xs text-amber-900">
          <li v-for="warning in preview.metadata.avertissements" :key="warning">• {{ warning }}</li>
        </ul>
      </div>

      <div class="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
        <div v-if="preview.rows.length === 0" class="p-10 text-center text-sm text-slate-500" role="status">
          Aucune donnée réelle ne correspond aux filtres sélectionnés.
        </div>
        <div v-else class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-200 text-left text-xs">
            <thead class="bg-slate-50 text-[10px] font-black uppercase tracking-wide text-slate-500">
              <tr>
                <th v-for="column in preview.columns" :key="`${column.label}-${column.unite ?? ''}`" class="whitespace-nowrap px-4 py-3">
                  {{ column.label }}<span v-if="column.unite"> ({{ column.unite }})</span>
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr v-for="row in preview.rows" :key="row.id" class="hover:bg-slate-50">
                <td
                  v-for="(cell, index) in row.cells"
                  :key="`${row.id}-${index}`"
                  class="whitespace-nowrap px-4 py-3 font-medium text-slate-700"
                  :class="{ 'italic text-slate-400': cell === null || cell === '' }"
                >
                  {{ formatCell(cell) }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="totalPages > 1" class="flex items-center justify-between border-t border-slate-200 px-4 py-3">
          <span class="text-xs text-slate-500">Page {{ page }} sur {{ totalPages }}</span>
          <div class="flex gap-2">
            <button
              class="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-bold disabled:opacity-40"
              :disabled="page <= 1 || loadingPreview"
              @click="store.goToPage(page - 1)"
            >
              Précédent
            </button>
            <button
              class="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-bold disabled:opacity-40"
              :disabled="page >= totalPages || loadingPreview"
              @click="store.goToPage(page + 1)"
            >
              Suivant
            </button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
