<script setup lang="ts">
import { computed, ref, onMounted } from 'vue';
import { RefreshCw, Upload, Plus, CheckCircle2, FileSpreadsheet, AlertCircle, Database } from 'lucide-vue-next';
import { sortiesX3Service } from './sortiesX3.service';
import { SortieX3Mouvement } from './sortiesX3.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import FormField from '@/components/ui/FormField.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { formatNumber } from '@/utils/formatters';

const mouvements = ref<SortieX3Mouvement[]>([]);
const displayedMouvements = computed(() => mouvements.value.slice(0, 500));
const hiddenMouvementsCount = computed(() => Math.max(0, mouvements.value.length - displayedMouvements.value.length));
const isLoading = ref(true);
const isSyncing = ref(false);
const isUploading = ref(false);
const isLoadingReference = ref(false);
const showManualForm = ref(false);
const successMessage = ref('');

const formMove = ref({
  numeroOf: 'OF18100563',
  composant: 'M2130RP',
  qtePrevue: 1,
  qteRealisee: 0,
  qteRestante: 1,
  qteBesoin: 7,
  qteConsommee: 0
});

const loadMouvements = async () => {
  isLoading.value = true;
  try {
    mouvements.value = await sortiesX3Service.getMouvements();
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const handleSync = async () => {
  isSyncing.value = true;
  successMessage.value = '';
  try {
    const res = await sortiesX3Service.triggerSynchroniser();
    successMessage.value = `Synchronisation automatique terminée ! ${res.count} nouveaux mouvements intégrés de Sage X3.`;
    await loadMouvements();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  } finally {
    isSyncing.value = false;
  }
};

const handleFileUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement;
  const file = target.files?.[0];
  if (!file) return;

  isUploading.value = true;
  successMessage.value = '';
  try {
    const res = await sortiesX3Service.importExcel(file);
    successMessage.value = `Importation du fichier Excel terminée ! ${res.successCount} lignes de mouvements ajoutées avec succès.`;
    await loadMouvements();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  } finally {
    isUploading.value = false;
    target.value = '';
  }
};

const handleLoadReference = async () => {
  isLoadingReference.value = true;
  successMessage.value = '';
  try {
    const res = await sortiesX3Service.chargerReferenceIntegree();
    successMessage.value = `Référence ListeOFX3 chargée : ${res.successCount} mouvements X3 disponibles.`;
    await loadMouvements();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  } finally {
    isLoadingReference.value = false;
  }
};

const handleManualSubmit = async () => {
  try {
    const now = new Date();
    const qteConsommee = Number(formMove.value.qteConsommee) || 0;

    await sortiesX3Service.createMouvement({
      ...formMove.value,
      articleParent: formMove.value.numeroOf,
      dateImputation: now.toISOString().slice(0, 10),
      codeMatiere: formMove.value.composant,
      designation: formMove.value.composant,
      dateCreation: now.toISOString().slice(0, 10),
      heureCreation: now.toTimeString().slice(0, 8),
      utilisateur: 'Admin SOPAL',
      quantiteImportee: qteConsommee,
      unite: 'KG',
      transaction: formMove.value.numeroOf,
      typePiece: 'OF',
      lot: formMove.value.numeroOf,
      pieceOrigine: formMove.value.numeroOf,
      designationOrigine: 'LISTEOFX3',
      emplacement: 'X3',
      source: 'Manuel'
    });

    successMessage.value = 'Mouvement manuel enregistré avec succès !';
    showManualForm.value = false;
    formMove.value.qteConsommee = 0;
    await loadMouvements();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

onMounted(() => {
  loadMouvements();
});
</script>

<template>
  <div class="space-y-6">
    <PageHeader title="Synchronisation Sorties Magasin X3" description="Import, saisie et suivi des sorties X3." />

    <div
      v-if="successMessage"
      class="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-emerald-800 text-xs font-semibold flex items-center gap-3"
    >
      <CheckCircle2 class="w-5 h-5 text-emerald-500 shrink-0" />
      <span>{{ successMessage }}</span>
    </div>

    <div class="bg-blue-50/40 border border-blue-100 rounded-xl p-3 flex items-start gap-3 text-xs text-blue-950">
      <AlertCircle class="w-5 h-5 text-blue-600 shrink-0 mt-0.5" />
      <div class="space-y-2 font-medium">
        <p class="font-bold">Format X3 : LISTEOFX3</p>
        <div class="flex flex-wrap gap-2 text-[10px] font-bold">
          <span class="bg-white border border-blue-100 text-blue-800 px-2 py-1 rounded">Composant -> matière</span>
          <span class="bg-white border border-blue-100 text-blue-800 px-2 py-1 rounded"
            >Qté consommée -> quantité bilan</span
          >
          <span class="bg-white border border-blue-100 text-blue-800 px-2 py-1 rounded"
            >Date clôture -> date mouvement</span
          >
        </div>
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <div class="bg-white rounded-xl border border-slate-100 p-5 shadow-sm space-y-4 flex flex-col justify-between">
        <div class="space-y-1.5">
          <div class="flex items-center gap-2">
            <Database class="w-4.5 h-4.5 text-blue-900" />
            <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">API Sage X3</h4>
          </div>
          <p class="text-slate-400 text-[11px] leading-normal">Derniers mouvements X3.</p>
        </div>

        <button
          @click="handleSync"
          :disabled="isSyncing"
          class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-xl flex items-center justify-center gap-1.5 cursor-pointer active:scale-95 transition-all"
        >
          <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': isSyncing }" />
          {{ isSyncing ? 'Synchronisation...' : 'Synchroniser Sage X3' }}
        </button>
      </div>

      <div class="bg-white rounded-xl border border-slate-100 p-5 shadow-sm space-y-4 flex flex-col justify-between">
        <div class="space-y-1.5">
          <div class="flex items-center gap-2">
            <FileSpreadsheet class="w-4.5 h-4.5 text-emerald-600" />
            <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">Excel X3</h4>
          </div>
          <p class="text-slate-400 text-[11px] leading-normal">Fichier OF exporté de X3.</p>
        </div>

        <label
          class="w-full bg-slate-50 hover:bg-slate-100 border border-dashed border-slate-200 hover:border-emerald-200 text-slate-700 hover:text-emerald-700 font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-xl flex items-center justify-center gap-1.5 cursor-pointer transition-all"
        >
          <Upload class="w-4 h-4" :class="{ 'animate-pulse': isUploading }" />
          <span>{{ isUploading ? 'Importation...' : 'Téléverser un Excel' }}</span>
          <input type="file" accept=".xlsx, .xls" @change="handleFileUpload" class="hidden" :disabled="isUploading" />
        </label>

        <button
          @click="handleLoadReference"
          :disabled="isLoadingReference"
          class="w-full bg-emerald-50 hover:bg-emerald-100 border border-emerald-100 text-emerald-700 font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-xl flex items-center justify-center gap-1.5 cursor-pointer active:scale-95 transition-all disabled:opacity-60"
        >
          <FileSpreadsheet class="w-4 h-4" :class="{ 'animate-pulse': isLoadingReference }" />
          {{ isLoadingReference ? 'Chargement référence...' : 'Utiliser ListeOFX3 référence' }}
        </button>
      </div>

      <div class="bg-white rounded-xl border border-slate-100 p-5 shadow-sm space-y-4 flex flex-col justify-between">
        <div class="space-y-1.5">
          <div class="flex items-center gap-2">
            <Plus class="w-4.5 h-4.5 text-blue-900" />
            <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">Saisie X3</h4>
          </div>
          <p class="text-slate-400 text-[11px] leading-normal">Colonnes identiques au fichier.</p>
        </div>

        <button
          @click="showManualForm = !showManualForm"
          class="w-full bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-xl flex items-center justify-center gap-1.5 cursor-pointer active:scale-95 transition-all"
        >
          <Plus class="w-4 h-4" />
          {{ showManualForm ? 'Fermer le formulaire' : 'Saisir un mouvement' }}
        </button>
      </div>
    </div>

    <div v-if="showManualForm" class="bg-white rounded-xl border border-slate-100 p-6 shadow-sm space-y-6">
      <div class="flex items-center gap-2 border-b border-slate-50 pb-3">
        <Plus class="w-4 h-4 text-blue-900" />
        <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">Nouveau mouvement X3</h4>
      </div>

      <form @submit.prevent="handleManualSubmit" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <FormField id="m-of" label="OF" hint="Ordre de fabrication." required>
          <input
            type="text"
            v-model="formMove.numeroOf"
            class="bg-slate-50 border border-slate-100 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white font-mono"
            placeholder="OF18100563"
            required
          />
        </FormField>

        <FormField id="m-composant" label="Composant" hint="Code matière pour le bilan." required>
          <input
            type="text"
            v-model="formMove.composant"
            class="bg-slate-50 border border-slate-100 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono text-blue-900"
            placeholder="M3010L0"
            required
          />
        </FormField>

        <FormField id="m-qte-prevue" label="Qté prévue" required>
          <input
            type="number"
            v-model.number="formMove.qtePrevue"
            step="0.01"
            class="bg-slate-50 border border-slate-100 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono text-right"
            placeholder="0.00"
            required
          />
        </FormField>

        <FormField id="m-qte-realisee" label="Qté Réalisé" required>
          <input
            type="number"
            v-model.number="formMove.qteRealisee"
            step="0.01"
            class="bg-slate-50 border border-slate-100 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono text-right"
            placeholder="0.00"
            required
          />
        </FormField>

        <FormField id="m-qte-restante" label="Qté Restante" required>
          <input
            type="number"
            v-model.number="formMove.qteRestante"
            step="0.01"
            class="bg-slate-50 border border-slate-100 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono text-right"
            placeholder="0.00"
            required
          />
        </FormField>

        <FormField id="m-qte-besoin" label="Qté Besoin" required>
          <input
            type="number"
            v-model.number="formMove.qteBesoin"
            step="0.01"
            class="bg-slate-50 border border-slate-100 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono text-right"
            placeholder="0.00"
            required
          />
        </FormField>

        <FormField id="m-qte-consommee" label="Qté consommée" hint="Quantité utilisée dans le bilan." required>
          <input
            type="number"
            v-model.number="formMove.qteConsommee"
            step="0.01"
            class="bg-slate-50 border border-slate-100 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono text-blue-900 text-right"
            placeholder="0.00"
            required
          />
        </FormField>

        <div class="flex items-end lg:col-span-2">
          <button
            type="submit"
            class="w-full bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-lg flex items-center justify-center gap-1.5 cursor-pointer active:scale-95 transition-all"
          >
            <CheckCircle2 class="w-4 h-4" />
            Enregistrer le mouvement
          </button>
        </div>
      </form>
    </div>

    <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col">
      <div class="px-6 py-4 border-b border-slate-50 flex flex-wrap justify-between items-center gap-2 bg-slate-50/20">
        <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">
          Mouvements de sortie X3 - colonnes LISTEOFX3
        </h3>
        <span class="text-[10px] font-bold text-slate-400 font-mono"
          >Nombre d'enregistrements : {{ mouvements.length }}</span
        >
        <span
          v-if="hiddenMouvementsCount"
          class="text-[10px] font-bold text-amber-700 bg-amber-50 border border-amber-100 px-2 py-1 rounded"
        >
          Affichage limité aux 500 premières lignes, {{ hiddenMouvementsCount }} lignes restent utilisées en calcul.
        </span>
      </div>

      <div v-if="isLoading">
        <LoadingState />
      </div>

      <div v-else class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr
              class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
            >
              <th class="p-3">OF</th>
              <th class="p-3">Composant</th>
              <th class="p-3 text-right">Qté prévue</th>
              <th class="p-3 text-right">Qté Réalisé</th>
              <th class="p-3 text-right">Qté Restante</th>
              <th class="p-3 text-right">Qté Besoin</th>
              <th class="p-3 text-right text-blue-900">Qté consommée</th>
              <th class="p-3">Source</th>
              <th class="p-3">Statut</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 text-xs font-medium">
            <tr v-for="move in displayedMouvements" :key="move.id" class="hover:bg-slate-50/30 transition-colors">
              <td class="p-3 font-mono font-bold text-slate-800">
                {{ move.numeroOf || move.pieceOrigine || move.transaction }}
              </td>
              <td class="p-3 font-mono font-bold text-blue-900">{{ move.composant || move.codeMatiere }}</td>
              <td class="p-3 text-right font-mono">
                {{ move.qtePrevue !== undefined ? formatNumber(move.qtePrevue, 2) : '-' }}
              </td>
              <td class="p-3 text-right font-mono">
                {{ move.qteRealisee !== undefined ? formatNumber(move.qteRealisee, 2) : '-' }}
              </td>
              <td class="p-3 text-right font-mono">
                {{ move.qteRestante !== undefined ? formatNumber(move.qteRestante, 2) : '-' }}
              </td>
              <td class="p-3 text-right font-mono">
                {{ move.qteBesoin !== undefined ? formatNumber(move.qteBesoin, 2) : '-' }}
              </td>
              <td class="p-3 text-right font-mono font-black text-blue-900">
                {{ formatNumber(move.qteConsommee ?? move.quantiteUtilisee, 2) }}
              </td>
              <td class="p-3">
                <span
                  class="px-2 py-0.5 rounded text-[9px] font-black uppercase tracking-wider"
                  :class="{
                    'bg-slate-100 text-slate-700': move.source === 'Manuel',
                    'bg-blue-50 text-blue-700': move.source === 'API Sage X3',
                    'bg-emerald-50 text-emerald-700': move.source === 'Fichier Excel'
                  }"
                >
                  {{ move.source }}
                </span>
              </td>
              <td class="p-3">
                <BadgeStatus :status="move.statut" />
              </td>
            </tr>
            <tr v-if="!mouvements.length">
              <td colspan="9" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
                Aucun mouvement trouvé
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
