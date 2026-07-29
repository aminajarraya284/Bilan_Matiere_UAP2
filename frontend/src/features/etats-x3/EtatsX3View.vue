<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { AlertTriangle, CheckCircle2, Download, FileSpreadsheet, History, Info, RefreshCw, Upload } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import { useAppToast } from '@/composables/useAppToast';
import {
  x3ExcelImportService,
  type X3ClassificationResult,
  type X3ExcelFileType,
  type X3ImportedSourceStatus
} from '@/services/x3ExcelImportService';
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

const importingFileType = ref<X3ExcelFileType | null>(null);
const recalculatingClassification = ref(false);
const classificationResult = ref<X3ClassificationResult | null>(null);

const refreshX3ExcelStatuses = async () => {
  await x3ExcelImportService.refreshStatuses();
  sourceCards.forEach(({ fileType }) => {
    sourceStatuses.value[fileType] = x3ExcelImportService.getStatus('USINAGE_2', fileType);
  });
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
    if (fileType === 'MVTSTOTRSVCR') {
      await x3ExcelImportService.importMVTSTOTRSVCR(file, 'USINAGE_2');
    } else {
      await x3ExcelImportService.importYRJMCH(file, 'USINAGE_2');
    }
    await refreshX3ExcelStatuses();
    toast.success(`${fileType} importé pour les états X3 Usinage 2 et UAP2.`);
  } catch (error) {
    await refreshX3ExcelStatuses();
    toast.error(error instanceof Error ? error.message : 'Import Excel X3 invalide.');
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
  void refreshX3ExcelStatuses();
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
