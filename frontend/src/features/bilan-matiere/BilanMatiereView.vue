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
  MessageSquare
} from 'lucide-vue-next';
import { RouterLink } from 'vue-router';
import { useAuthStore } from '@/features/auth/auth.store';
import { bilanService } from './bilan.service';
import { BilanMatiereReport, BilanUsinage2Api, BilanUap2ParOF } from './bilan.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import DataSourceBadge from '@/components/ui/DataSourceBadge.vue';
import { formatNumber } from '@/utils/formatters';
import { useAppDialog } from '@/composables/useAppDialog';
import { BUSINESS_RULES } from '@/config/businessRules';
import type { DataVertical } from '@/config/dataSources';

const authStore = useAuthStore();
const dialog = useAppDialog();
const isAdmin = computed(() => authStore.userRole === 'Admin');

// Tab state
type TabId = 'fonderie' | 'usinage2' | 'uap2';
const activeTab = ref<TabId>('fonderie');
const activeBilanVertical = computed<DataVertical>(() =>
  activeTab.value === 'fonderie' ? 'bilanFonderie' : activeTab.value === 'uap2' ? 'bilanUap2' : 'bilanU2'
);

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
const bilanUsinage = ref<BilanUsinage2Api | null>(null);
const bilansUsinage = computed(() => bilanUsinage.value?.lignes ?? []);
const isLoadingUsinage = ref(true);
const usinageError = ref('');
const totalRebutUsinage2Pieces = computed(() =>
  bilansUsinage.value.reduce((total, ligne) => total + ligne.rebutUsinage2Pieces, 0)
);
const totalEntreeOfPieces = computed(() =>
  bilansUsinage.value.reduce((total, ligne) => total + (ligne.entreeOfPieces ?? 0), 0)
);
const totalBilanUsinage2Pieces = computed(() =>
  bilansUsinage.value.reduce((total, ligne) => total + (ligne.bilanUsinage2Pieces ?? 0), 0)
);
const nombreAnomaliesUsinage2 = computed(() =>
  bilansUsinage.value.reduce((total, ligne) => total + ligne.anomalies.length, 0)
);

const getBilanUsinageStatus = (ligne: BilanUsinage2Api['lignes'][number]): string => {
  if (ligne.bilanUsinage2Pieces === null) return 'DonneesManquantes';
  return ligne.anomalies.length > 0 ? 'Anomalie' : 'OK';
};

/**
 * Etat vide explicite : plutot qu'un simple zero, on nomme la dependance manquante.
 * Les anomalies remontees par le backend permettent de distinguer les causes.
 */
const usinageEmptyMessage = computed(() => {
  if (!bilanUsinage.value) return 'Aucun bilan Usinage 2 chargé pour cette période.';
  const anomalies = bilanUsinage.value.lignes.flatMap((ligne) => ligne.anomalies);
  if (anomalies.includes('AUCUNE_RECEPTION_U2_VALIDEE')) {
    return 'Aucune réception U2 validée pour cette période.';
  }
  if (anomalies.length > 0) {
    return 'Les données nécessaires au calcul sont incomplètes.';
  }
  return 'Aucune donnée ne correspond à la période sélectionnée.';
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
  usinageError.value = '';
  try {
    bilanUsinage.value = await bilanService.getBilansUsinage(
      filterMois.value,
      filterDateDebut.value,
      filterDateFin.value
    );
  } catch (err) {
    console.error(err);
    bilanUsinage.value = null;
    usinageError.value = "Le Bilan Usinage 2 n'a pas pu être chargé depuis l'API. Aucun résultat de secours n'est affiché.";
  } finally {
    isLoadingUsinage.value = false;
  }
};

const formatNullable = (value: number | null, decimals = 0): string =>
  value === null ? '—' : formatNumber(value, decimals);

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
    <DataSourceBadge :vertical="activeBilanVertical" />

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
                <td colspan="11" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
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
      <div v-if="usinageError" class="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
        <p class="font-semibold">{{ usinageError }}</p>
        <button
          type="button"
          class="mt-3 rounded-lg bg-rose-700 px-4 py-2 text-xs font-bold uppercase text-white hover:bg-rose-800"
          @click="loadBilansUsinage"
        >
          Réessayer
        </button>
      </div>

      <div v-if="bilanUsinage" class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-6 gap-3">
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Quantité totale réceptionnée U2</p>
          <p class="mt-2 text-xl font-black text-blue-800">
            {{ formatNumber(bilanUsinage.quantiteTotaleReceptionneePieces, 0) }}
          </p>
          <p class="text-[10px] text-slate-400">pièces</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total Rebut U2</p>
          <p class="mt-2 text-xl font-black text-rose-700">{{ formatNumber(totalRebutUsinage2Pieces, 0) }}</p>
          <p class="text-[10px] text-slate-400">pièces</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total Entrée OF</p>
          <p class="mt-2 text-xl font-black text-slate-900">{{ formatNumber(totalEntreeOfPieces, 0) }}</p>
          <p class="text-[10px] text-slate-400">pièces signées</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Écart total Bilan U2</p>
          <p class="mt-2 text-xl font-black text-blue-900">{{ formatNumber(totalBilanUsinage2Pieces, 0) }}</p>
          <p class="text-[10px] text-slate-400">pièces</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Nombre de lignes</p>
          <p class="mt-2 text-xl font-black text-slate-900">{{ bilanUsinage.nombreLignes }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Nombre d’anomalies</p>
          <p class="mt-2 text-xl font-black text-amber-700">{{ nombreAnomaliesUsinage2 }}</p>
        </div>
      </div>

      <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
          <div>
            <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
              <Wrench class="w-4 h-4 text-blue-900" />
              Bilan Usinage 2
            </h3>
            <p class="mt-1 text-[10px] font-semibold text-slate-500">
              Réception U2 - Rebut U2 - Entrée OF
            </p>
          </div>
          <div v-if="bilansUsinage.length > 0" class="flex gap-2">
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
              Recharger
            </button>
          </div>
        </div>

        <div v-if="isLoadingUsinage">
          <LoadingState />
        </div>

        <div v-else-if="!usinageError" class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr
                class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
              >
                <th class="p-3">Numéro OF</th>
                <th class="p-3">Code article</th>
                <th class="p-3">Désignation</th>
                <th class="p-3">Atelier</th>
                <th class="p-3">Type matière</th>
                <th class="p-3 text-right">Quantité réceptionnée U2 (pièces)</th>
                <th class="p-3 text-right">Rebut U2 (pièces)</th>
                <th class="p-3 text-right">Entrée OF signée (pièces)</th>
                <th class="p-3 text-right">Résultat Bilan U2 (pièces)</th>
                <th class="p-3">Statut</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr
                v-for="item in bilansUsinage"
                :key="`pieces-${item.numeroOF}-${item.codeArticle}-${item.typeMatiere ?? ''}`"
                class="hover:bg-slate-50/30 transition-colors"
              >
                <td class="p-3 font-black text-blue-900 font-mono whitespace-nowrap">{{ item.numeroOF }}</td>
                <td class="p-3 font-bold text-slate-800 font-mono">{{ item.codeArticle }}</td>
                <td class="p-3 text-slate-600 max-w-[180px] truncate">{{ item.designationArticle ?? '—' }}</td>
                <td class="p-3">{{ item.atelierOrigine ?? '—' }}</td>
                <td class="p-3">{{ item.typeMatiere ?? '—' }}</td>
                <td class="p-3 text-right font-mono text-blue-700 font-bold">
                  {{ formatNumber(item.quantiteReceptionneePieces, 0) }}
                </td>
                <td class="p-3 text-right font-mono">{{ formatNumber(item.rebutUsinage2Pieces, 0) }}</td>
                <td class="p-3 text-right font-mono">{{ formatNullable(item.entreeOfPieces) }}</td>
                <td class="p-3 text-right font-mono font-black text-blue-900">
                  {{ formatNullable(item.bilanUsinage2Pieces) }}
                </td>
                <td class="p-3"><BadgeStatus :status="getBilanUsinageStatus(item)" /></td>
              </tr>
              <tr v-if="!bilansUsinage.length">
                <td colspan="10" class="p-6 text-center text-slate-400">
                  <p class="font-bold uppercase tracking-wider">{{ usinageEmptyMessage }}</p>
                  <div class="mt-3 flex flex-wrap justify-center gap-2">
                    <RouterLink
                      class="rounded-lg bg-slate-900 px-3 py-2 text-[10px] font-black uppercase text-white hover:bg-slate-800"
                      to="/receptions-usinage-2"
                    >
                      Créer une réception
                    </RouterLink>
                    <button
                      type="button"
                      class="rounded-lg border border-slate-200 px-3 py-2 text-[10px] font-black uppercase text-slate-700 hover:bg-slate-50"
                      @click="loadBilansUsinage"
                    >
                      Recharger
                    </button>
                  </div>
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
