<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { FileSpreadsheet, Filter, Link2, Loader2, RefreshCw, Search, Upload } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import ProgressSteps from '@/components/ui/ProgressSteps.vue';
import { useAppToast } from '@/composables/useAppToast';
import { formatNumber } from '@/utils/formatters';
import { sortiesX3Service } from './sortiesX3.service';
import type { ConsommationComposant } from './sortiesX3.types';

const TYPES_MATIERE_AUTORISES = ['M3000L0', 'M3010L0', 'M3060L0'] as const;
type TypeMatiere = typeof TYPES_MATIERE_AUTORISES[number] | 'tous';

const mouvements = ref<ConsommationComposant[]>([]);
const total = ref(0);
const mouvementFile = ref<File | null>(null);
const encoursFile = ref<File | null>(null);
const isLoading = ref(true);
const isUploading = ref(false);
const isRefreshing = ref(false);
const unmatchedCount = ref(0);
const filtreMatiere = ref<TypeMatiere>('tous');
const rechercheOF = ref('');
const numeroOFNormalise = computed(() => rechercheOF.value.trim().toUpperCase().replace(/\s+/g, ''));
const toast = useAppToast();
let mouvementsController: AbortController | null = null;
let rechercheOFTimer: ReturnType<typeof setTimeout> | undefined;

const mouvementsFiltres = computed(() => {
  if (filtreMatiere.value === 'tous') return mouvements.value;
  return mouvements.value.filter(m => m.codeTypeMatiere === filtreMatiere.value);
});

const loadMouvements = async () => {
  mouvementsController?.abort();
  const controller = new AbortController();
  mouvementsController = controller;
  isLoading.value = true;
  try {
    const response = await sortiesX3Service.getMouvements({
      numeroOF: numeroOFNormalise.value || undefined,
      signal: controller.signal
    });
    if (controller.signal.aborted) return;
    mouvements.value = response.items;
    total.value = response.total;
  } catch (error) {
    if (controller.signal.aborted) return;
    console.error(error);
    toast.error('Le chargement des consommations composants a échoué.');
  } finally {
    if (mouvementsController === controller) {
      mouvementsController = null;
      isLoading.value = false;
    }
  }
};

const handleRechercheOF = () => {
  if (rechercheOFTimer) clearTimeout(rechercheOFTimer);
  rechercheOFTimer = setTimeout(() => {
    void loadMouvements();
  }, 300);
};

const selectFile = (event: Event, type: 'mouvement' | 'encours') => {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0] ?? null;
  if (type === 'mouvement') mouvementFile.value = file;
  else encoursFile.value = file;
};

const handleImport = async () => {
  if (!mouvementFile.value || !encoursFile.value) {
    toast.error('Sélectionnez les deux fichiers Excel avant de lancer le rapprochement.');
    return;
  }

  isUploading.value = true;
  try {
    const result = await sortiesX3Service.importExcelFiles(mouvementFile.value, encoursFile.value);
    unmatchedCount.value = result.unmatchedCount;
    mouvements.value = result.consommations;
    total.value = result.consommations.length;
    filtreMatiere.value = 'tous';
    rechercheOF.value = '';
    toast.success(
      `${result.successCount} consommation(s) par OF générée(s)` +
        (result.unmatchedCount ? `, ${result.unmatchedCount} mouvement(s) sans correspondance.` : '.')
    );
  } catch (error) {
    console.error(error);
    toast.error(error instanceof Error ? error.message : 'Le rapprochement des deux fichiers a échoué.');
  } finally {
    isUploading.value = false;
  }
};

const handleRefresh = async () => {
  isRefreshing.value = true;
  try {
    await loadMouvements();
    toast.success(`${total.value} consommation(s) disponible(s).`);
  } finally {
    isRefreshing.value = false;
  }
};

const formatDate = (value: string): string => {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : value || '—';
};

onMounted(loadMouvements);
onBeforeUnmount(() => {
  mouvementsController?.abort();
  if (rechercheOFTimer) clearTimeout(rechercheOFTimer);
});
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Consommations composants par OF"
      description="Rapprocher les mouvements de stock par transaction VCR avec les encours de production pour obtenir la sortie MP, l'OF, l'article et sa désignation."
    />

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <article class="rounded-xl border border-slate-100 bg-white p-5 shadow-sm">
        <div class="flex items-start gap-3">
          <div class="rounded-lg bg-blue-50 p-2 text-blue-900">
            <FileSpreadsheet class="h-5 w-5" />
          </div>
          <div class="min-w-0 flex-1">
            <h2 class="text-xs font-black uppercase text-slate-800">1. Mvt de stock par trans-VCR</h2>
            <p class="mt-1 text-xs leading-5 text-slate-500">
              Source de la quantité « Sortie MP ». La transaction VCR sert de clé de rapprochement.
            </p>
            <p v-if="mouvementFile" class="mt-2 truncate text-[11px] font-bold text-emerald-700">
              {{ mouvementFile.name }}
            </p>
          </div>
        </div>
        <label
          class="mt-4 inline-flex cursor-pointer items-center gap-2 rounded-lg border border-dashed border-slate-300 px-3 py-2 text-xs font-bold text-blue-900 hover:bg-blue-50"
        >
          <Upload class="h-4 w-4" />
          Sélectionner le fichier Excel
          <input
            class="hidden"
            type="file"
            accept=".xls,.xlsx"
            :disabled="isUploading"
            @change="selectFile($event, 'mouvement')"
          />
        </label>
      </article>

      <article class="rounded-xl border border-slate-100 bg-white p-5 shadow-sm">
        <div class="flex items-start gap-3">
          <div class="rounded-lg bg-emerald-50 p-2 text-emerald-700">
            <FileSpreadsheet class="h-5 w-5" />
          </div>
          <div class="min-w-0 flex-1">
            <h2 class="text-xs font-black uppercase text-slate-800">2. Encours production</h2>
            <p class="mt-1 text-xs leading-5 text-slate-500">
              Source de l'OF, de l'article et de la désignation associés à la transaction VCR.
            </p>
            <p v-if="encoursFile" class="mt-2 truncate text-[11px] font-bold text-emerald-700">
              {{ encoursFile.name }}
            </p>
          </div>
        </div>
        <label
          class="mt-4 inline-flex cursor-pointer items-center gap-2 rounded-lg border border-dashed border-slate-300 px-3 py-2 text-xs font-bold text-blue-900 hover:bg-blue-50"
        >
          <Upload class="h-4 w-4" />
          Sélectionner le fichier Excel
          <input
            class="hidden"
            type="file"
            accept=".xls,.xlsx"
            :disabled="isUploading"
            @change="selectFile($event, 'encours')"
          />
        </label>
      </article>
    </div>

    <div
      class="flex flex-col gap-3 rounded-xl border border-blue-100 bg-blue-50/60 p-4 sm:flex-row sm:items-center sm:justify-between"
    >
      <div class="flex items-start gap-3">
        <Link2 class="mt-0.5 h-5 w-5 shrink-0 text-blue-900" />
        <div>
          <p class="text-xs font-black uppercase text-blue-950">Rapprochement des deux fichiers</p>
          <p class="mt-1 text-xs text-blue-900/70">
            Les mouvements sont regroupés par OF. Les retours et quantités négatives restent déduits de la sortie MP.
          </p>
        </div>
      </div>
      <button
        class="inline-flex shrink-0 items-center justify-center gap-2 rounded-lg bg-blue-900 px-4 py-2.5 text-xs font-bold text-white disabled:cursor-not-allowed disabled:opacity-50"
        :disabled="isUploading || !mouvementFile || !encoursFile"
        @click="handleImport"
      >
        <Loader2 v-if="isUploading" class="h-4 w-4 motion-spinner" />
        <Link2 v-else class="h-4 w-4" />
        {{ isUploading ? 'Rapprochement…' : 'Générer les consommations' }}
      </button>
    </div>

    <ProgressSteps v-if="isUploading" kind="import" label="Lecture et rapprochement des deux fichiers Excel" />

    <div
      v-if="unmatchedCount"
      class="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-xs font-semibold text-amber-800"
    >
      {{ unmatchedCount }} mouvement(s) n'ont pas trouvé de transaction VCR correspondante dans l'encours production.
    </div>

    <div class="overflow-hidden rounded-xl border border-slate-100 bg-white shadow-sm">
      <div class="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 px-5 py-4">
        <div>
          <h2 class="text-xs font-black uppercase text-slate-700">Sorties MP par ordre de fabrication</h2>
          <p class="mt-1 text-[11px] text-slate-500">Résultat consolidé issu des deux fichiers Excel. Seuls les types matière M3000L0, M3010L0, M3060L0 sont affichés.</p>
        </div>
        <div class="flex flex-wrap items-center gap-3">
          <div class="relative min-w-64 flex-1 sm:flex-none">
            <label for="sorties-of-search" class="sr-only">Rechercher par numéro OF exact</label>
            <Search class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              id="sorties-of-search"
              v-model="rechercheOF"
              type="search"
              class="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-xs font-semibold text-slate-700 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100 sm:w-72"
              placeholder="Rechercher par OF exact"
              @input="handleRechercheOF"
            />
          </div>
          <!-- Filtre par type matière -->
          <div class="flex items-center gap-1.5 rounded-lg border border-slate-200 px-2 py-1.5">
            <Filter class="h-3.5 w-3.5 text-slate-400" />
            <select
              v-model="filtreMatiere"
              class="bg-transparent text-[11px] font-semibold text-slate-700 outline-none"
              aria-label="Filtrer par type matière"
            >
              <option value="tous">Tous</option>
              <option v-for="tm in TYPES_MATIERE_AUTORISES" :key="tm" :value="tm">{{ tm }}</option>
            </select>
          </div>
          <span class="font-mono text-xs text-slate-500">{{ mouvementsFiltres.length }} / {{ total }} ligne(s)</span>
          <button
            class="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 px-3 py-2 text-[10px] font-black uppercase text-slate-700 hover:bg-slate-50 disabled:opacity-50"
            :disabled="isRefreshing"
            @click="handleRefresh"
          >
            <RefreshCw class="h-3.5 w-3.5" :class="{ 'motion-spinner': isRefreshing }" />
            Actualiser
          </button>
        </div>
      </div>

      <LoadingState v-if="isLoading" />
      <div v-else class="overflow-x-auto">
        <table class="min-w-[980px] w-full text-left text-xs">
          <thead class="bg-slate-50 text-[10px] uppercase tracking-wider text-slate-500">
            <tr>
              <th class="p-3">Date</th>
              <th class="p-3">OF</th>
              <th class="p-3">Article</th>
              <th class="p-3">Désignation</th>
              <th class="p-3">Type matière</th>
              <th class="p-3 text-right">Consommation</th>
              <th class="p-3">Unité</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-for="move in mouvementsFiltres" :key="move.id" class="hover:bg-slate-50/50">
              <td class="p-3 font-mono text-slate-600">{{ formatDate(move.dateImputation) }}</td>
              <td class="p-3 font-mono font-bold text-slate-900">{{ move.numeroOF }}</td>
              <td class="p-3 font-mono text-blue-900">{{ move.articlePrincipal }}</td>
              <td class="p-3 text-slate-600">{{ move.designationPrincipale || '—' }}</td>
              <td class="p-3">
                <span class="rounded-full bg-blue-50 px-2 py-0.5 font-mono text-[10px] font-bold text-blue-900">
                  {{ move.codeTypeMatiere }}
                </span>
              </td>
              <td class="p-3 text-right font-mono font-black text-blue-900">
                {{ formatNumber(move.consommation, 2) }}
              </td>
              <td class="p-3 font-mono">{{ move.unite }}</td>
            </tr>
            <tr v-if="!mouvementsFiltres.length">
              <td colspan="7" class="p-8 text-center text-slate-500">
                <span v-if="numeroOFNormalise">Aucune sortie magasin trouvée pour l’OF « {{ numeroOFNormalise }} ».</span>
                <span v-else-if="!mouvements.length">Importez les deux fichiers Excel pour générer les consommations.</span>
                <span v-else>Aucune ligne pour le filtre « {{ filtreMatiere }} ».</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
