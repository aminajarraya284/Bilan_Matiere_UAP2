<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { Download, FileArchive } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { formatNumber } from '@/utils/formatters';
import { importsHistoriqueService } from './importsHistorique.service';
import type { ImportBatchSummary, ImportSourceFile } from './importsHistorique.types';

const batches = ref<ImportBatchSummary[]>([]);
const filesByBatch = ref<Record<string, ImportSourceFile[]>>({});
const isLoading = ref(true);
const downloadingFileId = ref<string | null>(null);

const loadHistorique = async () => {
  isLoading.value = true;
  try {
    const diagnostic = await importsHistoriqueService.getDiagnostic();
    batches.value = diagnostic.derniersImportBatches;

    const filesEntries = await Promise.all(
      batches.value.map(async (batch) => [batch.id, await importsHistoriqueService.getFiles(batch.id)] as const)
    );
    filesByBatch.value = Object.fromEntries(filesEntries);
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const getStatutMeta = (statut: string) => {
  switch (statut) {
    case 'Termine':
      return { label: 'Terminé', className: 'bg-emerald-50 border-emerald-100 text-emerald-700' };
    case 'TermineAvecAvertissementArchive':
      return { label: 'Terminé, archive en erreur', className: 'bg-amber-50 border-amber-100 text-amber-700' };
    case 'Duplicate':
      return { label: 'Fichier identique', className: 'bg-blue-50 border-blue-100 text-blue-700' };
    case 'Echec':
      return { label: 'Échec', className: 'bg-rose-50 border-rose-100 text-rose-700' };
    case 'EnCours':
      return { label: 'En cours', className: 'bg-amber-50 border-amber-100 text-amber-700' };
    default:
      return { label: statut, className: 'bg-slate-50 border-slate-200 text-slate-700' };
  }
};

const formatBytes = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} o`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} Ko`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} Mo`;
};

const downloadFile = async (batchId: string, file: ImportSourceFile) => {
  downloadingFileId.value = file.id;
  try {
    await importsHistoriqueService.download(batchId, file.id, file.nomFichierOriginal);
  } catch (err) {
    console.error(err);
  } finally {
    downloadingFileId.value = null;
  }
};

onMounted(loadHistorique);
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Historique des imports"
      description="Derniers imports réels (GAMMEFAB, BOMARTPARENT, rapport machine, sorties X3) et téléchargement des fichiers sources archivés."
    />

    <div v-if="isLoading"><LoadingState /></div>

    <div v-else class="space-y-4">
      <div
        v-for="batch in batches"
        :key="batch.id"
        class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden"
      >
        <div class="px-5 py-4 flex flex-wrap items-center justify-between gap-3 border-b border-slate-50 bg-slate-50/20">
          <div>
            <div class="flex items-center gap-2">
              <span class="text-xs font-black text-slate-800 uppercase tracking-wider">{{ batch.typeImport }}</span>
              <span v-if="batch.profilImport" class="text-[10px] font-bold text-slate-400 font-mono">{{ batch.profilImport }}</span>
            </div>
            <p class="mt-1 text-[11px] font-semibold text-slate-500">
              {{ new Date(batch.dateCreationUtc).toLocaleString('fr-FR') }}
            </p>
          </div>
          <span
            class="px-2.5 py-0.5 rounded-full text-[10px] font-bold border"
            :class="getStatutMeta(batch.statut).className"
          >
            {{ getStatutMeta(batch.statut).label }}
          </span>
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-6 gap-3 p-5 border-b border-slate-50">
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">Lignes lues</span>
            <p class="mt-1 font-black font-mono text-slate-800">{{ formatNumber(batch.readCount, 0) }}</p>
          </div>
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">Insérées</span>
            <p class="mt-1 font-black font-mono text-slate-800">{{ formatNumber(batch.insertedCount, 0) }}</p>
          </div>
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">Modifiées</span>
            <p class="mt-1 font-black font-mono text-slate-800">{{ formatNumber(batch.updatedCount, 0) }}</p>
          </div>
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">Ignorées</span>
            <p class="mt-1 font-black font-mono text-slate-800">{{ formatNumber(batch.unchangedCount, 0) }}</p>
          </div>
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">Rejetées</span>
            <p class="mt-1 font-black font-mono text-slate-800">{{ formatNumber(batch.rejectedCount, 0) }}</p>
          </div>
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">Anomalies</span>
            <p class="mt-1 font-black font-mono" :class="batch.nombreAnomalies > 0 ? 'text-amber-700' : 'text-slate-800'">
              {{ formatNumber(batch.nombreAnomalies, 0) }}
            </p>
          </div>
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">Hash</span>
            <p class="mt-1 font-mono text-[10px] text-slate-400 truncate" :title="batch.hashFichierSha256">
              {{ batch.hashFichierSha256.slice(0, 16) }}…
            </p>
          </div>
        </div>

        <div class="p-5">
          <h4 class="text-[10px] font-black uppercase tracking-wider text-slate-400 mb-3">Fichiers sources archivés</h4>
          <div v-if="filesByBatch[batch.id]?.length" class="space-y-2">
            <div
              v-for="file in filesByBatch[batch.id]"
              :key="file.id"
              class="flex items-center justify-between gap-3 rounded-lg border border-slate-100 bg-slate-50/50 p-3"
            >
              <div class="flex items-center gap-2 min-w-0">
                <FileArchive class="w-4 h-4 text-blue-900 shrink-0" />
                <div class="min-w-0">
                  <p class="text-xs font-bold text-slate-800 truncate">{{ file.nomFichierOriginal }}</p>
                  <p class="text-[10px] font-semibold text-slate-400">
                    {{ formatBytes(file.tailleOctets) }} · {{ new Date(file.dateUploadUtc).toLocaleDateString('fr-FR') }}
                  </p>
                </div>
              </div>
              <button
                @click="downloadFile(batch.id, file)"
                :disabled="downloadingFileId === file.id"
                class="inline-flex items-center gap-1.5 bg-blue-900 hover:bg-blue-800 text-white font-bold text-[10px] uppercase tracking-wider py-2 px-3 rounded-lg cursor-pointer disabled:opacity-50 shrink-0"
              >
                <Download class="w-3.5 h-3.5" />
                Télécharger
              </button>
            </div>
          </div>
          <p v-else class="text-xs font-semibold text-slate-400">
            Aucun fichier source archivé pour cet import (import antérieur à l'activation de l'archivage).
          </p>
        </div>
      </div>

      <div v-if="!batches.length" class="bg-white rounded-xl border border-slate-100 shadow-sm p-10 text-center">
        <p class="text-sm font-bold text-slate-500">Aucun import trouvé</p>
      </div>
    </div>
  </div>
</template>
