<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { ArrowDownToLine, ArrowUpFromLine, Scale, Percent, Calendar } from 'lucide-vue-next';
import { dashboardService } from './dashboard.service';
import { DashboardStats, DashboardAlert, DashboardBilanRow } from './dashboard.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import StatCard from '@/components/ui/StatCard.vue';
import AlertBox from '@/components/ui/AlertBox.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { formatNumber } from '@/utils/formatters';
import { BUSINESS_RULES } from '@/config/businessRules';
import backgroundUrl from '@/assets/sopal-building-bg.webp?url';

const formatDateInput = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const getCurrentDashboardPeriod = () => {
  const today = new Date();
  const mois = formatDateInput(today).slice(0, 7);
  return {
    mois,
    dateDebut: `${mois}-01`,
    dateFin: formatDateInput(today)
  };
};

const currentPeriod = ref(getCurrentDashboardPeriod());
const stats = ref<DashboardStats | null>(null);
const alerts = ref<DashboardAlert[]>([]);
const listRows = ref<DashboardBilanRow[]>([]);
const isLoading = ref(true);
let refreshInterval: ReturnType<typeof setInterval> | undefined;

const cardSeries = computed(() => {
  return listRows.value.map((row) => {
    const outputTotal = row.encoursFin + row.dechets + row.usinage2 + row.poidsNonReceptionne;
    return {
      typeMatiere: row.typeMatiere,
      input: row.inputX3,
      output: outputTotal,
      difference: row.inputX3 - outputTotal,
      dechets: row.dechets,
      encoursFin: row.encoursFin,
      usinage2: row.usinage2,
      poidsNonReceptionne: row.poidsNonReceptionne,
      ecartKg: row.ecartKg,
      statut: row.statut
    };
  });
});

const maxCompare = computed(() => {
  const vals = cardSeries.value.flatMap((c) => [c.input || 0, c.output || 0]);
  return Math.max(1, ...vals);
});

const summaryTotals = computed(() => {
  return {
    totalInput: listRows.value.reduce((sum, row) => sum + row.inputX3, 0),
    totalDechets: listRows.value.reduce((sum, row) => sum + row.dechets, 0),
    totalEncoursFin: listRows.value.reduce((sum, row) => sum + row.encoursFin, 0),
    totalUsinage2: listRows.value.reduce((sum, row) => sum + row.usinage2, 0),
    totalPoidsNonReceptionne: listRows.value.reduce((sum, row) => sum + row.poidsNonReceptionne, 0)
  };
});

const wasteRows = computed(() => {
  return [...listRows.value].sort((a, b) => b.dechets - a.dechets).slice(0, 5);
});

const inventoryRows = computed(() => {
  return [...listRows.value].sort((a, b) => b.encoursFin - a.encoursFin).slice(0, 5);
});

const maxWasteValue = computed(() => {
  return Math.max(1, ...wasteRows.value.map((row) => row.dechets));
});

const maxInventoryValue = computed(() => {
  return Math.max(1, ...inventoryRows.value.map((row) => row.encoursFin));
});

const loadData = async () => {
  isLoading.value = true;
  try {
    currentPeriod.value = getCurrentDashboardPeriod();
    const dashboardData = await dashboardService.getDashboardData(
      currentPeriod.value.mois,
      currentPeriod.value.dateDebut,
      currentPeriod.value.dateFin
    );
    stats.value = dashboardData.stats;
    alerts.value = dashboardData.alerts;
    listRows.value = dashboardData.rows;
  } catch (error) {
    console.error('Failed to load dashboard data', error);
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  loadData();
  refreshInterval = setInterval(loadData, 60000);
});

onUnmounted(() => {
  if (refreshInterval) clearInterval(refreshInterval);
});
</script>

<template>
  <div
    class="relative overflow-hidden min-h-[calc(100vh-4rem)] rounded-2xl border border-white/70 bg-gradient-to-br from-slate-50 via-sky-50 to-emerald-50 dashboard-animated-bg shadow-sm"
  >
    <div
      class="absolute inset-0 bg-cover bg-center opacity-16"
      :style="{ backgroundImage: `url(${backgroundUrl})` }"
    ></div>
    <div class="absolute inset-0 bg-gradient-to-br from-white/96 via-sky-50/88 to-emerald-50/78"></div>
    <div class="relative space-y-6 py-8 px-5 sm:px-8">
      <!-- Page Header -->
      <PageHeader
        class="dashboard-enter"
        style="--delay: 0ms"
        title="Tableau de Bord — Bilan Matière"
        description="Indicateurs et écarts matière."
      >
        <template #actions>
          <div class="flex items-center gap-2 bg-white rounded-xl border border-slate-100 p-2 shadow-sm shrink-0">
            <Calendar class="w-4 h-4 text-slate-400 shrink-0" />
            <span class="text-[10px] font-bold text-slate-500 uppercase tracking-wider shrink-0">Temps reel</span>
            <span class="bg-slate-50 border border-slate-100 rounded-lg text-xs font-bold p-1 text-slate-700">
              {{ currentPeriod.dateFin }}
            </span>
          </div>
        </template>
      </PageHeader>

      <div v-if="isLoading">
        <LoadingState />
      </div>

      <div v-else class="space-y-6">
        <!-- Statistics Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 dashboard-enter" style="--delay: 40ms">
          <StatCard
            class="dashboard-enter"
            style="--delay: 80ms"
            title="Input Matière"
            :value="formatNumber(stats?.inputMatiere, 0)"
            unit="kg"
            color-class="bg-gradient-to-br from-sky-50 to-blue-100 text-blue-600"
            trend="+2.4% vs last mo."
            trend-type="custom-green"
            border-class="border-blue-100 border-l-4 border-l-blue-200"
          >
            <template #icon>
              <ArrowDownToLine class="w-4 h-4" />
            </template>
          </StatCard>

          <StatCard
            class="dashboard-enter"
            style="--delay: 140ms"
            title="Output Matière"
            :value="formatNumber(stats?.outputMatiere, 0)"
            unit="kg"
            color-class="bg-gradient-to-br from-emerald-50 to-teal-100 text-emerald-600"
            trend="95.5% Rendement"
            trend-type="custom-gray"
            border-class="border-emerald-100 border-l-4 border-l-emerald-200"
          >
            <template #icon>
              <ArrowUpFromLine class="w-4 h-4" />
            </template>
          </StatCard>

          <StatCard
            class="dashboard-enter"
            style="--delay: 200ms"
            title="Écart Matière"
            :value="formatNumber(stats?.ecartMatiere, 0)"
            unit="kg"
            color-class="bg-gradient-to-br from-amber-50 to-orange-100 text-amber-600"
            trend="Action requise"
            trend-type="custom-amber"
            border-class="border-amber-100 border-l-4 border-l-amber-200"
          >
            <template #icon>
              <Scale class="w-4 h-4" />
            </template>
          </StatCard>

          <StatCard
            class="dashboard-enter"
            style="--delay: 260ms"
            title="Taux d'Écart"
            :value="formatNumber(stats?.tauxEcart, 2)"
            unit="%"
            color-class="bg-gradient-to-br from-rose-50 to-pink-100 text-rose-600"
            :trend="`Seuil critique > ${BUSINESS_RULES.seuilEcartPourcentage}%`"
            trend-type="custom-red"
            border-class="border-rose-100 border-l-4 border-l-rose-200"
          >
            <template #icon>
              <Percent class="w-4 h-4" />
            </template>
          </StatCard>
        </div>

        <!-- Totaux globaux plus visibles -->
        <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 dashboard-enter" style="--delay: 160ms">
          <div
            class="dashboard-card rounded-2xl border border-sky-100 bg-gradient-to-br from-sky-50 to-blue-100 text-blue-950 p-6 shadow-sm shadow-blue-900/5"
          >
            <div class="text-xs uppercase tracking-[0.28em] text-blue-500 font-semibold">Input total</div>
            <div class="mt-4 text-4xl font-extrabold">{{ formatNumber(summaryTotals.totalInput, 0) }}</div>
            <div class="mt-2 text-sm text-blue-600">kg reçus dans X3</div>
          </div>
          <div
            class="dashboard-card rounded-2xl border border-rose-100 bg-gradient-to-br from-rose-50 to-orange-100 text-rose-950 p-6 shadow-sm shadow-rose-900/5"
          >
            <div class="text-xs uppercase tracking-[0.28em] text-rose-500 font-semibold">Déchets totaux</div>
            <div class="mt-4 text-4xl font-extrabold">{{ formatNumber(summaryTotals.totalDechets, 0) }}</div>
            <div class="mt-2 text-sm text-rose-600">kg déclarés en fonderie</div>
          </div>
          <div
            class="dashboard-card rounded-2xl border border-emerald-100 bg-gradient-to-br from-emerald-50 to-lime-100 text-emerald-950 p-6 shadow-sm shadow-emerald-900/5"
          >
            <div class="text-xs uppercase tracking-[0.28em] text-emerald-500 font-semibold">Encours fin</div>
            <div class="mt-4 text-4xl font-extrabold">{{ formatNumber(summaryTotals.totalEncoursFin, 0) }}</div>
            <div class="mt-2 text-sm text-emerald-600">kg en stock</div>
          </div>
          <div
            class="dashboard-card rounded-2xl border border-violet-100 bg-gradient-to-br from-violet-50 to-fuchsia-100 text-violet-950 p-6 shadow-sm shadow-violet-900/5"
          >
            <div class="text-xs uppercase tracking-[0.28em] text-violet-500 font-semibold">Usinage 2</div>
            <div class="mt-4 text-4xl font-extrabold">{{ formatNumber(summaryTotals.totalUsinage2, 0) }}</div>
            <div class="mt-2 text-sm text-violet-600">kg reclassés</div>
          </div>
        </div>

        <!-- Analyse déchets et inventaire -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 dashboard-enter" style="--delay: 240ms">
          <div class="dashboard-card rounded-3xl border border-rose-100 bg-white/95 p-6 shadow-sm shadow-rose-900/5">
            <div class="flex items-center justify-between mb-4">
              <div>
                <h3 class="text-base font-bold text-slate-900">Top déchets</h3>
                <p class="text-sm text-rose-500">Déchets déclarés par matière.</p>
              </div>
              <span class="text-[10px] uppercase tracking-[0.3em] text-rose-400">Top 5</span>
            </div>
            <div class="space-y-4">
              <div v-for="row in wasteRows" :key="row.typeMatiere" class="space-y-2">
                <div class="flex items-center justify-between text-sm font-semibold text-slate-800">
                  <span>{{ row.typeMatiere }}</span>
                  <span class="text-rose-500">{{ formatNumber(row.dechets, 0) }} kg</span>
                </div>
                <div class="h-4 rounded-full bg-rose-50 overflow-hidden">
                  <div
                    class="h-full rounded-full bg-gradient-to-r from-rose-300 to-orange-200 dashboard-bar"
                    :style="{ width: `${Math.round((row.dechets / maxWasteValue) * 100)}%` }"
                  ></div>
                </div>
              </div>
            </div>
          </div>

          <div
            class="dashboard-card rounded-3xl border border-emerald-100 bg-white/95 p-6 shadow-sm shadow-emerald-900/5"
          >
            <div class="flex items-center justify-between mb-4">
              <div>
                <h3 class="text-base font-bold text-slate-900">Top inventaire</h3>
                <p class="text-sm text-emerald-500">Encours fin par matière.</p>
              </div>
              <span class="text-[10px] uppercase tracking-[0.3em] text-emerald-400">Top 5</span>
            </div>
            <div class="space-y-4">
              <div v-for="row in inventoryRows" :key="row.typeMatiere" class="space-y-2">
                <div class="flex items-center justify-between text-sm font-semibold text-slate-800">
                  <span>{{ row.typeMatiere }}</span>
                  <span class="text-emerald-500">{{ formatNumber(row.encoursFin, 0) }} kg</span>
                </div>
                <div class="h-4 rounded-full bg-emerald-50 overflow-hidden">
                  <div
                    class="h-full rounded-full bg-gradient-to-r from-emerald-300 to-lime-200 dashboard-bar"
                    :style="{ width: `${Math.round((row.encoursFin / maxInventoryValue) * 100)}%` }"
                  ></div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Cartes par type matière -->
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 dashboard-enter" style="--delay: 320ms">
          <div
            v-for="(item, index) in cardSeries"
            :key="item.typeMatiere"
            class="dashboard-card dashboard-enter bg-white/95 rounded-3xl border border-slate-100 shadow-sm p-5"
            :style="{ '--delay': `${320 + index * 80}ms` }"
          >
            <div class="flex items-start justify-between gap-4">
              <div>
                <h3 class="text-lg font-bold text-slate-900">{{ item.typeMatiere }}</h3>
                <p class="text-xs text-slate-500 mt-1">Input, output et écart</p>
              </div>
              <BadgeStatus :status="item.statut" />
            </div>

            <div class="mt-6 grid gap-3 sm:grid-cols-2">
              <div class="rounded-3xl bg-sky-50/80 p-4 border border-sky-100">
                <div class="text-[10px] uppercase tracking-[0.24em] text-sky-500 font-semibold">Input</div>
                <div class="mt-3 text-3xl font-extrabold text-blue-800">{{ formatNumber(item.input, 0) }}</div>
                <div class="mt-1 text-xs text-slate-500">kg</div>
              </div>
              <div class="rounded-3xl bg-emerald-50/80 p-4 border border-emerald-100">
                <div class="text-[10px] uppercase tracking-[0.24em] text-emerald-500 font-semibold">Output</div>
                <div class="mt-3 text-3xl font-extrabold text-emerald-800">{{ formatNumber(item.output, 0) }}</div>
                <div class="mt-1 text-xs text-slate-500">kg</div>
              </div>
            </div>

            <div class="mt-4 rounded-3xl border border-amber-100 bg-amber-50/50 p-4">
              <div class="flex items-center justify-between text-sm font-semibold text-slate-800 mb-2">
                <span>Différence</span>
                <span :class="item.difference >= 0 ? 'text-emerald-600' : 'text-rose-500'"
                  >{{ formatNumber(item.difference, 0) }} kg</span
                >
              </div>
              <div class="rounded-full h-3 bg-white overflow-hidden">
                <div
                  class="h-full rounded-full dashboard-bar"
                  :class="
                    item.difference >= 0
                      ? 'bg-gradient-to-r from-emerald-300 to-lime-200'
                      : 'bg-gradient-to-r from-rose-300 to-orange-200'
                  "
                  :style="{
                    width: Math.min(100, (Math.abs(item.difference) / Math.max(1, item.input, item.output)) * 100) + '%'
                  }"
                />
              </div>
            </div>

            <!-- Comparatif Input vs Output -->
            <div class="mt-4">
              <div class="text-xs text-slate-500 font-semibold mb-2">Comparatif Input / Output</div>
              <div class="space-y-2">
                <div class="flex items-center gap-3">
                  <div class="flex-1">
                    <div class="text-[10px] text-slate-400">Input</div>
                    <div class="h-2 rounded-full bg-blue-50 overflow-hidden">
                      <div
                        class="h-full bg-gradient-to-r from-sky-300 to-blue-200 dashboard-bar"
                        :style="{ width: (item.input / maxCompare) * 100 + '%' }"
                      ></div>
                    </div>
                  </div>
                  <div class="w-20 text-right font-bold text-sm">{{ formatNumber(item.input, 0) }} kg</div>
                </div>
                <div class="flex items-center gap-3">
                  <div class="flex-1">
                    <div class="text-[10px] text-slate-400">Output</div>
                    <div class="h-2 rounded-full bg-emerald-50 overflow-hidden">
                      <div
                        class="h-full bg-gradient-to-r from-emerald-300 to-lime-200 dashboard-bar"
                        :style="{ width: (item.output / maxCompare) * 100 + '%' }"
                      ></div>
                    </div>
                  </div>
                  <div class="w-20 text-right font-bold text-sm">{{ formatNumber(item.output, 0) }} kg</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Active Alerts Alerts Box Grid -->
        <div
          v-if="alerts.length > 0"
          class="grid grid-cols-1 md:grid-cols-2 gap-4 dashboard-enter"
          style="--delay: 380ms"
        >
          <AlertBox
            v-for="alert in alerts"
            :key="alert.id"
            :type="alert.type === 'danger' ? 'danger' : alert.type === 'warning' ? 'warning' : 'info'"
            :title="alert.title"
            :message="alert.description"
          />
        </div>

        <!-- Main Summary Table wrapped in DataTable styling -->
        <div
          class="dashboard-card dashboard-enter bg-white/95 rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col"
          style="--delay: 440ms"
        >
          <div
            class="px-6 py-4 border-b border-slate-100 flex justify-between items-center bg-gradient-to-r from-slate-50 via-sky-50 to-emerald-50"
          >
            <h3 class="text-xs font-bold text-slate-800 uppercase tracking-wider">
              Résumé du Bilan Mensuel — par Type Matière
            </h3>
            <router-link
              to="/bilan-matiere"
              class="text-blue-600 hover:text-blue-800 text-[10px] font-bold uppercase tracking-wider"
            >
              Voir détails complets →
            </router-link>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr
                  class="bg-slate-50/80 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-widest"
                >
                  <th class="p-4 text-left">Type Matière</th>
                  <th class="p-4 text-right text-blue-500">Input X3 (kg)</th>
                  <th class="p-4 text-right text-emerald-500">Encours Fin (kg)</th>
                  <th class="p-4 text-right text-rose-500">Déchets Fonderie (kg)</th>
                  <th class="p-4 text-right text-cyan-500">Réceptions Usinage 2 (kg)</th>
                  <th class="p-4 text-right text-slate-800">Écart Estimé (kg)</th>
                  <th class="p-4 text-right">Statut</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 text-xs">
                <tr v-for="row in listRows" :key="row.typeMatiere" class="hover:bg-sky-50/45 transition-colors">
                  <td class="p-4 font-bold text-slate-800 font-mono">{{ row.typeMatiere }}</td>
                  <td class="p-4 text-right font-semibold font-mono text-blue-600">
                    {{ formatNumber(row.inputX3, 0) }}
                  </td>
                  <td class="p-4 text-right font-semibold font-mono text-emerald-600">
                    {{ formatNumber(row.encoursFin, 0) }}
                  </td>
                  <td class="p-4 text-right font-semibold font-mono text-rose-500">
                    {{ formatNumber(row.dechets, 0) }}
                  </td>
                  <td class="p-4 text-right font-semibold font-mono text-cyan-600">
                    {{ formatNumber(row.usinage2, 0) }}
                  </td>
                  <td
                    class="p-4 text-right font-mono font-bold"
                    :class="row.ecartKg < -BUSINESS_RULES.seuilEcartKgHighlight ? 'text-rose-500' : 'text-emerald-600'"
                  >
                    {{ formatNumber(row.ecartKg, 0) }}
                  </td>
                  <td class="p-4 text-right">
                    <BadgeStatus :status="row.statut" />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
