<script setup lang="ts">
import type { Component } from 'vue';
import { ref, onMounted, computed } from 'vue';
import {
  Scale,
  CheckCircle2,
  Calendar,
  RefreshCw,
  Printer,
  Wrench,
  BarChart3,
  MessageSquare,
  Plus
} from 'lucide-vue-next';
import { useAuthStore } from '@/features/auth/auth.store';
import { bilanService } from './bilan.service';
import { BilanMatiereReport, BilanUsinage2ParPiece, BilanUap2ParOF } from './bilan.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import FormField from '@/components/ui/FormField.vue';
import { formatNumber } from '@/utils/formatters';
import { useAppDialog } from '@/composables/useAppDialog';
import { BUSINESS_RULES } from '@/config/businessRules';

const authStore = useAuthStore();
const dialog = useAppDialog();
const isAdmin = computed(() => authStore.userRole === 'Admin');
const canEnterRebutUsinage = computed(
  () => authStore.userRole === 'Admin' || authStore.userRole === 'ResponsableUsinage2'
);

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
const rebutUsinageForm = ref({
  numeroOF: '',
  nombrePiecesRebut: 0
});

// UAP2 state
const bilansUap2 = ref<BilanUap2ParOF[]>([]);
const isLoadingUap2 = ref(true);

// Filters
const filterMois = ref<string>(BUSINESS_RULES.periodeParDefaut);
const filterDateDebut = ref<string>(BUSINESS_RULES.dateDebutParDefaut);
const filterDateFin = ref<string>(BUSINESS_RULES.dateFinParDefaut);
const filterAlliage = ref<string>('TOUS');

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
    bilansUsinage.value = await bilanService.getBilansUsinage(
      filterMois.value,
      filterDateDebut.value,
      filterDateFin.value
    );
    if (
      !bilansUsinage.value.some((item) => item.numeroOF === rebutUsinageForm.value.numeroOF)
    ) {
      rebutUsinageForm.value.numeroOF = bilansUsinage.value[0]?.numeroOF ?? '';
    }
  } catch (err) {
    console.error(err);
  } finally {
    isLoadingUsinage.value = false;
  }
};

type BilanUsinage2Display = BilanUsinage2ParPiece & Partial<Record<string, number>>;

const readUsinageNumber = (item: BilanUsinage2ParPiece, primaryKey: string, legacyKey?: string): number => {
  const displayItem = item as BilanUsinage2Display;
  const value = displayItem[primaryKey] ?? (legacyKey ? displayItem[legacyKey] : undefined);
  return typeof value === 'number' && Number.isFinite(value) ? value : 0;
};

const getUsinageEmrPieces = (item: BilanUsinage2ParPiece): number =>
  readUsinageNumber(item, 'quantiteEmrPieces', 'quantiteEmrX3');

const getUsinageRebutEmrX3Pieces = (item: BilanUsinage2ParPiece): number =>
  readUsinageNumber(item, 'quantiteRebutEmrX3Pieces');

const getUsinageEmrNetteX3Pieces = (item: BilanUsinage2ParPiece): number => {
  const displayItem = item as BilanUsinage2Display;
  if (typeof displayItem.quantiteEmrNetteX3Pieces === 'number') {
    return displayItem.quantiteEmrNetteX3Pieces;
  }

  return getUsinageEmrPieces(item) - getUsinageRebutEmrX3Pieces(item);
};

const getUsinageReceptionneePieces = (item: BilanUsinage2ParPiece): number =>
  readUsinageNumber(item, 'quantiteReceptionneeU2Pieces', 'quantiteReceptionneeU2');

const getUsinageRebutUsinage2Pieces = (item: BilanUsinage2ParPiece): number =>
  readUsinageNumber(item, 'rebutUsinage2Pieces', 'rebutUsinage2Kg');

const getUsinageEntreeOfPieces = (item: BilanUsinage2ParPiece): number =>
  readUsinageNumber(item, 'entreeOfPieces', 'entreeOFParPiece');

const getUsinageResultatEcartPieces = (item: BilanUsinage2ParPiece): number => {
  const displayItem = item as BilanUsinage2Display;
  const savedValue =
    displayItem.resultatEcartPieces ?? displayItem.ecartUsinage2Pieces ?? displayItem.ecartPieces;

  if (typeof savedValue === 'number' && Number.isFinite(savedValue)) {
    return savedValue;
  }

  return getUsinageEntreeOfPieces(item) - getUsinageReceptionneePieces(item) - getUsinageRebutUsinage2Pieces(item);
};

const getUsinageNonReceptionneePieces = (item: BilanUsinage2ParPiece): number =>
  readUsinageNumber(item, 'quantiteNonReceptionneePieces', 'quantiteNonReceptionnee');

const totalUsinage = computed(() => {
  const items = bilansUsinage.value;
  const totalEmr = items.reduce((s, i) => s + getUsinageEmrPieces(i), 0);
  const totalRebutEmrX3 = items.reduce((s, i) => s + getUsinageRebutEmrX3Pieces(i), 0);
  const totalEmrNetX3 = items.reduce((s, i) => s + getUsinageEmrNetteX3Pieces(i), 0);
  const totalRecep = items.reduce((s, i) => s + getUsinageReceptionneePieces(i), 0);
  const totalNonRecep = items.reduce((s, i) => s + getUsinageNonReceptionneePieces(i), 0);
  const totalPieces = items.reduce((s, i) => s + getUsinageEmrPieces(i), 0);
  const totalRebutUsinage2 = items.reduce((s, i) => s + getUsinageRebutUsinage2Pieces(i), 0);
  const ecartMoyen = totalPieces > 0 ? totalNonRecep / totalPieces : 0;
  const allOk = items.every((i) => i.statutControle === 'OK');
  return {
    totalEmr,
    totalRebutEmrX3,
    totalEmrNetX3,
    totalRecep,
    totalNonRecep,
    totalPieces,
    totalRebutUsinage2,
    ecartMoyen,
    statutGlobal: allOk ? 'OK' : 'AVerifier'
  };
});

const selectedRebutUsinageBilan = computed(() =>
  bilansUsinage.value.find((item) => item.numeroOF === rebutUsinageForm.value.numeroOF)
);

const handleAddRebutUsinage2 = async () => {
  const bilan = selectedRebutUsinageBilan.value;
  if (!bilan || rebutUsinageForm.value.nombrePiecesRebut <= 0) {
    await dialog.alert('Sélectionnez un OF et saisissez un nombre de pièces rebut supérieur à 0.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  try {
    await bilanService.createRebutUsinage2({
      numeroOF: bilan.numeroOF,
      codeArticle: bilan.codeArticle,
      codeTypeMatiere: bilan.codeTypeMatiere as Parameters<typeof bilanService.createRebutUsinage2>[0]['codeTypeMatiere'],
      dateDeclaration: filterDateFin.value,
      nombrePiecesRebut: Math.trunc(rebutUsinageForm.value.nombrePiecesRebut),
      poidsRebutKg: 0,
      observation: 'Saisie depuis le bilan Usinage 2'
    });
    rebutUsinageForm.value.nombrePiecesRebut = 0;
    successMessage.value = 'Rebut Usinage 2 enregistré et intégré au bilan.';
    await Promise.all([loadBilansUsinage(), loadBilansUap2()]);
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

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

type BilanUap2Display = BilanUap2ParOF & Partial<Record<string, number>>;

const getUap2RebutUsinage2Pieces = (item: BilanUap2ParOF): number => {
  const displayItem = item as BilanUap2Display;
  const value = displayItem.rebutUsinage2Pieces ?? displayItem.rebutUsinage2 ?? displayItem.rebutUsinage2Kg;
  return typeof value === 'number' && Number.isFinite(value) ? value : 0;
};

const getUap2ResultatLabel = (item: BilanUap2ParOF): string => {
  if (item.statut === 'Cloture') return 'Clôturé';
  return item.statut === 'OK' ? 'À clôturer' : 'À vérifier';
};

const getUap2ResultatClass = (item: BilanUap2ParOF): string => {
  if (item.statut === 'Cloture') return 'bg-slate-100 text-slate-700 border-slate-200';
  if (item.statut === 'OK') return 'bg-emerald-50 text-emerald-700 border-emerald-200';
  return 'bg-amber-50 text-amber-700 border-amber-200';
};

const totalUap2 = computed(() => {
  const items = bilansUap2.value;
  const totalSortieMP = items.reduce((s, i) => s + i.quantiteSortieMPKg, 0);
  const totalQteCtrl = items.reduce((s, i) => s + i.quantiteControlePieces, 0);
  const totalRebuts = items.reduce((s, i) => s + getUap2RebutUsinage2Pieces(i), 0);
  const totalEntreeSF = items.reduce((s, i) => s + i.entreeSfNetKg, 0);
  const totalOutput = items.reduce((s, i) => s + i.outputOFKg, 0);
  const ecartGlobal = items.reduce((s, i) => s + i.ecartBilanOFKg, 0);
  const tauxGlobal = totalSortieMP > 0 ? (ecartGlobal / totalSortieMP) * 100 : 0;
  return {
    totalSortieMP,
    totalQteCtrl,
    totalRebuts,
    totalEntreeSF,
    totalOutput,
    ecartGlobal,
    tauxGlobal
  };
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
  loadBilans();
  loadBilansUsinage();
  loadBilansUap2();
});
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <PageHeader
      title="Bilan matière"
      description="Calculer, contrôler et valider les écarts de production de l’UAP2."
    />

    <!-- Success notice -->
    <div
      v-if="successMessage"
      class="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-emerald-800 text-xs font-semibold flex items-center gap-3"
    >
      <CheckCircle2 class="w-5 h-5 text-emerald-500 shrink-0" />
      <span>{{ successMessage }}</span>
    </div>

    <!-- Tab Bar -->
    <div class="app-tabs">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        @click="switchTab(tab.id)"
        class="app-tab flex-1"
        :class="activeTab === tab.id ? 'app-tab--active' : ''"
      >
        <component :is="tab.icon" class="w-4 h-4" />
        {{ tab.label }}
      </button>
    </div>

    <!-- ===================== ONGLET FONDERIE ===================== -->
    <template v-if="activeTab === 'fonderie'">
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
                <th class="p-3 text-right text-blue-900 bg-blue-50/20">OUTPUT Total (kg)</th>
                <th class="p-3 text-right text-rose-900">Écart (kg)</th>
                <th class="p-3 text-right">Taux Écart</th>
                <th class="p-3">Statut</th>
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
                <td class="p-3 text-right font-semibold font-mono text-blue-700">
                  {{ formatNumber(report.receptionsUsinage2, 0) }}
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-900 bg-blue-50/20">
                  {{ formatNumber(report.outputTotal, 0) }}
                </td>
                <td
                  class="p-3 text-right font-black font-mono"
                  :class="report.ecartKg > BUSINESS_RULES.seuilEcartKgHighlight ? 'text-rose-600' : 'text-slate-700'"
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
              </tr>
              <tr v-if="!filteredReports.length">
                <td
                  colspan="11"
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
                <td class="p-3 text-right font-black font-mono text-blue-700">
                  {{ formatNumber(totalReport.receptionsUsinage2, 0) }}
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-900 bg-blue-50/20">
                  {{ formatNumber(totalReport.outputTotal, 0) }}
                </td>
                <td
                  class="p-3 text-right font-black font-mono"
                  :class="
                    totalReport.ecartKg > BUSINESS_RULES.seuilEcartKgHighlight ? 'text-rose-600' : 'text-slate-700'
                  "
                >
                  {{ formatNumber(totalReport.ecartKg, 0) }} kg
                </td>
                <td
                  class="p-3 text-right font-bold font-mono"
                  :class="
                    totalReport.tauxEcart > BUSINESS_RULES.seuilEcartPourcentage ? 'text-rose-600' : 'text-slate-600'
                  "
                >
                  {{ formatNumber(totalReport.tauxEcart, 2) }} %
                </td>
                <td class="p-3">—</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

    </template>

    <!-- ===================== ONGLET USINAGE 2 ===================== -->
    <template v-if="activeTab === 'usinage2'">
      <div v-if="canEnterRebutUsinage" class="bg-white rounded-xl border border-slate-100 shadow-sm p-5 space-y-4">
        <div class="flex items-center gap-2 border-b border-slate-50 pb-3">
          <Wrench class="w-4 h-4 text-blue-900" />
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">Saisie Rebut Usinage 2</h3>
        </div>

        <form
          @submit.prevent="handleAddRebutUsinage2"
          class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 bg-slate-50/50 border border-slate-100 p-4 rounded-xl"
        >
          <FormField id="rebut-u2-of" label="OF" hint="Le rebut sera rattaché à cet OF." required>
            <select
              v-model="rebutUsinageForm.numeroOF"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
              required
            >
              <option value="" disabled>Sélectionner un OF</option>
              <option v-for="item in bilansUsinage" :key="item.bilanKey" :value="item.numeroOF">
                {{ item.numeroOF }}
              </option>
            </select>
          </FormField>

          <FormField id="rebut-u2-article" label="Article" hint="Article lié à l’OF sélectionné.">
            <input
              :value="selectedRebutUsinageBilan?.codeArticle || ''"
              type="text"
              readonly
              placeholder="Article"
              class="bg-slate-100 border border-slate-200 rounded-lg p-2 text-xs font-bold font-mono text-slate-700 w-full"
            />
          </FormField>

          <FormField
            id="rebut-u2-pieces"
            label="Rebut Usinage 2 (pièces)"
            hint="Nombre de pièces rebut utilisé dans le bilan."
            required
          >
            <input
              v-model.number="rebutUsinageForm.nombrePiecesRebut"
              type="number"
              min="1"
              step="1"
              placeholder="0"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold font-mono w-full"
              required
            />
          </FormField>

          <div class="flex items-end">
            <button
              type="submit"
              class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-lg flex items-center justify-center gap-1.5 cursor-pointer active:scale-95 transition-all"
            >
              <Plus class="w-4 h-4" />
              Enregistrer le rebut
            </button>
          </div>
        </form>
      </div>

      <!-- Summary cards Usinage -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total pièces réceptionnées U2</p>
          <p class="mt-2 text-xl font-black text-blue-800">{{ formatNumber(totalUsinage.totalRecep, 0) }}</p>
          <p class="text-[10px] text-slate-400">pièces</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Nombre de pièces</p>
          <p class="mt-2 text-xl font-black text-slate-900">{{ formatNumber(totalUsinage.totalPieces, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Rebut Usinage 2</p>
          <p class="mt-2 text-xl font-black text-rose-600">
            {{ formatNumber(totalUsinage.totalRebutUsinage2, 0) }}
          </p>
          <p class="text-[10px] text-slate-400">pièces</p>
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
              <tr
                class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
              >
                <th class="p-3">OF</th>
                <th class="p-3">Article</th>
                <th class="p-3">Désignation article</th>
                <th class="p-3 text-right text-blue-900 font-bold">Total nombre de pièces réceptionnées</th>
                <th class="p-3 text-right text-rose-800">Total rebut Usinage 2</th>
                <th class="p-3 text-right text-slate-800 font-bold">Total entrée OF par pièce</th>
                <th class="p-3 text-right text-slate-900 font-bold">Résultat : écart</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="item in bilansUsinage" :key="item.id" class="hover:bg-slate-50/30 transition-colors">
                <td class="p-3 font-black text-blue-900 font-mono whitespace-nowrap">{{ item.numeroOF }}</td>
                <td class="p-3 font-bold text-slate-800 font-mono">{{ item.codeArticle }}</td>
                <td class="p-3 text-slate-600 max-w-[180px] truncate">{{ item.designationArticle }}</td>
                <td class="p-3 text-right font-bold font-mono text-blue-700">
                  {{ formatNumber(getUsinageReceptionneePieces(item), 0) }}
                </td>
                <td class="p-3 text-right font-bold font-mono text-rose-600">
                  {{ formatNumber(getUsinageRebutUsinage2Pieces(item), 0) }}
                </td>
                <td class="p-3 text-right font-bold font-mono text-slate-700">
                  {{ formatNumber(getUsinageEntreeOfPieces(item), 0) }}
                </td>
                <td
                  class="p-3 text-right font-black font-mono"
                  :class="getUsinageResultatEcartPieces(item) === 0 ? 'text-emerald-700' : 'text-rose-700'"
                >
                  {{ formatNumber(getUsinageResultatEcartPieces(item), 0) }}
                </td>
              </tr>
              <tr v-if="!bilansUsinage.length">
                <td colspan="7" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
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
      <!-- Summary cards UAP2 -->
      <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total sortie MP</p>
          <p class="mt-1.5 text-lg font-black text-slate-900">{{ formatNumber(totalUap2.totalSortieMP, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total quantité contrôlée</p>
          <p class="mt-1.5 text-lg font-black text-slate-900">{{ formatNumber(totalUap2.totalQteCtrl, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total rebut Usinage 2</p>
          <p class="mt-1.5 text-lg font-black text-rose-600">{{ formatNumber(totalUap2.totalRebuts, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total entrée OF</p>
          <p class="mt-1.5 text-lg font-black text-slate-700">{{ formatNumber(totalUap2.totalEntreeSF, 0) }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Écart global OF</p>
          <p class="mt-1.5 text-lg font-black" :class="totalUap2.ecartGlobal < 0 ? 'text-rose-600' : 'text-slate-700'">
            {{ formatNumber(totalUap2.ecartGlobal, 2) }}
          </p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-3 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Taux écart global</p>
          <p
            class="mt-1.5 text-lg font-black"
            :class="Math.abs(totalUap2.tauxGlobal) > 3 ? 'text-rose-600' : 'text-slate-700'"
          >
            {{ formatNumber(totalUap2.tauxGlobal, 2) }} %
          </p>
        </div>
      </div>

      <!-- Table UAP2 -->
      <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
            <BarChart3 class="w-4 h-4 text-blue-900" />
            Bilan UAP2 par OF
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
              <tr
                class="bg-slate-50/50 border-b border-slate-100 text-[9px] text-slate-500 font-bold uppercase tracking-wider"
              >
                <th class="p-2.5">OF</th>
                <th class="p-2.5">Article</th>
                <th class="p-2.5">Désignation</th>
                <th class="p-2.5 text-right font-bold">Total CTRL (pièces)</th>
                <th class="p-2.5 text-right">Poids PHY</th>
                <th class="p-2.5 text-right text-rose-900 font-bold">Écart en kg</th>
                <th class="p-2.5 text-right">Rebut SCI</th>
                <th class="p-2.5 text-right">Rebut TRN</th>
                <th class="p-2.5 text-right">Rebut EMR</th>
                <th class="p-2.5 text-right">Rebut Usinage 2</th>
                <th class="p-2.5 text-right font-bold">Total entrée OF</th>
                <th class="p-2.5 text-right">Écart par OF</th>
                <th class="p-2.5 text-right font-bold">Résultat</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 font-medium">
              <tr v-for="item in bilansUap2" :key="item.bilanKey" class="hover:bg-slate-50/30 transition-colors">
                <td class="p-2.5 font-black text-blue-900 font-mono whitespace-nowrap">{{ item.numeroOF }}</td>
                <td class="p-2.5 font-mono text-slate-700">{{ item.codeArticle }}</td>
                <td class="p-2.5 text-slate-500 max-w-[160px] truncate">{{ item.designationArticle }}</td>
                <td class="p-2.5 text-right font-semibold font-mono text-blue-800">
                  {{ formatNumber(item.quantiteControlePieces, 0) }}
                </td>
                <td class="p-2.5 text-right font-semibold font-mono">
                  {{ formatNumber(item.poidsPhysiqueUnitaireKg, 3) }}
                </td>
                <td
                  class="p-2.5 text-right font-black font-mono"
                  :class="Math.abs(item.ecartBilanOFKg) > 0 ? 'text-rose-600' : 'text-slate-700'"
                >
                  {{ formatNumber(item.ecartBilanOFKg, 2) }}
                </td>
                <td class="p-2.5 text-right font-mono text-rose-500">{{ formatNumber(item.rebutSciKg, 2) }}</td>
                <td class="p-2.5 text-right font-mono text-rose-500">{{ formatNumber(item.rebutTrnKg, 2) }}</td>
                <td class="p-2.5 text-right font-mono text-rose-500">{{ formatNumber(item.rebutEmrKg, 2) }}</td>
                <td class="p-2.5 text-right font-bold font-mono text-rose-600">
                  {{ formatNumber(getUap2RebutUsinage2Pieces(item), 0) }}
                </td>
                <td class="p-2.5 text-right font-bold font-mono text-slate-700">
                  {{ formatNumber(item.entreeSfNetKg, 2) }}
                </td>
                <td
                  class="p-2.5 text-right font-bold font-mono"
                  :class="Math.abs(item.tauxEcartOF) > 3 ? 'text-rose-600' : 'text-slate-600'"
                >
                  {{ formatNumber(item.tauxEcartOF, 2) }} %
                </td>
                <td class="p-2.5 text-right">
                  <button
                    v-if="isAdmin"
                    @click="openVerifyDialog(item.id, 'uap2', item.statut, item.observation || '')"
                    class="inline-flex rounded-full border px-2.5 py-1 text-[10px] font-black uppercase"
                    :class="getUap2ResultatClass(item)"
                  >
                    {{ getUap2ResultatLabel(item) }}
                  </button>
                  <span
                    v-else
                    class="inline-flex rounded-full border px-2.5 py-1 text-[10px] font-black uppercase"
                    :class="getUap2ResultatClass(item)"
                  >
                    {{ getUap2ResultatLabel(item) }}
                  </span>
                </td>
              </tr>
              <tr v-if="!bilansUap2.length">
                <td colspan="13" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
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
        <div
          class="relative bg-white rounded-2xl border border-slate-100 shadow-2xl w-full max-w-lg mx-4 p-6 space-y-5"
        >
          <div class="flex items-center gap-3 border-b border-slate-100 pb-4">
            <div class="bg-blue-50 rounded-xl p-2.5">
              <MessageSquare class="w-5 h-5 text-blue-700" />
            </div>
            <div>
              <h3 class="text-sm font-black text-slate-800">Vérifier le bilan</h3>
              <p class="text-[10px] text-slate-400 font-bold uppercase tracking-wider">
                Ajouter commentaire, justification et statut de contrôle
              </p>
            </div>
          </div>

          <div class="space-y-4">
            <div>
              <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-1.5"
                >Statut de contrôle</label
              >
              <select
                v-model="verifyStatut"
                class="w-full bg-slate-50 border border-slate-200 rounded-lg px-3 py-2 text-xs font-bold focus:outline-none focus:ring-2 focus:ring-blue-200"
              >
                <option value="OK">OK</option>
                <option value="AVerifier">À vérifier</option>
                <option value="Anomalie">Anomalie</option>
                <option value="Justifie">Justifié</option>
                <option value="Regularise">Régularisé</option>
                <option v-if="verifyTarget.type === 'uap2'" value="Cloture">Clôturé</option>
              </select>
            </div>

            <div>
              <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-1.5"
                >Observation</label
              >
              <textarea
                v-model="verifyObservation"
                rows="2"
                class="w-full bg-slate-50 border border-slate-200 rounded-lg px-3 py-2 text-xs font-semibold focus:outline-none focus:ring-2 focus:ring-blue-200"
                placeholder="Observation sur le contrôle..."
              ></textarea>
            </div>

            <div>
              <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-1.5"
                >Justification</label
              >
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
