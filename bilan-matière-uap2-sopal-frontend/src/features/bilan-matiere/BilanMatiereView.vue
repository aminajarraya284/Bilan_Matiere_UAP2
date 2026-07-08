<script setup lang="ts">
import type { Component } from 'vue';
import { ref, onMounted, computed } from 'vue';
import {
  Scale, Calculator, CheckCircle2, Lock, FileText, Send, Calendar, RefreshCw, Printer,
  Wrench, BarChart3, AlertTriangle, Eye, MessageSquare, Database
} from 'lucide-vue-next';
import { useAuthStore } from '@/features/auth/auth.store';
import { bilanService } from './bilan.service';
import { BilanMatiereReport, BilanUsinage2ParPiece, BilanUap2ParOF } from './bilan.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { formatNumber } from '@/utils/formatters';
import { useAppDialog } from '@/composables/useAppDialog';
import { BUSINESS_RULES } from '@/config/businessRules';

// Synchronisation imports
import { synchronisationService } from '@/features/synchronisation/synchronisation.service';
import { SourceSynchronisationX3, DonneeApplicationStatus } from '@/features/receptions-usinage2/receptions.types';

const authStore = useAuthStore();
const dialog = useAppDialog();
const isAdmin = computed(() => authStore.userRole === 'Admin');

// Tab state
type TabId = 'fonderie' | 'usinage2' | 'uap2';
const activeTab = ref<TabId>('fonderie');

const tabs: { id: TabId; label: string; icon: Component }[] = [
  { id: 'fonderie', label: 'Bilan Fonderie', icon: Scale },
  { id: 'usinage2', label: 'Bilan Usinage 2', icon: Wrench },
  { id: 'uap2', label: 'Bilan UAP2 Total', icon: BarChart3 }
];

// Fonderie state
const reports = ref<BilanMatiereReport[]>([]);
const isLoading = ref(true);
const isCalculating = ref(false);
const successMessage = ref('');

// Usinage state
const bilansUsinage = ref<BilanUsinage2ParPiece[]>([]);
const isLoadingUsinage = ref(true);

// UAP2 state
const bilansUap2 = ref<BilanUap2ParOF[]>([]);
const isLoadingUap2 = ref(true);

// Synchronisation state
const x3Sources = ref<SourceSynchronisationX3[]>([]);
const appDataStatus = ref<DonneeApplicationStatus[]>([]);
const isLoadingSync = ref(false);
const isMockSync = import.meta.env.VITE_USE_REAL_API !== 'true';

// Filters
const filterMois = ref<string>(BUSINESS_RULES.periodeParDefaut);
const filterDateDebut = ref<string>(BUSINESS_RULES.dateDebutParDefaut);
const filterDateFin = ref<string>(BUSINESS_RULES.dateFinParDefaut);
const filterAlliage = ref<string>('TOUS');

// Justification modal/drawer state (Fonderie)
const activeReportId = ref<number | null>(null);
const justificationText = ref('');

// Verification dialog (Usinage / UAP2)
const showVerifyDialog = ref(false);
const verifyTarget = ref<{ id: number; type: 'usinage' | 'uap2'; currentStatut: string; currentObs: string }>({
  id: 0,
  type: 'usinage',
  currentStatut: 'OK',
  currentObs: ''
});
const verifyStatut = ref('OK');
const verifyObservation = ref('');
const verifyJustification = ref('');

// ===== SYNCHRONISATION =====
const loadSyncStatus = async () => {
  try {
    x3Sources.value = await synchronisationService.getX3Status();
    appDataStatus.value = await synchronisationService.getAppStatusAll();
  } catch (err) {
    console.error(err);
  }
};

const handleSyncAll = async () => {
  isLoadingSync.value = true;
  try {
    x3Sources.value = await synchronisationService.syncAll();
    successMessage.value = 'Synchronisation complète de toutes les sources X3 réussie.';
    await loadSyncStatus();
    await loadBilans();
    await loadBilansUsinage();
    await loadBilansUap2();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  } finally {
    isLoadingSync.value = false;
  }
};

const handleSyncSource = async (sourceId: string) => {
  isLoadingSync.value = true;
  try {
    if (sourceId === 'emr') {
      x3Sources.value = await synchronisationService.syncEmr();
      successMessage.value = 'Synchronisation de l’état EMR X3 réussie.';
    } else if (sourceId === 'sorties-mp') {
      x3Sources.value = await synchronisationService.syncSortiesMp();
      successMessage.value = 'Synchronisation des sorties MP X3 réussie.';
    } else if (sourceId === 'rapport-machine') {
      x3Sources.value = await synchronisationService.syncRapportMachine();
      successMessage.value = 'Synchronisation du rapport journalier machine réussie.';
    } else if (sourceId === 'rebuts') {
      x3Sources.value = await synchronisationService.syncRebuts();
      successMessage.value = 'Synchronisation des rebuts EMR / SCI / TRN réussie.';
    }
    await loadSyncStatus();
    await loadBilans();
    await loadBilansUsinage();
    await loadBilansUap2();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  } finally {
    isLoadingSync.value = false;
  }
};

const handleRefreshStatus = async () => {
  isLoadingSync.value = true;
  try {
    await loadSyncStatus();
    await loadBilans();
    await loadBilansUsinage();
    await loadBilansUap2();
    successMessage.value = 'Données actualisées.';
    setTimeout(() => (successMessage.value = ''), 2000);
  } catch (err) {
    console.error(err);
  } finally {
    isLoadingSync.value = false;
  }
};

const formatSyncStatus = (status: string): string => {
  switch (status) {
    case 'Synchronise': return 'Synchronisé';
    case 'ASynchroniser': return 'À synchroniser';
    case 'Erreur': return 'Erreur';
    case 'Partiel': return 'Partiel';
    case 'NonDisponible': return 'Non disponible';
    default: return status;
  }
};

const getSyncStatusClass = (status: string): string => {
  switch (status) {
    case 'Synchronise': return 'text-emerald-600 bg-emerald-50 border-emerald-100';
    case 'ASynchroniser': return 'text-amber-600 bg-amber-50 border-amber-100';
    case 'Erreur': return 'text-rose-600 bg-rose-50 border-rose-100';
    case 'Partiel': return 'text-blue-600 bg-blue-50 border-blue-100';
    default: return 'text-slate-600 bg-slate-50 border-slate-100';
  }
};

const getAppStatusClass = (status: string): string => {
  switch (status) {
    case 'OK': return 'text-emerald-600 bg-emerald-50 border-emerald-100';
    case 'ACompleter': return 'text-amber-600 bg-amber-50 border-amber-100';
    case 'Erreur': return 'text-rose-600 bg-rose-50 border-rose-100';
    default: return 'text-slate-600 bg-slate-50 border-slate-100';
  }
};

// ===== FONDERIE =====
const loadBilans = async () => {
  isLoading.value = true;
  try {
    reports.value = await bilanService.getBilans(filterMois.value, filterDateDebut.value, filterDateFin.value);
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const handleCalculer = async () => {
  isCalculating.value = true;
  successMessage.value = '';
  try {
    await bilanService.calculerBilan(filterMois.value, filterAlliage.value, filterDateDebut.value, filterDateFin.value);
    successMessage.value = 'Calcul du bilan matière relancé avec succès.';
    await loadBilans();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  } finally {
    isCalculating.value = false;
  }
};

const handleValider = async (id: number) => {
  try {
    await bilanService.validerBilan(id);
    successMessage.value = 'Bilan matière validé avec succès.';
    await loadBilans();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const handleCloturer = async (id: number) => {
  if (
    await dialog.confirm(
      'Voulez-vous clôturer définitivement ce bilan ? Cette action verrouillera toutes les données du mois.',
      {
        title: 'Clôturer le bilan',
        tone: 'warning',
        confirmLabel: 'Clôturer'
      }
    )
  ) {
    try {
      await bilanService.cloturerBilan(id, 'fonderie');
      successMessage.value = 'Bilan clôturé et archivé.';
      await loadBilans();
      setTimeout(() => (successMessage.value = ''), 4000);
    } catch (err) {
      console.error(err);
    }
  }
};

const handleValiderAll = async () => {
  if (
    await dialog.confirm(
      'Voulez-vous valider tous les bilans sélectionnés pour cette période et ce type de matière ?',
      {
        title: 'Valider les bilans',
        tone: 'warning',
        confirmLabel: 'Valider'
      }
    )
  ) {
    try {
      const reportsToValidate = filteredReports.value.filter((r) => r.statut !== 'Valide' && r.statut !== 'Cloture');
      await Promise.all(reportsToValidate.map((r) => bilanService.validerBilan(r.id)));
      successMessage.value = 'Tous les bilans affichés ont été validés.';
      await loadBilans();
      setTimeout(() => (successMessage.value = ''), 4000);
    } catch (err) {
      console.error(err);
    }
  }
};

const handlePrint = () => {
  window.print();
};

const openJustifier = (report: BilanMatiereReport) => {
  activeReportId.value = report.id;
  justificationText.value = report.justification || '';
};

const submitJustification = async () => {
  if (activeReportId.value === null) return;
  try {
    await bilanService.justifierEcart(activeReportId.value, justificationText.value);
    successMessage.value = "Justification d'écart enregistrée.";
    activeReportId.value = null;
    await loadBilans();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const filteredReports = computed(() => {
  if (filterAlliage.value === 'TOUS') return reports.value;
  return reports.value.filter((r) => r.typeMatiere === filterAlliage.value);
});

const availableMatieres = computed(() => {
  return Array.from(new Set(reports.value.map((report) => report.typeMatiere))).sort();
});

const totalReport = computed(() => {
  const totals = filteredReports.value.reduce(
    (acc, report) => {
      acc.encoursDebut += report.encoursDebut;
      acc.sortiesX3 += report.sortiesX3;
      acc.inputTotal += report.inputTotal;
      acc.encoursFinReel += report.encoursFinReel;
      acc.dechetsFonderie += report.dechetsFonderie;
      acc.receptionsUsinage2 += report.receptionsUsinage2;
      acc.poidsNonReceptionne += report.poidsNonReceptionne || 0;
      acc.nombrePiecesNonReceptionnees += report.nombrePiecesNonReceptionnees || 0;
      acc.outputTotal += report.outputTotal;
      acc.ecartKg += report.ecartKg;
      return acc;
    },
    {
      encoursDebut: 0,
      sortiesX3: 0,
      inputTotal: 0,
      encoursFinReel: 0,
      dechetsFonderie: 0,
      receptionsUsinage2: 0,
      poidsNonReceptionne: 0,
      nombrePiecesNonReceptionnees: 0,
      outputTotal: 0,
      ecartKg: 0
    }
  );

  return {
    ...totals,
    tauxEcart: totals.inputTotal > 0 ? (totals.ecartKg / totals.inputTotal) * 100 : 0
  };
});

// ===== USINAGE 2 =====
const loadBilansUsinage = async () => {
  isLoadingUsinage.value = true;
  try {
    bilansUsinage.value = await bilanService.getBilansUsinage(filterMois.value, filterDateDebut.value, filterDateFin.value);
  } catch (err) {
    console.error(err);
  } finally {
    isLoadingUsinage.value = false;
  }
};

const totalUsinage = computed(() => {
  const items = bilansUsinage.value;
  const totalEmr = items.reduce((s, i) => s + i.quantiteEmrX3, 0);
  const totalRecep = items.reduce((s, i) => s + i.quantiteReceptionneeU2, 0);
  const totalNonRecep = items.reduce((s, i) => s + i.quantiteNonReceptionnee, 0);
  const totalPieces = items.reduce((s, i) => s + i.nombrePieces, 0);
  const ecartMoyen = totalPieces > 0 ? totalNonRecep / totalPieces : 0;
  const allOk = items.every((i) => i.statutControle === 'OK');
  return { totalEmr, totalRecep, totalNonRecep, totalPieces, ecartMoyen, statutGlobal: allOk ? 'OK' : 'AVerifier' };
});

// ===== UAP2 =====
const loadBilansUap2 = async () => {
  isLoadingUap2.value = true;
  try {
    bilansUap2.value = await bilanService.getBilansUap2(filterMois.value, filterDateDebut.value, filterDateFin.value);
  } catch (err) {
    console.error(err);
  } finally {
    isLoadingUap2.value = false;
  }
};

const totalUap2 = computed(() => {
  const items = bilansUap2.value;
  const totalSortieMP = items.reduce((s, i) => s + i.quantiteSortieMP, 0);
  const totalQteCtrl = items.reduce((s, i) => s + i.qteCtrl, 0);
  const totalRecep = items.reduce((s, i) => s + i.quantiteReceptionneeU2, 0);
  const totalNonRecep = items.reduce((s, i) => s + i.quantiteNonReceptionnee, 0);
  const totalRebuts = items.reduce((s, i) => s + i.totalRebutEmrSciTrn, 0);
  const totalEntreeSF = items.reduce((s, i) => s + i.entreeSF, 0);
  const totalOutput = items.reduce((s, i) => s + i.outputOF, 0);
  const ecartGlobal = items.reduce((s, i) => s + i.ecartBilanOF, 0);
  const tauxGlobal = totalSortieMP > 0 ? (ecartGlobal / totalSortieMP) * 100 : 0;
  return { totalSortieMP, totalQteCtrl, totalRecep, totalNonRecep, totalRebuts, totalEntreeSF, totalOutput, ecartGlobal, tauxGlobal };
});

// ===== VERIFY DIALOG =====
const openVerifyDialog = (id: number, type: 'usinage' | 'uap2', currentStatut: string, currentObs: string) => {
  verifyTarget.value = { id, type, currentStatut, currentObs };
  verifyStatut.value = currentStatut;
  verifyObservation.value = currentObs;
  verifyJustification.value = '';
  showVerifyDialog.value = true;
};

const submitVerification = async () => {
  try {
    await bilanService.verifierBilan(verifyTarget.value.id, verifyTarget.value.type, {
      statutControle: verifyStatut.value,
      observation: verifyObservation.value,
      justification: verifyJustification.value
    });
    successMessage.value = 'Vérification enregistrée avec succès.';
    showVerifyDialog.value = false;
    if (verifyTarget.value.type === 'usinage') await loadBilansUsinage();
    else await loadBilansUap2();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

// ===== TAB SWITCH =====
const switchTab = (tab: TabId) => {
  activeTab.value = tab;
  if (tab === 'usinage2' && bilansUsinage.value.length === 0) loadBilansUsinage();
  if (tab === 'uap2' && bilansUap2.value.length === 0) loadBilansUap2();
};

// ===== INIT =====
onMounted(() => {
  loadSyncStatus();
  loadBilans();
  loadBilansUsinage();
  loadBilansUap2();
});
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <PageHeader title="Bilan Matière" description="Calcul et validation des écarts de production UAP2." />

    <!-- Success notice -->
    <div
      v-if="successMessage"
      class="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-emerald-800 text-xs font-semibold flex items-center gap-3"
    >
      <CheckCircle2 class="w-5 h-5 text-emerald-500 shrink-0" />
      <span>{{ successMessage }}</span>
    </div>

    <!-- ===================== SOURCES X3 & DONNÉES APPLICATION (ADMIN ONLY) ===================== -->
    <div v-if="isAdmin" class="bg-white rounded-xl border border-slate-100 shadow-sm p-5 space-y-4">
      <div class="flex items-center justify-between border-b border-slate-50 pb-3 flex-wrap gap-2">
        <div class="flex items-center gap-2">
          <Database class="w-5 h-5 text-blue-900" />
          <h3 class="text-sm font-black text-slate-800 uppercase tracking-wider">Sources X3 & données application</h3>
        </div>

        <div class="flex gap-2 flex-wrap">
          <button
            @click="handleSyncAll"
            :disabled="isLoadingSync"
            class="px-3.5 py-2 bg-blue-900 hover:bg-blue-800 text-white rounded-lg text-xs font-bold flex items-center gap-1.5 cursor-pointer disabled:opacity-50 transition-all"
          >
            <RefreshCw class="w-3.5 h-3.5" :class="{ 'animate-spin': isLoadingSync }" />
            Synchroniser X3
          </button>
          <button
            @click="handleSyncSource('emr')"
            :disabled="isLoadingSync"
            class="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-[10px] font-extrabold uppercase tracking-wide cursor-pointer disabled:opacity-50"
          >
            Synchroniser état EMR
          </button>
          <button
            @click="handleSyncSource('sorties-mp')"
            :disabled="isLoadingSync"
            class="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-[10px] font-extrabold uppercase tracking-wide cursor-pointer disabled:opacity-50"
          >
            Synchroniser sorties MP
          </button>
          <button
            @click="handleSyncSource('rapport-machine')"
            :disabled="isLoadingSync"
            class="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-[10px] font-extrabold uppercase tracking-wide cursor-pointer disabled:opacity-50"
          >
            Synchroniser rapport machine
          </button>
          <button
            @click="handleSyncSource('rebuts')"
            :disabled="isLoadingSync"
            class="px-3 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-[10px] font-extrabold uppercase tracking-wide cursor-pointer disabled:opacity-50"
          >
            Synchroniser rebuts
          </button>
          <button
            @click="handleRefreshStatus"
            :disabled="isLoadingSync"
            class="px-3.5 py-2 bg-slate-700 hover:bg-slate-800 text-white rounded-lg text-xs font-bold flex items-center gap-1.5 cursor-pointer disabled:opacity-50 transition-all"
          >
            Actualiser les données
          </button>
        </div>
      </div>

      <!-- Banner if simulated sync -->
      <div v-if="isMockSync" class="bg-amber-50 text-amber-800 border border-amber-100 text-[10px] font-black uppercase tracking-wider rounded-lg px-3 py-2 flex items-center gap-2">
        <AlertTriangle class="w-4 h-4 text-amber-600" />
        Synchronisation simulée — backend non connecté
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <!-- Bloc 1 — Sources X3 -->
        <div class="rounded-xl border border-slate-100 p-4 space-y-3">
          <h4 class="text-[10px] font-black text-slate-400 uppercase tracking-wider border-b border-slate-50 pb-2">Bloc 1 — Sources X3 synchronisées</h4>
          
          <div class="space-y-2.5">
            <div v-for="source in x3Sources" :key="source.id" class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 p-2.5 rounded-lg bg-slate-50 border border-slate-100/50">
              <div class="space-y-0.5">
                <div class="text-xs font-black text-slate-800">{{ source.nom }}</div>
                <div class="text-[9px] font-bold text-slate-400 font-mono">
                  Dernière synchro : {{ source.derniereSynchronisation || '—' }} | Lignes: {{ source.nombreLignes || 0 }} | Erreurs: {{ source.nombreErreurs || 0 }}
                </div>
              </div>
              <div class="flex items-center gap-2">
                <span class="px-2 py-0.5 border rounded-full text-[9px] font-bold" :class="getSyncStatusClass(source.statut)">
                  {{ formatSyncStatus(source.statut) }}
                </span>
                <button
                  @click="handleSyncSource(source.id)"
                  :disabled="isLoadingSync"
                  class="px-2 py-1 bg-white hover:bg-slate-100 border border-slate-200 text-blue-900 rounded text-[9px] font-black uppercase tracking-wide cursor-pointer disabled:opacity-50"
                >
                  Synchroniser
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Bloc 2 — Données application -->
        <div class="rounded-xl border border-slate-100 p-4 space-y-3">
          <h4 class="text-[10px] font-black text-slate-400 uppercase tracking-wider border-b border-slate-50 pb-2">Bloc 2 — Données application</h4>

          <div class="space-y-2.5">
            <div v-for="data in appDataStatus" :key="data.id" class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 p-2.5 rounded-lg bg-slate-50 border border-slate-100/50">
              <div class="space-y-0.5">
                <div class="text-xs font-black text-slate-800">
                  {{ data.nom }} 
                  <span v-if="data.id === 'receptions-u2'" class="text-[9px] font-bold text-blue-700 bg-blue-50 px-1.5 py-0.5 rounded ml-1 border border-blue-100 uppercase tracking-wider">Origine: Application</span>
                </div>
                <div class="text-[9px] font-bold text-slate-400 font-mono">
                  Origine: {{ data.origine }} | Mise à jour : {{ data.derniereMiseAJour || '—' }} | Lignes: {{ data.nombreLignes || 0 }}
                </div>
              </div>
              <span class="px-2 py-0.5 border rounded-full text-[9px] font-bold self-start sm:self-center" :class="getAppStatusClass(data.statut)">
                {{ data.statut }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Tab Bar -->
    <div class="bg-white rounded-xl border border-slate-100 shadow-sm p-1.5 flex gap-1">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        @click="switchTab(tab.id)"
        class="flex-1 flex items-center justify-center gap-2 py-3 px-4 rounded-lg text-xs font-bold uppercase tracking-wider transition-all duration-200 cursor-pointer"
        :class="
          activeTab === tab.id
            ? 'bg-blue-900 text-white shadow-lg shadow-blue-900/20'
            : 'text-slate-500 hover:bg-slate-50 hover:text-slate-700'
        "
      >
        <component :is="tab.icon" class="w-4 h-4" />
        {{ tab.label }}
      </button>
    </div>

    <!-- ===================== ONGLET FONDERIE ===================== -->
    <template v-if="activeTab === 'fonderie'">
      <!-- Calculations equations banner -->
      <div class="bg-slate-900 border border-slate-800 rounded-xl p-4 text-white space-y-3">
        <div class="flex items-center gap-2.5 border-b border-slate-800 pb-3">
          <Calculator class="w-5 h-5 text-blue-400" />
          <h4 class="text-xs font-black uppercase tracking-wider">Règles de calcul — Bilan Fonderie</h4>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-3 text-xs">
          <div class="space-y-1">
            <span class="text-[10px] text-slate-400 font-bold uppercase">Entrée</span>
            <p class="font-bold text-blue-300 bg-slate-950 p-2.5 rounded-lg font-mono text-[11px]">Input Fonderie = Stock début Fonderie + Sorties magasin X3</p>
          </div>
          <div class="space-y-1">
            <span class="text-[10px] text-slate-400 font-bold uppercase">Sortie</span>
            <p class="font-bold text-emerald-300 bg-slate-950 p-2.5 rounded-lg font-mono text-[11px]">
              Output Fonderie = Stock fin Fonderie + Déchets Fonderie + Réceptions Usinage 2 + Poids non réceptionné si non inclus dans stock fin
            </p>
          </div>
          <div class="space-y-1">
            <span class="text-[10px] text-slate-400 font-bold uppercase">Écart</span>
            <p class="font-bold text-rose-300 bg-slate-950 p-2.5 rounded-lg font-mono text-[11px]">Écart Fonderie = Input Fonderie - Output Fonderie</p>
          </div>
        </div>

        <div class="pt-3 border-t border-slate-800/60 flex flex-wrap gap-2 text-[10px] text-slate-300 font-bold">
          <span class="bg-amber-900/30 text-amber-200 px-2.5 py-1 rounded-md flex items-center gap-1.5">
            <AlertTriangle class="w-3 h-3" />
            Le poids non réceptionné ne doit être ajouté que s’il n’est pas déjà inclus dans l’inventaire fin, afin d’éviter le double comptage.
          </span>
        </div>
      </div>

      <!-- Filters & Calculations Controls -->
      <div
        class="bg-white rounded-xl border border-slate-100 p-5 shadow-sm flex flex-col md:flex-row justify-between items-center gap-4"
      >
        <div class="flex flex-wrap items-center gap-4 w-full md:w-auto">
          <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
            <Calendar class="w-4 h-4 text-slate-400 shrink-0" />
            <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Date Début</span>
            <input
              type="date"
              v-model="filterDateDebut"
              class="bg-transparent border-none text-xs font-bold focus:outline-none cursor-pointer text-slate-700"
              @change="
                () => {
                  filterMois = filterDateDebut.slice(0, 7);
                  loadBilans();
                }
              "
            />
          </div>

          <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
            <Calendar class="w-4 h-4 text-slate-400 shrink-0" />
            <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Date Fin</span>
            <input
              type="date"
              v-model="filterDateFin"
              class="bg-transparent border-none text-xs font-bold focus:outline-none cursor-pointer text-slate-700"
              @change="loadBilans"
            />
          </div>

          <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
            <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Filtrer Type Matière</span>
            <select
              v-model="filterAlliage"
              class="bg-transparent border-none text-xs font-bold cursor-pointer focus:outline-none"
            >
              <option value="TOUS">TOUS LES TYPES MATIÈRE</option>
              <option v-for="matiere in availableMatieres" :key="matiere" :value="matiere">{{ matiere }}</option>
            </select>
          </div>
        </div>

        <div class="flex flex-wrap gap-2 items-center justify-end w-full md:w-auto">
          <button
            @click="handlePrint"
            class="bg-slate-700 hover:bg-slate-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-5 rounded-xl flex items-center gap-2 cursor-pointer active:scale-95 transition-all"
          >
            <Printer class="w-4 h-4" />
            Imprimer
          </button>
          <button
            @click="handleValiderAll"
            class="bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-5 rounded-xl flex items-center gap-2 cursor-pointer active:scale-95 transition-all"
          >
            <CheckCircle2 class="w-4 h-4" />
            Valider tout
          </button>
          <button
            @click="handleCalculer"
            :disabled="isCalculating"
            class="bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-6 rounded-xl flex items-center gap-1.5 cursor-pointer disabled:opacity-50 active:scale-95 transition-all w-full md:w-auto justify-center"
          >
            <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': isCalculating }" />
            {{ isCalculating ? 'Calcul en cours...' : 'Calculer le Bilan Fonderie' }}
          </button>
        </div>
      </div>

      <!-- Main Reports Table -->
      <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col">
        <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
            <Scale class="w-4 h-4 text-blue-900" />
            Résultats Consolidés Bilan Fonderie
          </h3>
          <span class="text-[10px] text-slate-400 font-bold font-mono"
            >Période : Du {{ filterDateDebut }} au {{ filterDateFin }}</span
          >
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
                <th class="p-3">Type Matière</th>
                <th class="p-3 text-right">Encours Début (kg)</th>
                <th class="p-3 text-right">Sorties X3 (kg)</th>
                <th class="p-3 text-right text-blue-900 bg-blue-50/10">INPUT Total (kg)</th>
                <th class="p-3 text-right">Encours Fin Réel (kg)</th>
                <th class="p-3 text-right">Déchets Fonderie (kg)</th>
                <th class="p-3 text-right">Réceptions U2 (kg)</th>
                <th v-if="isAdmin" class="p-3 text-right">Poids non réceptionné</th>
                <th class="p-3 text-right text-emerald-900 bg-emerald-50/10">OUTPUT Total (kg)</th>
                <th class="p-3 text-right text-rose-900">Écart (kg)</th>
                <th class="p-3 text-right">Taux Écart</th>
                <th class="p-3">Statut</th>
                <th class="p-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="report in filteredReports" :key="report.id" class="hover:bg-slate-50/30 transition-colors">
                <td class="p-3 font-bold text-slate-800 font-mono">{{ report.typeMatiere }}</td>
                <td class="p-3 text-right font-semibold font-mono">{{ formatNumber(report.encoursDebut, 0) }}</td>
                <td class="p-3 text-right font-semibold font-mono text-slate-600">
                  {{ formatNumber(report.sortiesX3, 0) }}
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-900 bg-blue-50/10">
                  {{ formatNumber(report.inputTotal, 0) }}
                </td>
                <td class="p-3 text-right font-semibold font-mono">{{ formatNumber(report.encoursFinReel, 0) }}</td>
                <td class="p-3 text-right font-semibold font-mono text-rose-600">
                  {{ formatNumber(report.dechetsFonderie, 0) }}
                </td>
                <td class="p-3 text-right font-semibold font-mono text-emerald-600">
                  {{ formatNumber(report.receptionsUsinage2, 0) }}
                </td>
                <td v-if="isAdmin" class="p-3 text-right font-semibold font-mono">
                  <div class="font-bold text-slate-700">{{ formatNumber(report.poidsNonReceptionne || 0, 2) }} kg</div>
                  <div class="text-[10px] text-slate-400">
                    {{ formatNumber(report.nombrePiecesNonReceptionnees || 0, 0) }} pcs
                  </div>
                </td>
                <td class="p-3 text-right font-black font-mono text-emerald-900 bg-emerald-50/10">
                  {{ formatNumber(report.outputTotal, 0) }}
                </td>
                <td
                  class="p-3 text-right font-black font-mono"
                  :class="report.ecartKg > BUSINESS_RULES.seuilEcartKgHighlight ? 'text-rose-600 animate-pulse' : 'text-slate-700'"
                >
                  {{ formatNumber(report.ecartKg, 0) }} kg
                </td>
                <td
                  class="p-3 text-right font-bold font-mono"
                  :class="report.tauxEcart > BUSINESS_RULES.seuilEcartPourcentage ? 'text-rose-600' : 'text-slate-600'"
                >
                  {{ formatNumber(report.tauxEcart, 2) }} %
                </td>
                <td class="p-3">
                  <BadgeStatus :status="report.statut" />
                </td>
                <td class="p-3 text-right">
                  <div class="inline-flex gap-1.5">
                    <button
                      @click="openJustifier(report)"
                      class="px-2 py-1 bg-slate-50 hover:bg-slate-100 border border-slate-200 text-slate-700 rounded text-[10px] font-extrabold cursor-pointer"
                      title="Ajouter justification d'écart"
                    >
                      Justifier
                    </button>
                    <button
                      v-if="report.statut === 'Brouillon' || report.statut === 'AvecEcart'"
                      @click="handleValider(report.id)"
                      class="px-2 py-1 bg-emerald-50 hover:bg-emerald-100 border border-emerald-100 text-emerald-700 rounded text-[10px] font-extrabold cursor-pointer"
                    >
                      Valider
                    </button>
                    <button
                      v-if="report.statut === 'Valide'"
                      @click="handleCloturer(report.id)"
                      class="px-2 py-1 bg-slate-900 hover:bg-slate-800 text-white rounded text-[10px] font-extrabold flex items-center gap-1 cursor-pointer"
                    >
                      <Lock class="w-3 h-3" />
                      Clôturer
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="!filteredReports.length">
                <td
                  :colspan="isAdmin ? 13 : 12"
                  class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider"
                >
                  Aucun bilan matière disponible pour cette sélection
                </td>
              </tr>
            </tbody>
            <tfoot>
              <tr
                class="bg-slate-100 border-t border-slate-200 text-[10px] text-slate-600 font-bold uppercase tracking-wider"
              >
                <td class="p-3">Total</td>
                <td class="p-3 text-right font-black font-mono">{{ formatNumber(totalReport.encoursDebut, 0) }}</td>
                <td class="p-3 text-right font-black font-mono">{{ formatNumber(totalReport.sortiesX3, 0) }}</td>
                <td class="p-3 text-right font-black font-mono text-blue-900 bg-blue-50/10">
                  {{ formatNumber(totalReport.inputTotal, 0) }}
                </td>
                <td class="p-3 text-right font-black font-mono">{{ formatNumber(totalReport.encoursFinReel, 0) }}</td>
                <td class="p-3 text-right font-black font-mono text-rose-600">
                  {{ formatNumber(totalReport.dechetsFonderie, 0) }}
                </td>
                <td class="p-3 text-right font-black font-mono text-emerald-600">
                  {{ formatNumber(totalReport.receptionsUsinage2, 0) }}
                </td>
                <td v-if="isAdmin" class="p-3 text-right font-black font-mono">
                  <div>{{ formatNumber(totalReport.poidsNonReceptionne, 2) }} kg</div>
                  <div class="text-[10px] text-slate-400">
                    {{ formatNumber(totalReport.nombrePiecesNonReceptionnees, 0) }} pcs
                  </div>
                </td>
                <td class="p-3 text-right font-black font-mono text-emerald-900 bg-emerald-50/10">
                  {{ formatNumber(totalReport.outputTotal, 0) }}
                </td>
                <td
                  class="p-3 text-right font-black font-mono"
                  :class="totalReport.ecartKg > BUSINESS_RULES.seuilEcartKgHighlight ? 'text-rose-600' : 'text-slate-700'"
                >
                  {{ formatNumber(totalReport.ecartKg, 0) }} kg
                </td>
                <td
                  class="p-3 text-right font-bold font-mono"
                  :class="totalReport.tauxEcart > BUSINESS_RULES.seuilEcartPourcentage ? 'text-rose-600' : 'text-slate-600'"
                >
                  {{ formatNumber(totalReport.tauxEcart, 2) }} %
                </td>
                <td class="p-3">—</td>
                <td class="p-3"></td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

      <!-- Justification Entry Form Box -->
      <div v-if="activeReportId !== null" class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm space-y-3">
        <div class="flex items-center gap-2 border-b border-slate-50 pb-3">
          <FileText class="w-4.5 h-4.5 text-blue-900" />
          <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">Justifier l'écart</h4>
        </div>

        <div class="space-y-2">
          <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider">Commentaire court</label>
          <textarea
            v-model="justificationText"
            rows="2"
            class="w-full bg-slate-50 border border-slate-100 rounded-lg p-2 text-xs font-semibold focus:bg-white"
            placeholder="Ex. perte four, inventaire, bavure..."
          ></textarea>

          <div class="flex justify-end gap-3">
            <button
              @click="activeReportId = null"
              class="px-4 py-2 border border-slate-200 text-slate-600 rounded-lg text-xs font-bold hover:bg-slate-50 cursor-pointer"
            >
              Annuler
            </button>
            <button
              @click="submitJustification"
              class="px-4 py-2 bg-blue-900 hover:bg-blue-800 text-white rounded-lg text-xs font-bold flex items-center gap-1.5 cursor-pointer"
            >
              <Send class="w-3.5 h-3.5" />
              Enregistrer la justification
            </button>
          </div>
        </div>
      </div>
    </template>

    <!-- ===================== ONGLET USINAGE 2 ===================== -->
    <template v-if="activeTab === 'usinage2'">
      <!-- Formules Usinage -->
      <div class="bg-slate-900 border border-slate-800 rounded-xl p-4 text-white space-y-3">
        <div class="flex items-center gap-2.5 border-b border-slate-800 pb-3">
          <Calculator class="w-5 h-5 text-blue-400" />
          <div>
            <h4 class="text-xs font-black uppercase tracking-wider">Bilan Matière Usinage 2 par pièce</h4>
            <p class="text-[10px] text-slate-400 mt-0.5">Contrôle des quantités EMR issues de X3, des réceptions Usinage 2 et des écarts par pièce.</p>
          </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
          <div class="space-y-1">
            <span class="text-[10px] text-slate-400 font-bold uppercase">Quantité non réceptionnée</span>
            <p class="font-bold text-amber-300 bg-slate-950 p-2.5 rounded-lg font-mono text-[11px]">Quantité non réceptionnée = Total Qté EMR issue de X3 - Total Qté réceptionnée Usinage 2 issue de l’application</p>
          </div>
          <div class="space-y-1">
            <span class="text-[10px] text-slate-400 font-bold uppercase">Écart par pièce</span>
            <p class="font-bold text-rose-300 bg-slate-950 p-2.5 rounded-lg font-mono text-[11px]">Écart par pièce = Quantité non réceptionnée / Nombre de pièces</p>
          </div>
        </div>
        <div class="pt-3 border-t border-slate-800/60 flex flex-wrap gap-2 text-[10px] text-slate-300 font-bold font-mono">
          <span class="bg-blue-900/40 text-blue-200 px-2.5 py-1 rounded-md flex items-center gap-1.5">
            <AlertTriangle class="w-3 h-3" />
            Le même contrôle est utilisé dans la page Réceptions U2 comme alerte Admin et dans le Bilan Usinage 2 comme indicateur de bilan.
          </span>
        </div>
      </div>

      <!-- Summary cards Usinage -->
      <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total Qté EMR issue de X3</p>
          <p class="mt-2 text-xl font-black text-slate-900">{{ formatNumber(totalUsinage.totalEmr, 2) }}</p>
          <p class="text-[10px] text-slate-400">kg</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total Qté réceptionnée U2</p>
          <p class="mt-2 text-xl font-black text-emerald-700">{{ formatNumber(totalUsinage.totalRecep, 2) }}</p>
          <p class="text-[10px] text-slate-400">kg</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Quantité non réceptionnée</p>
          <p class="mt-2 text-xl font-black" :class="totalUsinage.totalNonRecep > 0 ? 'text-amber-600' : 'text-slate-700'">{{ formatNumber(totalUsinage.totalNonRecep, 2) }}</p>
          <p class="text-[10px] text-slate-400">kg</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Nombre de pièces</p>
          <p class="mt-2 text-xl font-black text-slate-900">{{ formatNumber(totalUsinage.totalPieces, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Écart par pièce</p>
          <p class="mt-2 text-xl font-black text-rose-600">{{ formatNumber(totalUsinage.ecartMoyen, 4) }}</p>
          <p class="text-[10px] text-slate-400">kg / pièce</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Statut contrôle</p>
          <div class="mt-2">
            <BadgeStatus :status="totalUsinage.statutGlobal" />
          </div>
        </div>
      </div>

      <!-- Table Usinage -->
      <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
            <Wrench class="w-4 h-4 text-blue-900" />
            Tableau Bilan Usinage 2 par pièce
          </h3>
          <div class="flex gap-2">
            <button
              @click="handlePrint"
              class="bg-slate-700 hover:bg-slate-800 text-white font-bold text-[10px] uppercase py-1.5 px-3 rounded-lg flex items-center gap-1.5 cursor-pointer"
            >
              <Printer class="w-3.5 h-3.5" />
              Imprimer
            </button>
            <button
              @click="loadBilansUsinage"
              class="bg-blue-900 hover:bg-blue-800 text-white font-bold text-[10px] uppercase py-1.5 px-3 rounded-lg flex items-center gap-1.5 cursor-pointer"
            >
              <RefreshCw class="w-3.5 h-3.5" />
              Recalculer Usinage
            </button>
          </div>
        </div>

        <div v-if="isLoadingUsinage">
          <LoadingState />
        </div>

        <div v-else class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider">
                <th class="p-3">Article</th>
                <th class="p-3">Désignation article</th>
                <th class="p-3">Type matière</th>
                <th class="p-3 text-right">Nombre de pièces</th>
                <th class="p-3 text-right">Total Qté EMR issue de X3</th>
                <th class="p-3 text-right text-emerald-900 font-bold">Total Qté réceptionnée U2 application</th>
                <th class="p-3 text-right text-amber-800">Quantité non réceptionnée</th>
                <th class="p-3 text-right text-rose-900">Écart par pièce</th>
                <th class="p-3">Statut contrôle</th>
                <th class="p-3">Observation</th>
                <th class="p-3 text-right">Action</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="item in bilansUsinage" :key="item.id" class="hover:bg-slate-50/30 transition-colors">
                <td class="p-3 font-bold text-slate-800 font-mono">{{ item.codeArticle }}</td>
                <td class="p-3 text-slate-600 max-w-[180px] truncate">{{ item.designationArticle }}</td>
                <td class="p-3 font-mono text-slate-500">{{ item.codeTypeMatiere }}</td>
                <td class="p-3 text-right font-semibold font-mono">{{ formatNumber(item.nombrePieces, 0) }}</td>
                <td class="p-3 text-right font-semibold font-mono">{{ formatNumber(item.quantiteEmrX3, 2) }}</td>
                <td class="p-3 text-right font-semibold font-mono text-emerald-600">{{ formatNumber(item.quantiteReceptionneeU2, 2) }}</td>
                <td class="p-3 text-right font-bold font-mono" :class="item.quantiteNonReceptionnee > 0 ? 'text-amber-600' : 'text-slate-600'">
                  {{ formatNumber(item.quantiteNonReceptionnee, 2) }}
                </td>
                <td class="p-3 text-right font-bold font-mono" :class="Math.abs(item.ecartParPiece) > 0.001 ? 'text-rose-600' : 'text-slate-600'">
                  {{ formatNumber(item.ecartParPiece, 4) }}
                </td>
                <td class="p-3">
                  <BadgeStatus :status="item.statutControle" />
                </td>
                <td class="p-3 text-[10px] text-slate-500 max-w-[150px] truncate">{{ item.observation || '—' }}</td>
                <td class="p-3 text-right">
                  <button
                    v-if="isAdmin"
                    @click="openVerifyDialog(item.id, 'usinage', item.statutControle, item.observation || '')"
                    class="px-2.5 py-1.5 bg-blue-50 hover:bg-blue-100 border border-blue-100 text-blue-700 rounded-lg text-[10px] font-bold cursor-pointer flex items-center gap-1 ml-auto"
                  >
                    <Eye class="w-3.5 h-3.5" />
                    Vérifier
                  </button>
                </td>
              </tr>
              <tr v-if="!bilansUsinage.length">
                <td colspan="11" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
                  Aucun bilan usinage disponible pour cette sélection
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>

    <!-- ===================== ONGLET UAP2 TOTAL ===================== -->
    <template v-if="activeTab === 'uap2'">
      <!-- Formules UAP2 -->
      <div class="bg-slate-900 border border-slate-800 rounded-xl p-4 text-white space-y-3">
        <div class="flex items-center gap-2.5 border-b border-slate-800 pb-3">
          <Calculator class="w-5 h-5 text-blue-400" />
          <div>
            <h4 class="text-xs font-black uppercase tracking-wider">Bilan Matière UAP2 Total par OF</h4>
            <p class="text-[10px] text-slate-400 mt-0.5">Consolidation du bilan matière Fonderie et Usinage 2 par ordre de fabrication.</p>
          </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-3 text-xs">
          <div class="space-y-1">
            <span class="text-[10px] text-slate-400 font-bold uppercase">Input OF</span>
            <p class="font-bold text-blue-300 bg-slate-950 p-2.5 rounded-lg font-mono text-[11px]">Input OF = Quantité sortie MP</p>
          </div>
          <div class="space-y-1">
            <span class="text-[10px] text-slate-400 font-bold uppercase">Output OF</span>
            <p class="font-bold text-emerald-300 bg-slate-950 p-2.5 rounded-lg font-mono text-[11px]">Output OF = (Qté CTRL × Poids PHY) + Rebut EMR / SCI / TRN + Total Qté réceptionnée Usinage 2 + Rebut Usinage 2 + Entrée SF</p>
          </div>
          <div class="space-y-1">
            <span class="text-[10px] text-slate-400 font-bold uppercase">Écart bilan OF</span>
            <p class="font-bold text-rose-300 bg-slate-950 p-2.5 rounded-lg font-mono text-[11px]">Écart bilan OF = Input OF - Output OF</p>
          </div>
        </div>
        <div class="pt-3 border-t border-slate-800/60 flex flex-wrap gap-2 text-[10px] text-slate-300 font-bold">
          <span class="bg-amber-900/30 text-amber-200 px-2.5 py-1 rounded-md flex items-center gap-1.5">
            <AlertTriangle class="w-3 h-3" />
            Quantité non réceptionnée = Total Qté EMR issue de X3 - Total Qté réceptionnée Usinage 2 issue de l’application. Elle est affichée pour expliquer le décalage, mais ne doit pas être comptée deux fois dans le bilan UAP2.
          </span>
        </div>
      </div>

      <!-- Summary cards UAP2 -->
      <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-8 gap-3">
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total sortie MP</p>
          <p class="mt-1.5 text-lg font-black text-slate-900">{{ formatNumber(totalUap2.totalSortieMP, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total quantité contrôlée</p>
          <p class="mt-1.5 text-lg font-black text-slate-900">{{ formatNumber(totalUap2.totalQteCtrl, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total réceptionné U2</p>
          <p class="mt-1.5 text-lg font-black text-emerald-700">{{ formatNumber(totalUap2.totalRecep, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total non réceptionné</p>
          <p class="mt-1.5 text-lg font-black text-amber-600">{{ formatNumber(totalUap2.totalNonRecep, 2) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total rebuts</p>
          <p class="mt-1.5 text-lg font-black text-rose-600">{{ formatNumber(totalUap2.totalRebuts, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total entrée SF</p>
          <p class="mt-1.5 text-lg font-black text-slate-700">{{ formatNumber(totalUap2.totalEntreeSF, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Écart global OF</p>
          <p class="mt-1.5 text-lg font-black" :class="totalUap2.ecartGlobal < 0 ? 'text-rose-600' : 'text-slate-700'">{{ formatNumber(totalUap2.ecartGlobal, 2) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Taux écart global</p>
          <p class="mt-1.5 text-lg font-black" :class="Math.abs(totalUap2.tauxGlobal) > 3 ? 'text-rose-600' : 'text-slate-700'">{{ formatNumber(totalUap2.tauxGlobal, 2) }} %</p>
        </div>
      </div>

      <!-- Table UAP2 -->
      <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
            <BarChart3 class="w-4 h-4 text-blue-900" />
            Tableau Bilan UAP2 par OF
          </h3>
          <div class="flex gap-2">
            <button
              @click="handlePrint"
              class="bg-slate-700 hover:bg-slate-800 text-white font-bold text-[10px] uppercase py-1.5 px-3 rounded-lg flex items-center gap-1.5 cursor-pointer"
            >
              <Printer class="w-3.5 h-3.5" />
              Imprimer
            </button>
            <button
              @click="loadBilansUap2"
              class="bg-blue-900 hover:bg-blue-800 text-white font-bold text-[10px] uppercase py-1.5 px-3 rounded-lg flex items-center gap-1.5 cursor-pointer"
            >
              <RefreshCw class="w-3.5 h-3.5" />
              Recalculer UAP2
            </button>
          </div>
        </div>

        <div v-if="isLoadingUap2">
          <LoadingState />
        </div>

        <div v-else class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-[11px]">
            <thead>
              <tr class="bg-slate-50/50 border-b border-slate-100 text-[9px] text-slate-500 font-bold uppercase tracking-wider">
                <th class="p-2.5">OF</th>
                <th class="p-2.5">Article</th>
                <th class="p-2.5">Désignation article</th>
                <th class="p-2.5">Type matière</th>
                <th class="p-2.5 text-right font-bold">Quantité sortie MP</th>
                <th class="p-2.5 text-right">Qté CTRL</th>
                <th class="p-2.5 text-right">Poids PHY</th>
                <th class="p-2.5 text-right bg-blue-50/20">Qté CTRL × Poids PHY</th>
                <th class="p-2.5 text-right">Rebut EMR</th>
                <th class="p-2.5 text-right">Rebut SCI</th>
                <th class="p-2.5 text-right">Rebut TRN</th>
                <th class="p-2.5 text-right text-rose-800 font-bold">Total rebut EMR/SCI/TRN</th>
                <th class="p-2.5 text-right text-emerald-800 font-bold">Total Qté réceptionnée U2 application</th>
                <th class="p-2.5 text-right text-amber-800 font-bold">Quantité non réceptionnée</th>
                <th class="p-2.5 text-right">Écart par pièce</th>
                <th class="p-2.5 text-right">Rebut Usinage 2</th>
                <th class="p-2.5 text-right">Entrée SF</th>
                <th class="p-2.5 text-right bg-emerald-50/20 font-bold">Output OF</th>
                <th class="p-2.5 text-right text-rose-900 font-bold">Écart bilan OF</th>
                <th class="p-2.5 text-right">Taux écart OF</th>
                <th class="p-2.5">Statut</th>
                <th class="p-2.5">Observation</th>
                <th class="p-2.5 text-right font-bold">Action</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 font-medium">
              <tr v-for="item in bilansUap2" :key="item.id" class="hover:bg-slate-50/30 transition-colors">
                <td class="p-2.5 font-black text-blue-900 font-mono">{{ item.of }}</td>
                <td class="p-2.5 font-mono text-slate-700">{{ item.codeArticle }}</td>
                <td class="p-2.5 text-slate-500 max-w-[120px] truncate">{{ item.designationArticle }}</td>
                <td class="p-2.5 font-mono text-slate-400">{{ item.codeTypeMatiere }}</td>
                <td class="p-2.5 text-right font-semibold font-mono">{{ formatNumber(item.quantiteSortieMP, 0) }}</td>
                <td class="p-2.5 text-right font-semibold font-mono">{{ formatNumber(item.qteCtrl, 0) }}</td>
                <td class="p-2.5 text-right font-semibold font-mono">{{ formatNumber(item.poidsPhy, 2) }}</td>
                <td class="p-2.5 text-right font-black font-mono text-blue-800 bg-blue-50/20">{{ formatNumber(item.poidsControle, 2) }}</td>
                <td class="p-2.5 text-right font-mono text-rose-500">{{ formatNumber(item.rebutEMR, 2) }}</td>
                <td class="p-2.5 text-right font-mono text-rose-500">{{ formatNumber(item.rebutSCI, 2) }}</td>
                <td class="p-2.5 text-right font-mono text-rose-500">{{ formatNumber(item.rebutTRN, 2) }}</td>
                <td class="p-2.5 text-right font-bold font-mono text-rose-700">{{ formatNumber(item.totalRebutEmrSciTrn, 2) }}</td>
                <td class="p-2.5 text-right font-semibold font-mono text-emerald-600">{{ formatNumber(item.quantiteReceptionneeU2, 2) }}</td>
                <td class="p-2.5 text-right font-bold font-mono text-amber-600">{{ formatNumber(item.quantiteNonReceptionnee, 2) }}</td>
                <td class="p-2.5 text-right font-mono text-slate-500">{{ formatNumber(item.ecartParPiece, 4) }}</td>
                <td class="p-2.5 text-right font-mono text-rose-400">{{ formatNumber(item.rebutUsinage2, 2) }}</td>
                <td class="p-2.5 text-right font-mono text-slate-600">{{ formatNumber(item.entreeSF, 2) }}</td>
                <td class="p-2.5 text-right font-black font-mono text-emerald-800 bg-emerald-50/20">{{ formatNumber(item.outputOF, 2) }}</td>
                <td class="p-2.5 text-right font-black font-mono" :class="item.ecartBilanOF < 0 ? 'text-rose-600' : 'text-slate-700'">
                  {{ formatNumber(item.ecartBilanOF, 2) }}
                </td>
                <td class="p-2.5 text-right font-bold font-mono" :class="Math.abs(item.tauxEcartOF) > 3 ? 'text-rose-600' : 'text-slate-600'">
                  {{ formatNumber(item.tauxEcartOF, 2) }}
                </td>
                <td class="p-2.5">
                  <BadgeStatus :status="item.statut" />
                </td>
                <td class="p-2.5 text-[10px] text-slate-500 max-w-[100px] truncate">{{ item.observation || '—' }}</td>
                <td class="p-2.5 text-right">
                  <button
                    v-if="isAdmin"
                    @click="openVerifyDialog(item.id, 'uap2', item.statut, item.observation || '')"
                    class="px-2 py-1 bg-blue-50 hover:bg-blue-100 border border-blue-100 text-blue-700 rounded text-[10px] font-bold cursor-pointer flex items-center gap-1 ml-auto"
                  >
                    <Eye class="w-3 h-3" />
                    Vérifier
                  </button>
                </td>
              </tr>
              <tr v-if="!bilansUap2.length">
                <td colspan="23" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
                  Aucun bilan UAP2 disponible pour cette sélection
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>

    <!-- ===================== DIALOG VERIFICATION ===================== -->
    <Teleport to="body">
      <div v-if="showVerifyDialog" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="absolute inset-0 bg-slate-900/60 backdrop-blur-sm" @click="showVerifyDialog = false"></div>
        <div class="relative bg-white rounded-2xl border border-slate-100 shadow-2xl w-full max-w-lg mx-4 p-6 space-y-5">
          <div class="flex items-center gap-3 border-b border-slate-100 pb-4">
            <div class="bg-blue-50 rounded-xl p-2.5">
              <MessageSquare class="w-5 h-5 text-blue-700" />
            </div>
            <div>
              <h3 class="text-sm font-black text-slate-800">Vérifier le bilan</h3>
              <p class="text-[10px] text-slate-400 font-bold uppercase tracking-wider">Ajouter commentaire, justification et statut de contrôle</p>
            </div>
          </div>

          <div class="space-y-4">
            <div>
              <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-1.5">Statut de contrôle</label>
              <select v-model="verifyStatut" class="w-full bg-slate-50 border border-slate-200 rounded-lg px-3 py-2 text-xs font-bold focus:outline-none focus:ring-2 focus:ring-blue-200">
                <option value="OK">OK</option>
                <option value="AVerifier">À vérifier</option>
                <option value="Anomalie">Anomalie</option>
                <option value="Justifie">Justifié</option>
                <option value="Regularise">Régularisé</option>
                <option v-if="verifyTarget.type === 'uap2'" value="Cloture">Clôturé</option>
              </select>
            </div>

            <div>
              <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-1.5">Observation</label>
              <textarea
                v-model="verifyObservation"
                rows="2"
                class="w-full bg-slate-50 border border-slate-200 rounded-lg px-3 py-2 text-xs font-semibold focus:outline-none focus:ring-2 focus:ring-blue-200"
                placeholder="Observation sur le contrôle..."
              ></textarea>
            </div>

            <div>
              <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-1.5">Justification</label>
              <textarea
                v-model="verifyJustification"
                rows="2"
                class="w-full bg-slate-50 border border-slate-200 rounded-lg px-3 py-2 text-xs font-semibold focus:outline-none focus:ring-2 focus:ring-blue-200"
                placeholder="Justification de l'écart..."
              ></textarea>
            </div>
          </div>

          <div class="flex justify-end gap-3 pt-3 border-t border-slate-100">
            <button
              @click="showVerifyDialog = false"
              class="px-5 py-2.5 border border-slate-200 text-slate-600 rounded-xl text-xs font-bold hover:bg-slate-50 cursor-pointer"
            >
              Annuler
            </button>
            <button
              @click="submitVerification"
              class="px-5 py-2.5 bg-blue-900 hover:bg-blue-800 text-white rounded-xl text-xs font-bold flex items-center gap-2 cursor-pointer active:scale-95 transition-all"
            >
              <CheckCircle2 class="w-4 h-4" />
              Enregistrer
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
