<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { AlertTriangle, CheckCircle2, FileSpreadsheet, RefreshCw, Upload } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import { useAppToast } from '@/composables/useAppToast';
import {
  x3ExcelImportService,
  type X3BilanType,
  type X3ExcelFileType,
  type X3ImportedSourceStatus
} from '@/services/x3ExcelImportService';

const toast = useAppToast();

const emptyX3Status = (bilanType: X3BilanType, fileType: X3ExcelFileType): X3ImportedSourceStatus => ({
  bilanType,
  fileType,
  fileName: '',
  importedAt: '',
  rowCount: 0,
  status: 'Non importé'
});

const bilanTypes: { id: X3BilanType; label: string }[] = [
  { id: 'USINAGE_2', label: 'Bilan Usinage 2' },
  { id: 'UAP2', label: 'Bilan UAP2' }
];

const sourceCards: { fileType: X3ExcelFileType; title: string; subtitle: string; description: string }[] = [
  {
    fileType: 'MVTSTOTRSVCR',
    title: 'MVTSTOTRSVCR',
    subtitle: 'Mouvements stock X3',
    description: 'État X3 utilisé pour les mouvements de stock et les entrées OF.'
  },
  {
    fileType: 'YRJMCH',
    title: 'YRJMCH',
    subtitle: 'Rapport machine / OF X3',
    description: 'État X3 utilisé pour les quantités EMR, rebuts EMR et informations OF.'
  }
];

const x3ExcelStatuses = ref<Record<X3BilanType, Record<X3ExcelFileType, X3ImportedSourceStatus>>>({
  USINAGE_2: {
    MVTSTOTRSVCR: emptyX3Status('USINAGE_2', 'MVTSTOTRSVCR'),
    YRJMCH: emptyX3Status('USINAGE_2', 'YRJMCH')
  },
  UAP2: {
    MVTSTOTRSVCR: emptyX3Status('UAP2', 'MVTSTOTRSVCR'),
    YRJMCH: emptyX3Status('UAP2', 'YRJMCH')
  }
});

const isSyncingAll = ref(false);
const syncingFileType = ref<X3ExcelFileType | null>(null);

const refreshX3ExcelStatuses = () => {
  bilanTypes.forEach(({ id: bilanType }) => {
    sourceCards.forEach(({ fileType }) => {
      x3ExcelStatuses.value[bilanType][fileType] = x3ExcelImportService.getStatus(bilanType, fileType);
    });
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
  return new Intl.DateTimeFormat('fr-FR', {
    dateStyle: 'short',
    timeStyle: 'short'
  }).format(new Date(value));
};

const handleImportX3Excel = async (event: Event, fileType: X3ExcelFileType) => {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;

  try {
    await Promise.all(
      bilanTypes.map(({ id: bilanType }) =>
        fileType === 'MVTSTOTRSVCR'
          ? x3ExcelImportService.importMVTSTOTRSVCR(file, bilanType)
          : x3ExcelImportService.importYRJMCH(file, bilanType)
      )
    );
    refreshX3ExcelStatuses();
    toast.success(`${fileType} importé pour les états X3 Usinage 2 et UAP2.`);
  } catch (error) {
    refreshX3ExcelStatuses();
    toast.error(error instanceof Error ? error.message : 'Import Excel X3 invalide.');
  } finally {
    input.value = '';
  }
};

const handleSyncX3ExcelSource = async (fileType: X3ExcelFileType) => {
  const isImportedForBothBilans = bilanTypes.every(
    ({ id: bilanType }) => x3ExcelStatuses.value[bilanType][fileType].status === 'Importé'
  );

  if (!isImportedForBothBilans) {
    toast.error(`Importez d'abord le fichier ${fileType} pour les états X3 Usinage 2 et UAP2.`);
    return;
  }

  syncingFileType.value = fileType;
  try {
    refreshX3ExcelStatuses();
    toast.success(`${fileType} synchronisé localement pour les bilans Usinage 2 et UAP2.`);
  } finally {
    syncingFileType.value = null;
  }
};

const handleSyncBothX3ExcelSources = async () => {
  const hasSourcesForBothBilans = bilanTypes.every(({ id: bilanType }) =>
    x3ExcelImportService.hasBothSources(bilanType)
  );

  if (!hasSourcesForBothBilans) {
    refreshX3ExcelStatuses();
    toast.error('Importez MVTSTOTRSVCR et YRJMCH avant de synchroniser les états X3.');
    return;
  }

  isSyncingAll.value = true;
  try {
    refreshX3ExcelStatuses();
    toast.success('Synchronisation locale des états X3 terminée pour Usinage 2 et UAP2.');
  } finally {
    isSyncingAll.value = false;
  }
};

onMounted(refreshX3ExcelStatuses);
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="États X3"
      description="Importer, contrôler et synchroniser les fichiers X3 utilisés par les bilans Usinage 2 et UAP2."
    />

    <div class="rounded-2xl border border-slate-100 bg-white p-5 shadow-sm">
      <div class="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div class="flex items-start gap-3">
          <div class="rounded-xl bg-blue-50 p-3 text-blue-900">
            <FileSpreadsheet class="h-6 w-6" />
          </div>
          <div>
            <h2 class="text-sm font-black uppercase tracking-wider text-slate-900">Sources X3 communes</h2>
            <p class="mt-1 max-w-3xl text-xs font-semibold leading-5 text-slate-500">
              Cette page centralise les imports X3. Les bilans ne contiennent plus ces cartes pour garder uniquement les
              calculs et résultats.
            </p>
          </div>
        </div>

        <button
          class="inline-flex items-center justify-center gap-2 rounded-xl bg-blue-900 px-4 py-2.5 text-xs font-black uppercase tracking-wider text-white hover:bg-blue-800 disabled:opacity-50"
          :disabled="isSyncingAll"
          @click="handleSyncBothX3ExcelSources"
        >
          <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': isSyncingAll }" />
          Synchroniser les 2 fichiers X3
        </button>
      </div>
    </div>

    <div class="grid grid-cols-1 gap-5 xl:grid-cols-2">
      <article
        v-for="card in sourceCards"
        :key="card.fileType"
        class="rounded-2xl border border-slate-100 bg-white p-5 shadow-sm"
      >
        <div class="flex flex-col gap-4 border-b border-slate-100 pb-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 class="text-base font-black text-slate-950">{{ card.title }}</h3>
            <p class="text-xs font-bold text-blue-900">{{ card.subtitle }}</p>
            <p class="mt-2 text-xs font-medium leading-5 text-slate-500">{{ card.description }}</p>
          </div>

          <div class="flex flex-wrap gap-2">
            <label
              class="inline-flex cursor-pointer items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-2 text-[10px] font-black uppercase text-blue-900 hover:bg-slate-50"
            >
              <Upload class="h-3.5 w-3.5" />
              Importer Excel
              <input
                class="hidden"
                type="file"
                accept=".xlsx,.xls"
                @change="handleImportX3Excel($event, card.fileType)"
              />
            </label>
            <button
              class="inline-flex items-center gap-1.5 rounded-lg bg-slate-900 px-3 py-2 text-[10px] font-black uppercase text-white hover:bg-slate-800 disabled:opacity-50"
              :disabled="syncingFileType === card.fileType"
              @click="handleSyncX3ExcelSource(card.fileType)"
            >
              <RefreshCw class="h-3.5 w-3.5" :class="{ 'animate-spin': syncingFileType === card.fileType }" />
              Synchroniser
            </button>
          </div>
        </div>

        <div class="mt-4 grid grid-cols-1 gap-3">
          <div
            v-for="bilan in bilanTypes"
            :key="`${card.fileType}-${bilan.id}`"
            class="rounded-xl border border-slate-100 bg-slate-50/70 p-4"
          >
            <div class="flex flex-wrap items-center justify-between gap-2">
              <h4 class="text-xs font-black uppercase tracking-wider text-slate-800">{{ bilan.label }}</h4>
              <span
                class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[10px] font-black"
                :class="getX3ExcelStatusClass(x3ExcelStatuses[bilan.id][card.fileType].status)"
              >
                <CheckCircle2
                  v-if="x3ExcelStatuses[bilan.id][card.fileType].status === 'Importé'"
                  class="h-3.5 w-3.5"
                />
                <AlertTriangle
                  v-else-if="x3ExcelStatuses[bilan.id][card.fileType].status === 'Erreur'"
                  class="h-3.5 w-3.5"
                />
                {{ x3ExcelStatuses[bilan.id][card.fileType].status }}
              </span>
            </div>

            <div class="mt-3 grid grid-cols-1 gap-2 text-[10px] font-bold text-slate-500 sm:grid-cols-3">
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Fichier</span>
                <span class="break-words text-slate-800">
                  {{ x3ExcelStatuses[bilan.id][card.fileType].fileName || '—' }}
                </span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Dernier import</span>
                <span class="text-slate-800">
                  {{ formatX3ImportDate(x3ExcelStatuses[bilan.id][card.fileType].importedAt) }}
                </span>
              </div>
              <div class="rounded-lg border border-slate-100 bg-white p-2">
                <span class="block uppercase text-slate-400">Lignes</span>
                <span class="text-slate-800">{{ x3ExcelStatuses[bilan.id][card.fileType].rowCount }}</span>
              </div>
              <div
                v-if="x3ExcelStatuses[bilan.id][card.fileType].error"
                class="rounded-lg border border-rose-100 bg-rose-50 p-2 text-rose-700 sm:col-span-3"
              >
                {{ x3ExcelStatuses[bilan.id][card.fileType].error }}
              </div>
            </div>
          </div>
        </div>
      </article>
    </div>
  </div>
</template>
