<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { AlertTriangle, CheckCircle2, ChevronLeft, ChevronRight, Download, FileSpreadsheet, History, Info, RefreshCw, Search, Table, Upload } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import { useAppToast } from '@/composables/useAppToast';
import {
  x3ExcelImportService,
  type X3ClassificationResult,
  type X3ExcelFileType,
  type X3ImportedSource,
  type X3ImportedSourceStatus
} from '@/services/x3ExcelImportService';
import { rapportMachineService } from '@/features/rapport-machine/rapportMachine.service';
import type { ProcessusRapportMachine, RapportMachineItem } from '@/features/rapport-machine/rapportMachine.types';
import { mouvementsStockService } from '@/features/mouvements-stock/mouvementsStock.service';
import type { MouvementStockItem } from '@/features/mouvements-stock/mouvementsStock.types';
import {
  AUCUNE_ENTREE_OF_MESSAGE,
  FORMULES,
  USAGE_BLOCK,
  X3_CARDS
} from './etatsX3.content';

const toast = useAppToast();

const emptyX3Status = (fileType: X3ExcelFileType): X3ImportedSourceStatus => ({
  bilanType: 'USINAGE_2',
  fileType,
  fileName: '',
  importedAt: '',
  rowCount: 0,
  status: 'Non importé'
});

const sourceCards = X3_CARDS;

const sourceStatuses = ref<Record<X3ExcelFileType, X3ImportedSourceStatus>>({
  MVTSTOTRSVCR: emptyX3Status('MVTSTOTRSVCR'),
  YRJMCH: emptyX3Status('YRJMCH')
});

interface ImportResultSummary {
  fileType: X3ExcelFileType;
  fileName: string;
  importedAt: string;
  status: string;
  rowsRead?: number;
  rowsInserted?: number;
  rowsUpdated?: number;
  rowsIgnored?: number;
  rowsRejected?: number;
  rowCount?: number;
  error?: string;
}

const importingFileType = ref<X3ExcelFileType | null>(null);
const recalculatingClassification = ref(false);
const classificationResult = ref<X3ClassificationResult | null>(null);
const lastImportResult = ref<ImportResultSummary | null>(null);

// Données et filtres du tableau YRJMCH
const yrjmchItems = ref<RapportMachineItem[]>([]);
const yrjmchTotal = ref(0);
const yrjmchPage = ref(1);
const yrjmchPageSize = ref(10);
const yrjmchLoading = ref(false);
const yrjmchError = ref<string | null>(null);
const yrjmchSearchOF = ref('');
const yrjmchSearchArticle = ref('');
const yrjmchFilterProcessus = ref<ProcessusRapportMachine | ''>('');

// Données et filtres du tableau MVTSTOTRSVCR
const mvtItems = ref<MouvementStockItem[]>([]);
const mvtTotal = ref(0);
const mvtPage = ref(1);
const mvtPageSize = ref(10);
const mvtLoading = ref(false);
const mvtError = ref<string | null>(null);
const mvtSearchOF = ref('');
const mvtSearchArticle = ref('');

const refreshX3ExcelStatuses = async () => {
  await x3ExcelImportService.refreshStatuses();
  sourceCards.forEach(({ fileType }) => {
    sourceStatuses.value[fileType] = x3ExcelImportService.getStatus('USINAGE_2', fileType);
  });
};

const fetchYrjmchData = async () => {
  yrjmchLoading.value = true;
  yrjmchError.value = null;
  try {
    const res = await rapportMachineService.search({
      page: yrjmchPage.value,
      pageSize: yrjmchPageSize.value,
      numeroOF: yrjmchSearchOF.value.trim() || undefined,
      codeArticle: yrjmchSearchArticle.value.trim() || undefined,
      processus: yrjmchFilterProcessus.value || undefined,
      sortBy: 'date',
      sortDirection: 'desc'
    });
    yrjmchItems.value = res.items;
    yrjmchTotal.value = res.total;
  } catch (error) {
    yrjmchItems.value = [];
    yrjmchTotal.value = 0;
    yrjmchError.value = error instanceof Error ? error.message : 'Impossible de charger les données YRJMCH.';
  } finally {
    yrjmchLoading.value = false;
  }
};

const fetchMvtData = async () => {
  mvtLoading.value = true;
  mvtError.value = null;
  try {
    const res = await mouvementsStockService.search({
      page: mvtPage.value,
      pageSize: mvtPageSize.value,
      numeroOF: mvtSearchOF.value.trim() || undefined,
      codeArticle: mvtSearchArticle.value.trim() || undefined,
      sortBy: 'date',
      sortDirection: 'desc'
    });
    mvtItems.value = res.items;
    mvtTotal.value = res.total;
  } catch (error) {
    mvtItems.value = [];
    mvtTotal.value = 0;
    mvtError.value = error instanceof Error ? error.message : 'Impossible de charger les données MVTSTOTRSVCR.';
  } finally {
    mvtLoading.value = false;
  }
};

const getX3ExcelStatusClass = (status: string): string => {
  switch (status) {
    case 'Importé':
      return 'text-emerald-700 bg-emerald-50 border-emerald-200';
    case 'Erreur':
      return 'text-rose-700 bg-rose-50 border-rose-200';
    default:
      return 'text-slate-700 bg-slate-100 border-slate-200';
  }
};

const formatX3ImportDate = (value: string): string => {
  if (!value) return '—';
  return new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
};

const handleImportX3Excel = async (event: Event, fileType: X3ExcelFileType) => {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  if (importingFileType.value) {
    input.value = '';
    return;
  }

  importingFileType.value = fileType;
  try {
    let imported: X3ImportedSource;
    if (fileType === 'MVTSTOTRSVCR') {
      imported = await x3ExcelImportService.importMVTSTOTRSVCR(file, 'USINAGE_2');
      await fetchMvtData();
    } else {
      imported = await x3ExcelImportService.importYRJMCH(file, 'USINAGE_2');
      await fetchYrjmchData();
    }
    await refreshX3ExcelStatuses();
    const updatedStatus = sourceStatuses.value[fileType];

    lastImportResult.value = {
      fileType,
      fileName: file.name,
      importedAt: new Date().toISOString(),
      status: imported.status || updatedStatus.status || 'Importé',
      rowsRead: imported.rowsRead ?? updatedStatus.rowsRead,
      rowsInserted: imported.rowsInserted ?? updatedStatus.rowsInserted,
      rowsUpdated: imported.rowsUpdated ?? updatedStatus.rowsUpdated,
      rowsIgnored: imported.rowsIgnored ?? updatedStatus.rowsIgnored,
      rowsRejected: imported.rowsRejected ?? updatedStatus.rowsRejected,
      rowCount: imported.rowCount ?? updatedStatus.rowCount,
      error: imported.error || updatedStatus.error
    };

    const insertedCount = lastImportResult.value.rowsInserted ?? 0;
    const updatedCount = lastImportResult.value.rowsUpdated ?? 0;
    const ignoredCount = lastImportResult.value.rowsIgnored ?? 0;
    const summaryText = `${insertedCount} insérée(s), ${updatedCount} mise(s) à jour, ${ignoredCount} inchangée(s)`;
    toast.success(`Import ${fileType} réussi (${file.name}) : ${summaryText}.`);
  } catch (error) {
    await refreshX3ExcelStatuses();
    const errMsg = error instanceof Error ? error.message : 'Import Excel X3 invalide.';
    lastImportResult.value = {
      fileType,
      fileName: file.name,
      importedAt: new Date().toISOString(),
      status: 'Erreur',
      error: errMsg
    };
    toast.error(errMsg);
  } finally {
    importingFileType.value = null;
    input.value = '';
  }
};

const formatOptionalCount = (value: number | undefined): string =>
  typeof value === 'number' ? value.toLocaleString('fr-FR') : '—';

const hasImportedSource = (fileType: X3ExcelFileType): boolean =>
  sourceStatuses.value[fileType].status === 'Importé';

const getImportButtonLabel = (fileType: X3ExcelFileType): string => {
  if (importingFileType.value === fileType) return 'Import en cours';
  return hasImportedSource(fileType) ? 'Réimporter une nouvelle version' : 'Importer Excel';
};

const handleDownloadArchive = async (status: X3ImportedSourceStatus) => {
  await x3ExcelImportService.downloadArchivedFile(status);
};

const handleRecalculerClassification = async () => {
  if (recalculatingClassification.value) return;
  recalculatingClassification.value = true;
  try {
    classificationResult.value = await x3ExcelImportService.recalculerClassificationYRJMCH();
    toast.success(
      `Classification recalculée : ${classificationResult.value.lignesModifiees.toLocaleString('fr-FR')} ligne(s) modifiée(s).`
    );
  } catch (error) {
    toast.error(error instanceof Error ? error.message : 'Le recalcul de la classification a échoué.');
  } finally {
    recalculatingClassification.value = false;
  }
};

/**
 * Un import MVT qui a lu des lignes sans en retenir aucune signale, en pratique,
 * un export ne contenant pas de transaction « Entrée OF ».
 */
const mvtSansEntreeOf = computed(() => {
  const status = sourceStatuses.value.MVTSTOTRSVCR;
  return status.status === 'Importé' && (status.rowsRead ?? 0) > 0 && status.rowCount === 0;
});

onMounted(() => {
  void Promise.all([refreshX3ExcelStatuses(), fetchYrjmchData(), fetchMvtData()]);
});
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="États X3"
      description="Importer, contrôler et synchroniser les fichiers X3 utilisés par les bilans Usinage 2 et UAP2."
    />

    <div class="rounded-2xl border border-slate-100 bg-white p-5 shadow-sm">
      <div class="flex items-start gap-3">
        <div class="rounded-xl bg-blue-50 p-3 text-blue-900">
          <FileSpreadsheet class="h-6 w-6" />
        </div>
        <div>
          <h2 class="text-sm font-black uppercase tracking-wider text-slate-900">Sources X3 communes</h2>
          <p class="mt-1 max-w-3xl text-xs font-semibold leading-5 text-slate-500">
            Cette page centralise les sources X3 utilisées dans les bilans matière. Les fichiers peuvent être
            réimportés à chaque nouvelle version et leur historique est conservé dans SQL Server.
          </p>
          <p class="mt-2 max-w-3xl rounded-lg bg-blue-50/70 px-3 py-2 text-xs font-bold leading-5 text-blue-900">
            Pour YRJMCH, le processus est déterminé automatiquement par les trois premiers caractères du poste de la
            colonne B : SCI, EMR ou TRN. Les calculs sont ensuite regroupés par Numéro OF et Code article.
          </p>
        </div>
      </div>
    </div>

    <!-- Bannière Résultat du dernier import -->
    <div
      v-if="lastImportResult"
      class="rounded-2xl border p-5 shadow-sm transition-all"
      :class="
        lastImportResult.status === 'Erreur'
          ? 'border-rose-200 bg-rose-50/80'
          : 'border-emerald-200 bg-emerald-50/80'
      "
    >
      <div class="flex items-start justify-between gap-4">
        <div class="flex items-start gap-3">
          <div
            class="rounded-xl p-2.5"
            :class="
              lastImportResult.status === 'Erreur'
                ? 'bg-rose-100 text-rose-800'
                : 'bg-emerald-100 text-emerald-800'
            "
          >
            <CheckCircle2 v-if="lastImportResult.status !== 'Erreur'" class="h-6 w-6" />
            <AlertTriangle v-else class="h-6 w-6" />
          </div>
          <div>
            <div class="flex flex-wrap items-center gap-2">
              <h3
                class="text-sm font-black uppercase tracking-wider"
                :class="lastImportResult.status === 'Erreur' ? 'text-rose-900' : 'text-emerald-950'"
              >
                Résultat de l'import — {{ lastImportResult.fileType }}
              </h3>
              <span
                class="rounded-full px-2.5 py-0.5 text-[10px] font-black uppercase"
                :class="
                  lastImportResult.status === 'Erreur'
                    ? 'bg-rose-200 text-rose-900'
                    : 'bg-emerald-200 text-emerald-900'
                "
              >
                {{ lastImportResult.status === 'Erreur' ? 'Échec de l\'import' : 'Import Réussi' }}
              </span>
            </div>
            <p
              class="mt-1 text-xs font-semibold"
              :class="lastImportResult.status === 'Erreur' ? 'text-rose-700' : 'text-emerald-800'"
            >
              Fichier : <span class="font-bold">{{ lastImportResult.fileName }}</span>
              <span class="mx-2 text-slate-300">•</span>
              {{ formatX3ImportDate(lastImportResult.importedAt) }}
            </p>
          </div>
        </div>
        <button
          type="button"
          class="text-xs font-bold text-slate-400 hover:text-slate-600"
          @click="lastImportResult = null"
        >
          ✕ Masquer
        </button>
      </div>

      <div
        v-if="lastImportResult.status !== 'Erreur'"
        class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6"
      >
        <div class="rounded-xl border border-emerald-200/60 bg-white p-3 shadow-xs">
          <span class="block text-[10px] font-black uppercase text-slate-400">Lignes lues</span>
          <span class="text-base font-black text-slate-900">{{ formatOptionalCount(lastImportResult.rowsRead) }}</span>
        </div>
        <div class="rounded-xl border border-emerald-200/60 bg-white p-3 shadow-xs">
          <span class="block text-[10px] font-black uppercase text-emerald-600">Insérées (Nouvelles)</span>
          <span class="text-base font-black text-emerald-700">+{{ formatOptionalCount(lastImportResult.rowsInserted) }}</span>
        </div>
        <div class="rounded-xl border border-emerald-200/60 bg-white p-3 shadow-xs">
          <span class="block text-[10px] font-black uppercase text-blue-600">Mises à jour</span>
          <span class="text-base font-black text-blue-700">{{ formatOptionalCount(lastImportResult.rowsUpdated) }}</span>
        </div>
        <div class="rounded-xl border border-emerald-200/60 bg-white p-3 shadow-xs">
          <span class="block text-[10px] font-black uppercase text-slate-400">Inchangées</span>
          <span class="text-base font-black text-slate-600">{{ formatOptionalCount(lastImportResult.rowsIgnored) }}</span>
        </div>
        <div class="rounded-xl border border-emerald-200/60 bg-white p-3 shadow-xs">
          <span class="block text-[10px] font-black uppercase text-rose-500">Erreurs / Rejetées</span>
          <span class="text-base font-black text-rose-700">{{ formatOptionalCount(lastImportResult.rowsRejected) }}</span>
        </div>
        <div class="rounded-xl border border-emerald-200/60 bg-white p-3 shadow-xs">
          <span class="block text-[10px] font-black uppercase text-slate-400">Total valides</span>
          <span class="text-base font-black text-slate-900">{{ formatOptionalCount(lastImportResult.rowCount) }}</span>
        </div>
      </div>

      <div
        v-else-if="lastImportResult.error"
        class="mt-3 rounded-xl border border-rose-200 bg-white p-3 text-xs font-semibold text-rose-800"
      >
        {{ lastImportResult.error }}
      </div>
    </div>

    <div class="grid grid-cols-1 gap-5 xl:grid-cols-2">
      <article
        v-for="card in sourceCards"
        :key="card.fileType"
        class="rounded-2xl border border-slate-100 bg-white p-5 shadow-sm"
      >
        <div class="flex flex-col gap-4 border-b border-slate-100 pb-4 sm:flex-row sm:items-start sm:justify-between">
          <div class="min-w-0">
            <h3 class="text-base font-black text-slate-950">{{ card.title }}</h3>
            <p class="text-xs font-bold text-blue-900">{{ card.subtitle }}</p>
            <p class="mt-2 text-xs font-medium leading-5 text-slate-500">{{ card.description }}</p>
            <p class="mt-3 text-[10px] font-black uppercase tracking-wider text-slate-400">Utilisé par</p>
            <ul class="mt-1 space-y-0.5 text-xs font-semibold text-slate-600">
              <li v-for="usage in card.usedBy" :key="usage">• {{ usage }}</li>
            </ul>
          </div>

          <div class="flex flex-wrap gap-2">
            <label
              class="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-2 text-[10px] font-black uppercase text-blue-900 hover:bg-slate-50"
              :class="{
                'cursor-not-allowed opacity-50': importingFileType !== null,
                'cursor-pointer': importingFileType === null
              }"
            >
              <Upload class="h-3.5 w-3.5" :class="{ 'animate-pulse': importingFileType === card.fileType }" />
              {{ getImportButtonLabel(card.fileType) }}
              <input
                class="hidden"
                type="file"
                accept=".xlsx,.xls"
                :disabled="importingFileType !== null"
                @change="handleImportX3Excel($event, card.fileType)"
              />
            </label>
            <RouterLink
              class="inline-flex items-center gap-1.5 rounded-lg bg-slate-900 px-3 py-2 text-[10px] font-black uppercase text-white hover:bg-slate-800"
              to="/imports-historique"
            >
              <History class="h-3.5 w-3.5" />
              Consulter l'historique
            </RouterLink>
            <button
              v-if="card.fileType === 'YRJMCH'"
              type="button"
              class="inline-flex items-center gap-1.5 rounded-lg bg-blue-900 px-3 py-2 text-[10px] font-black uppercase text-white hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-50"
              :disabled="recalculatingClassification"
              @click="handleRecalculerClassification"
            >
              <RefreshCw class="h-3.5 w-3.5" :class="{ 'animate-spin': recalculatingClassification }" />
              Recalculer la classification
            </button>
          </div>
        </div>

        <!-- Fonctionnement : enchaînement exact du traitement, sans interprétation. -->
        <div class="mt-4 rounded-xl border border-slate-100 bg-slate-50/70 p-4">
          <h4 class="text-xs font-black uppercase tracking-wider text-slate-800">Fonctionnement</h4>
          <ul class="mt-2 space-y-1">
            <li
              v-for="etape in card.flow"
              :key="etape"
              class="font-mono text-[11px] font-semibold leading-5 text-slate-600"
            >
              {{ etape }}
            </li>
          </ul>
        </div>

        <div
          class="mt-3 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-blue-100 bg-blue-50 p-3 text-blue-900"
        >
          <span class="flex items-start gap-2 text-xs font-semibold leading-5">
            <Info class="mt-0.5 h-4 w-4 shrink-0" />
            {{ card.notice.text }}
          </span>
        </div>

        <div
          v-if="card.fileType === 'YRJMCH' && classificationResult"
          class="mt-3 grid grid-cols-2 gap-2 rounded-xl border border-emerald-100 bg-emerald-50 p-3 text-[10px] font-bold text-emerald-900 sm:grid-cols-4"
        >
          <span>Total : {{ classificationResult.lignesAnalysees.toLocaleString('fr-FR') }}</span>
          <span>Modifiées : {{ classificationResult.lignesModifiees.toLocaleString('fr-FR') }}</span>
          <span>Inchangées : {{ classificationResult.lignesInchangees.toLocaleString('fr-FR') }}</span>
          <span>SCI : {{ classificationResult.lignesSci.toLocaleString('fr-FR') }}</span>
          <span>EMR : {{ classificationResult.lignesEmr.toLocaleString('fr-FR') }}</span>
          <span>TRN : {{ classificationResult.lignesTrn.toLocaleString('fr-FR') }}</span>
          <span>Inconnues : {{ classificationResult.lignesInconnues.toLocaleString('fr-FR') }}</span>
        </div>

        <div
          v-if="card.fileType === 'MVTSTOTRSVCR' && mvtSansEntreeOf"
          class="mt-3 flex items-start gap-2 rounded-xl border border-rose-200 bg-rose-50 p-3 text-xs font-semibold leading-5 text-rose-800"
        >
          <AlertTriangle class="mt-0.5 h-4 w-4 shrink-0" />
          {{ AUCUNE_ENTREE_OF_MESSAGE }}
        </div>

        <div class="mt-4">
          <div class="rounded-xl border border-slate-100 bg-slate-50/70 p-4">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <h4 class="text-xs font-black uppercase tracking-wider text-slate-800">Dernier import</h4>
              <span
                class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[10px] font-black"
                :class="getX3ExcelStatusClass(sourceStatuses[card.fileType].status)"
              >
                <CheckCircle2 v-if="sourceStatuses[card.fileType].status === 'Importé'" class="h-3.5 w-3.5" />
                <AlertTriangle v-else-if="sourceStatuses[card.fileType].status === 'Erreur'" class="h-3.5 w-3.5" />
                {{ sourceStatuses[card.fileType].status }}
              </span>
            </div>

            <div class="mt-3 grid grid-cols-1 gap-2 text-[10px] font-bold text-slate-500 sm:grid-cols-3">
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Fichier</span>
                <span class="break-words text-slate-800">{{ sourceStatuses[card.fileType].fileName || '—' }}</span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Dernier import</span>
                <span class="text-slate-800">{{ formatX3ImportDate(sourceStatuses[card.fileType].importedAt) }}</span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Profil</span>
                <span class="text-slate-800">{{ sourceStatuses[card.fileType].profile || '—' }}</span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Lignes lues</span>
                <span class="text-slate-800">{{ formatOptionalCount(sourceStatuses[card.fileType].rowsRead) }}</span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Lignes valides</span>
                <span class="text-slate-800">{{ formatOptionalCount(sourceStatuses[card.fileType].rowCount) }}</span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Erreurs de lecture</span>
                <span class="text-slate-800">{{ formatOptionalCount(sourceStatuses[card.fileType].rowsRejected) }}</span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Insérées</span>
                <span class="text-slate-800">{{ formatOptionalCount(sourceStatuses[card.fileType].rowsInserted) }}</span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Mises à jour</span>
                <span class="text-slate-800">{{ formatOptionalCount(sourceStatuses[card.fileType].rowsUpdated) }}</span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Inchangées</span>
                <span class="text-slate-800">{{ formatOptionalCount(sourceStatuses[card.fileType].rowsIgnored) }}</span>
              </div>

              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Archive</span>
                <button
                  v-if="sourceStatuses[card.fileType].hasArchivedFile"
                  class="inline-flex items-center gap-1 text-blue-700 hover:text-blue-900"
                  type="button"
                  @click="handleDownloadArchive(sourceStatuses[card.fileType])"
                >
                  <Download class="h-3.5 w-3.5" />
                  Télécharger
                </button>
                <span v-else class="text-slate-800">Aucun fichier archivé pour cet ancien import</span>
              </div>
              <div
                v-if="sourceStatuses[card.fileType].error"
                class="rounded-lg border border-rose-100 bg-rose-50 p-2 text-rose-700 sm:col-span-3"
              >
                {{ sourceStatuses[card.fileType].error }}
              </div>
            </div>
          </div>
        </div>

        <!-- Tableau des lignes importées YRJMCH -->
        <div v-if="card.fileType === 'YRJMCH'" class="mt-4 border-t border-slate-100 pt-4">
          <div class="flex flex-wrap items-center justify-between gap-3 mb-3">
            <div class="flex items-center gap-2">
              <Table class="h-4 w-4 text-blue-900" />
              <h4 class="text-xs font-black uppercase tracking-wider text-slate-900">
                Lignes importées YRJMCH
                <span v-if="yrjmchTotal > 0" class="ml-1 font-normal text-slate-400">({{ yrjmchTotal.toLocaleString('fr-FR') }} lignes)</span>
              </h4>
            </div>
            <div class="flex flex-wrap items-center gap-2">
              <div class="relative min-w-[110px]">
                <Search class="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-slate-400" />
                <input
                  v-model="yrjmchSearchOF"
                  type="text"
                  placeholder="N° OF..."
                  class="w-full rounded-lg border border-slate-200 bg-white py-1.5 pl-8 pr-2 text-xs font-semibold text-slate-800 placeholder-slate-400 focus:border-blue-500 focus:outline-none"
                  @input="yrjmchPage = 1; fetchYrjmchData()"
                />
              </div>
              <div class="relative min-w-[110px]">
                <input
                  v-model="yrjmchSearchArticle"
                  type="text"
                  placeholder="Article..."
                  class="w-full rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-800 placeholder-slate-400 focus:border-blue-500 focus:outline-none"
                  @input="yrjmchPage = 1; fetchYrjmchData()"
                />
              </div>
              <select
                v-model="yrjmchFilterProcessus"
                class="rounded-lg border border-slate-200 bg-white px-2 py-1.5 text-xs font-semibold text-slate-800 focus:border-blue-500 focus:outline-none"
                @change="yrjmchPage = 1; fetchYrjmchData()"
              >
                <option value="">Tous processus</option>
                <option value="SCI">SCI</option>
                <option value="EMR">EMR</option>
                <option value="TRN">TRN</option>
                <option value="CTRL">CTRL</option>
                <option value="Inconnue">Inconnue</option>
              </select>
            </div>
          </div>

          <div class="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-2xs">
            <table class="w-full text-left text-xs font-semibold text-slate-700">
              <thead class="bg-slate-50 text-[10px] font-black uppercase text-slate-500">
                <tr>
                  <th class="px-3 py-2">Date</th>
                  <th class="px-3 py-2">N° OF</th>
                  <th class="px-3 py-2">Article</th>
                  <th class="px-3 py-2">Poste</th>
                  <th class="px-3 py-2">Processus</th>
                  <th class="px-3 py-2 text-right">Qté TOTALE</th>
                  <th class="px-3 py-2 text-right">Qté Rebut</th>
                  <th class="px-3 py-2 text-right">Qté Nette</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-if="yrjmchLoading">
                  <td colspan="8" class="px-3 py-6 text-center text-xs font-medium text-slate-400">
                    Chargement des rapports machine...
                  </td>
                </tr>
                <tr v-else-if="yrjmchError">
                  <td colspan="8" class="px-3 py-6 text-center text-xs font-medium text-slate-400">
                    Impossible de charger les données YRJMCH : {{ yrjmchError }}
                  </td>
                </tr>
                <tr v-else-if="!yrjmchItems.length">
                  <td colspan="8" class="px-3 py-6 text-center text-xs font-medium text-slate-400">
                    <template v-if="sourceStatuses.YRJMCH.status === 'Importé' && (sourceStatuses.YRJMCH.rowsRead ?? 0) > 0">
                      L'import est enregistré mais aucune donnée métier persistée n'a été retrouvée.
                    </template>
                    <template v-else>Aucune donnée YRJMCH n'a encore été importée.</template>
                  </td>
                </tr>
                <tr v-for="item in yrjmchItems" :key="item.id" class="hover:bg-slate-50/60">
                  <td class="px-3 py-2 font-mono text-[11px] text-slate-600">{{ item.dateProduction }}</td>
                  <td class="px-3 py-2 font-bold text-slate-900">{{ item.numeroOF }}</td>
                  <td class="px-3 py-2">
                    <span class="block font-bold text-blue-900">{{ item.codeArticle }}</span>
                    <span v-if="item.designationArticle" class="block max-w-[130px] truncate text-[10px] font-medium text-slate-400">
                      {{ item.designationArticle }}
                    </span>
                  </td>
                  <td class="px-3 py-2 font-mono text-slate-800">
                    {{ item.codePoste }}
                    <span v-if="item.numeroOperation" class="text-[10px] text-slate-400">({{ item.numeroOperation }})</span>
                  </td>
                  <td class="px-3 py-2">
                    <span
                      class="inline-flex rounded-full border px-2 py-0.5 text-[9px] font-black uppercase"
                      :class="{
                        'border-emerald-200 bg-emerald-50 text-emerald-800': item.processus === 'SCI',
                        'border-blue-200 bg-blue-50 text-blue-800': item.processus === 'EMR',
                        'border-purple-200 bg-purple-50 text-purple-800': item.processus === 'TRN',
                        'border-amber-200 bg-amber-50 text-amber-800': item.processus === 'CTRL',
                        'border-slate-200 bg-slate-100 text-slate-600': item.processus === 'Inconnue'
                      }"
                    >
                      {{ item.processus }}
                    </span>
                  </td>
                  <td class="px-3 py-2 text-right font-mono font-bold text-slate-900">{{ item.quantiteTotale.toLocaleString('fr-FR') }}</td>
                  <td class="px-3 py-2 text-right font-mono text-rose-600">{{ item.quantiteRebut > 0 ? item.quantiteRebut.toLocaleString('fr-FR') : '0' }}</td>
                  <td class="px-3 py-2 text-right font-mono font-bold text-emerald-700">{{ item.quantiteNette.toLocaleString('fr-FR') }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div v-if="yrjmchTotal > 0" class="mt-3 flex items-center justify-between gap-2 text-xs font-semibold text-slate-500">
            <span>Page {{ yrjmchPage }} / {{ Math.ceil(yrjmchTotal / yrjmchPageSize) }}</span>
            <div class="flex items-center gap-1">
              <button
                type="button"
                class="inline-flex items-center rounded-lg border border-slate-200 bg-white p-1 hover:bg-slate-50 disabled:opacity-40"
                :disabled="yrjmchPage <= 1 || yrjmchLoading"
                @click="yrjmchPage--; fetchYrjmchData()"
              >
                <ChevronLeft class="h-4 w-4" />
              </button>
              <button
                type="button"
                class="inline-flex items-center rounded-lg border border-slate-200 bg-white p-1 hover:bg-slate-50 disabled:opacity-40"
                :disabled="yrjmchPage * yrjmchPageSize >= yrjmchTotal || yrjmchLoading"
                @click="yrjmchPage++; fetchYrjmchData()"
              >
                <ChevronRight class="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>

        <!-- Tableau des mouvements MVTSTOTRSVCR -->
        <div v-else-if="card.fileType === 'MVTSTOTRSVCR'" class="mt-4 border-t border-slate-100 pt-4">
          <div class="flex flex-wrap items-center justify-between gap-3 mb-3">
            <div class="flex items-center gap-2">
              <Table class="h-4 w-4 text-blue-900" />
              <h4 class="text-xs font-black uppercase tracking-wider text-slate-900">
                Lignes d'Entrées OF MVTSTOTRSVCR
                <span v-if="mvtTotal > 0" class="ml-1 font-normal text-slate-400">({{ mvtTotal.toLocaleString('fr-FR') }} mouvements)</span>
              </h4>
            </div>
            <div class="flex flex-wrap items-center gap-2">
              <div class="relative min-w-[120px]">
                <Search class="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-slate-400" />
                <input
                  v-model="mvtSearchOF"
                  type="text"
                  placeholder="N° OF..."
                  class="w-full rounded-lg border border-slate-200 bg-white py-1.5 pl-8 pr-2 text-xs font-semibold text-slate-800 placeholder-slate-400 focus:border-blue-500 focus:outline-none"
                  @input="mvtPage = 1; fetchMvtData()"
                />
              </div>
              <div class="relative min-w-[120px]">
                <input
                  v-model="mvtSearchArticle"
                  type="text"
                  placeholder="Article..."
                  class="w-full rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-800 placeholder-slate-400 focus:border-blue-500 focus:outline-none"
                  @input="mvtPage = 1; fetchMvtData()"
                />
              </div>
            </div>
          </div>

          <div class="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-2xs">
            <table class="w-full text-left text-xs font-semibold text-slate-700">
              <thead class="bg-slate-50 text-[10px] font-black uppercase text-slate-500">
                <tr>
                  <th class="px-3 py-2">Imputation</th>
                  <th class="px-3 py-2">N° OF</th>
                  <th class="px-3 py-2">Article</th>
                  <th class="px-3 py-2">Désignation</th>
                  <th class="px-3 py-2 text-right">Qté signée</th>
                  <th class="px-3 py-2">Unité</th>
                  <th class="px-3 py-2">Transaction</th>
                  <th class="px-3 py-2">Nature</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-if="mvtLoading">
                  <td colspan="8" class="px-3 py-6 text-center text-xs font-medium text-slate-400">
                    Chargement des mouvements d'entrée OF...
                  </td>
                </tr>
                <tr v-else-if="mvtError">
                  <td colspan="8" class="px-3 py-6 text-center text-xs font-medium text-slate-400">
                    Impossible de charger les données MVTSTOTRSVCR : {{ mvtError }}
                  </td>
                </tr>
                <tr v-else-if="!mvtItems.length">
                  <td colspan="8" class="px-3 py-6 text-center text-xs font-medium text-slate-400">
                    <template v-if="sourceStatuses.MVTSTOTRSVCR.status === 'Importé' && (sourceStatuses.MVTSTOTRSVCR.rowsRead ?? 0) > 0">
                      L'import est enregistré mais aucune donnée métier persistée n'a été retrouvée.
                    </template>
                    <template v-else>Aucune donnée MVTSTOTRSVCR n'a encore été importée.</template>
                  </td>
                </tr>
                <tr v-for="item in mvtItems" :key="item.id" class="hover:bg-slate-50/60">
                  <td class="px-3 py-2 font-mono text-[11px] text-slate-600">{{ item.dateImputation }}</td>
                  <td class="px-3 py-2 font-bold text-slate-900">{{ item.numeroOF || '—' }}</td>
                  <td class="px-3 py-2 font-bold text-blue-900">{{ item.codeArticle || '—' }}</td>
                  <td class="px-3 py-2 max-w-[150px] truncate text-[11px] text-slate-600">{{ item.designation || '—' }}</td>
                  <td class="px-3 py-2 text-right font-mono font-bold text-slate-900">{{ item.quantiteSignee.toLocaleString('fr-FR') }}</td>
                  <td class="px-3 py-2 font-mono text-[11px] text-slate-500">{{ item.unite || '—' }}</td>
                  <td class="px-3 py-2 font-mono text-[11px] text-slate-600">{{ item.transaction || '—' }}</td>
                  <td class="px-3 py-2">
                    <span
                      class="inline-flex rounded-full border px-2 py-0.5 text-[9px] font-black uppercase"
                      :class="{
                        'border-emerald-200 bg-emerald-50 text-emerald-800': item.nature === 'EntreeOF',
                        'border-blue-200 bg-blue-50 text-blue-800': item.nature === 'SortieMP',
                        'border-slate-200 bg-slate-100 text-slate-600': item.nature === 'NonClassee'
                      }"
                    >
                      {{ item.nature }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div v-if="mvtTotal > 0" class="mt-3 flex items-center justify-between gap-2 text-xs font-semibold text-slate-500">
            <span>Page {{ mvtPage }} / {{ Math.ceil(mvtTotal / mvtPageSize) }}</span>
            <div class="flex items-center gap-1">
              <button
                type="button"
                class="inline-flex items-center rounded-lg border border-slate-200 bg-white p-1 hover:bg-slate-50 disabled:opacity-40"
                :disabled="mvtPage <= 1 || mvtLoading"
                @click="mvtPage--; fetchMvtData()"
              >
                <ChevronLeft class="h-4 w-4" />
              </button>
              <button
                type="button"
                class="inline-flex items-center rounded-lg border border-slate-200 bg-white p-1 hover:bg-slate-50 disabled:opacity-40"
                :disabled="mvtPage * mvtPageSize >= mvtTotal || mvtLoading"
                @click="mvtPage++; fetchMvtData()"
              >
                <ChevronRight class="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      </article>
    </div>

    <!-- Rapprochement des deux sources. -->
    <div class="rounded-2xl border border-slate-100 bg-white p-5 shadow-sm">
      <h2 class="text-sm font-black uppercase tracking-wider text-slate-900">{{ USAGE_BLOCK.title }}</h2>

      <div class="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div class="rounded-xl border border-slate-100 bg-slate-50/70 p-4">
          <h3 class="text-xs font-black uppercase tracking-wider text-blue-900">{{ USAGE_BLOCK.yrjmch.title }}</h3>
          <ul class="mt-2 space-y-1">
            <li
              v-for="item in USAGE_BLOCK.yrjmch.items"
              :key="item"
              class="text-xs font-semibold leading-5 text-slate-600"
            >
              • {{ item }}
            </li>
          </ul>
        </div>
        <div class="rounded-xl border border-slate-100 bg-slate-50/70 p-4">
          <h3 class="text-xs font-black uppercase tracking-wider text-blue-900">{{ USAGE_BLOCK.mvt.title }}</h3>
          <ul class="mt-2 space-y-1">
            <li v-for="item in USAGE_BLOCK.mvt.items" :key="item" class="text-xs font-semibold leading-5 text-slate-600">
              • {{ item }}
            </li>
          </ul>
        </div>
      </div>

      <p class="mt-4 rounded-lg bg-blue-50/70 px-3 py-2 text-xs font-bold text-blue-900">
        Clé de rapprochement : <span class="font-mono">{{ USAGE_BLOCK.cle }}</span>
      </p>

      <div class="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div v-for="formule in FORMULES" :key="formule.bilan" class="rounded-xl border border-slate-100 p-4">
          <h3 class="text-xs font-black uppercase tracking-wider text-slate-800">{{ formule.bilan }}</h3>
          <ul class="mt-2 space-y-1">
            <li
              v-for="ligne in formule.lignes"
              :key="ligne"
              class="font-mono text-[11px] font-semibold leading-5 text-slate-600"
            >
              {{ ligne }}
            </li>
          </ul>
        </div>
      </div>

      <p class="mt-3 text-[10px] font-semibold text-slate-400">
        Ces formules sont affichées à titre explicatif. Les valeurs correspondantes sont calculées par le backend et
        ne sont jamais recalculées par cet écran.
      </p>
    </div>
  </div>
</template>
