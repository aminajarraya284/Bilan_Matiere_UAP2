<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue';
import {
  ArrowDownToLine,
  DatabaseZap,
  FileSpreadsheet,
  FileText,
  Flame,
  PackageCheck,
  RefreshCw,
  Scale,
  Warehouse,
  Wrench
} from 'lucide-vue-next';
import AppButton from '@/components/ui/AppButton.vue';
import SkeletonLoader from '@/components/ui/SkeletonLoader.vue';
import { useAppToast } from '@/composables/useAppToast';
import { BUSINESS_RULES } from '@/config/businessRules';
import { formatNumber } from '@/utils/formatters';
import { dashboardService } from './dashboard.service';
import type {
  DashboardAlert,
  DashboardBilanRow,
  DashboardDataSourceStatus,
  DashboardFilterOptions,
  DashboardStats
} from './dashboard.types';
import AlertSummaryCard from './components/AlertSummaryCard.vue';
import DashboardComparisonCard from './components/DashboardComparisonCard.vue';
import DashboardFilters from './components/DashboardFilters.vue';
import DashboardHeader from './components/DashboardHeader.vue';
import DashboardSection from './components/DashboardSection.vue';
import DashboardTable from './components/DashboardTable.vue';
import KpiCard from './components/KpiCard.vue';

const formatDateInput = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const getCurrentDashboardPeriod = () => {
  if (import.meta.env.VITE_USE_REAL_API !== 'true') {
    return {
      mois: BUSINESS_RULES.periodeParDefaut,
      dateDebut: BUSINESS_RULES.dateDebutParDefaut,
      dateFin: BUSINESS_RULES.dateFinParDefaut
    };
  }

  const today = new Date();
  const mois = formatDateInput(today).slice(0, 7);
  return { mois, dateDebut: `${mois}-01`, dateFin: formatDateInput(today) };
};

const buildPeriod = (year: string, month: string) => `${year}-${month}`;

const resolvePeriodRange = (period: string) => {
  const [year, month] = period.split('-').map(Number);
  const lastDay = new Date(year!, month!, 0).getDate();
  return {
    mois: period,
    dateDebut: `${period}-01`,
    dateFin: `${period}-${String(lastDay).padStart(2, '0')}`
  };
};

const defaultTypeMatieres = ['M3000L0', 'M3010L0', 'M3060L0'];
const toast = useAppToast();

const currentPeriod = ref(getCurrentDashboardPeriod());
const draftMonth = ref(currentPeriod.value.mois.slice(5, 7));
const draftYear = ref(currentPeriod.value.mois.slice(0, 4));
const draftTypeMatiere = ref('all');
const draftNumeroOF = ref('all');
const appliedTypeMatiere = ref('all');
const appliedNumeroOF = ref('all');

const stats = ref<DashboardStats | null>(null);
const alerts = ref<DashboardAlert[]>([]);
const listRows = ref<DashboardBilanRow[]>([]);
const dashboardSources = ref<DashboardDataSourceStatus[]>([]);
const filterOptions = ref<DashboardFilterOptions>({
  typeMatieres: defaultTypeMatieres,
  numerosOF: [],
  ofTypeMatieres: {}
});

const isLoading = ref(true);
const loadError = ref('');
const chartMotionEnabled = ref(true);
const chartAnimationKey = ref(0);
const lastSyncLabel = ref('En attente');
let refreshInterval: ReturnType<typeof setInterval> | undefined;

const availableTypeMatieres = computed(() =>
  Array.from(new Set([...defaultTypeMatieres, ...filterOptions.value.typeMatieres])).sort()
);

const availableNumerosOF = computed(() => filterOptions.value.numerosOF);

const relatedTypeMatieresForOF = computed(() => {
  if (appliedNumeroOF.value === 'all') return null;
  return filterOptions.value.ofTypeMatieres?.[appliedNumeroOF.value] ?? [];
});

const visibleRows = computed(() =>
  listRows.value.filter((row) => {
    if (appliedTypeMatiere.value !== 'all' && row.typeMatiere !== appliedTypeMatiere.value) return false;
    if (relatedTypeMatieresForOF.value && !relatedTypeMatieresForOF.value.includes(row.typeMatiere)) return false;
    return true;
  })
);

const visibleAlerts = computed(() =>
  alerts.value.filter((alert) => {
    if (appliedNumeroOF.value !== 'all' && alert.numeroOF && alert.numeroOF !== appliedNumeroOF.value) return false;
    if (appliedTypeMatiere.value !== 'all' && alert.typeMatiere && alert.typeMatiere !== appliedTypeMatiere.value) {
      return false;
    }
    return true;
  })
);

const cardSeries = computed(() =>
  visibleRows.value.map((row) => {
    const outputTotal = row.encoursFin + row.dechets + row.usinage2 + row.poidsNonReceptionne;
    const difference = row.inputX3 - outputTotal;
    const tauxEcart = row.inputX3 > 0 ? Math.abs(difference / row.inputX3) * 100 : 0;
    return {
      typeMatiere: row.typeMatiere,
      input: row.inputX3,
      output: outputTotal,
      difference,
      tauxEcart,
      stockDebut: row.stockDebut ?? 0,
      sortiesX3: row.sortiesX3 ?? Math.max(row.inputX3 - (row.stockDebut ?? 0), 0),
      dechets: row.dechets,
      encoursFin: row.encoursFin,
      usinage2: row.usinage2,
      poidsNonReceptionne: row.poidsNonReceptionne,
      ecartKg: row.ecartKg,
      statut: row.statut
    };
  })
);

const visibleTotals = computed(() => {
  const stockDebut = visibleRows.value.reduce((sum, row) => sum + (row.stockDebut ?? 0), 0);
  const sortiesMagasinX3 = visibleRows.value.reduce(
    (sum, row) => sum + (row.sortiesX3 ?? Math.max(row.inputX3 - (row.stockDebut ?? 0), 0)),
    0
  );
  const input = visibleRows.value.reduce((sum, row) => sum + row.inputX3, 0);
  const dechets = visibleRows.value.reduce((sum, row) => sum + row.dechets, 0);
  const receptionsU2 = visibleRows.value.reduce((sum, row) => sum + row.usinage2, 0);
  const stockFin = visibleRows.value.reduce((sum, row) => sum + row.encoursFin, 0);
  const poidsNonReceptionne = visibleRows.value.reduce((sum, row) => sum + row.poidsNonReceptionne, 0);
  const output = stockFin + dechets + receptionsU2 + poidsNonReceptionne;
  const ecart = input - output;
  const tauxEcart = input > 0 ? Math.abs(ecart / input) * 100 : 0;
  return {
    stockDebut,
    sortiesMagasinX3,
    input,
    dechets,
    receptionsU2,
    stockFin,
    poidsNonReceptionne,
    output,
    ecart,
    tauxEcart
  };
});

const gapTone = computed<'success' | 'warning' | 'danger'>(() => {
  if (visibleTotals.value.tauxEcart > BUSINESS_RULES.seuilEcartPourcentage) return 'danger';
  if (Math.abs(visibleTotals.value.ecart) > 0.001) return 'warning';
  return 'success';
});

const gapStatusLabel = computed(() => {
  if (gapTone.value === 'danger') return 'Critique';
  if (gapTone.value === 'warning') return 'À vérifier';
  return 'Conforme';
});

const sourceSyncStatus = computed(() => {
  const x3Sources = dashboardSources.value.filter((source) => source.group === 'X3');
  if (!x3Sources.length) return 'Non disponible';
  if (x3Sources.some((source) => source.status === 'Non disponible')) return 'Non disponible';
  if (x3Sources.some((source) => source.status === 'Ancien')) return 'À vérifier';
  return 'À jour';
});

const quickActions = [
  {
    label: 'Importer sorties magasin X3',
    description: 'Croiser mouvements VCR et encours production.',
    to: '/sorties-x3',
    icon: FileSpreadsheet
  },
  {
    label: 'Synchroniser les références',
    description: 'Gamme, nomenclature et articles.',
    to: '/references-fabrication',
    icon: DatabaseZap
  },
  {
    label: 'Réceptions Usinage 2',
    description: 'Contrôler les réceptions U2.',
    to: '/receptions-usinage-2',
    icon: Wrench
  },
  {
    label: 'Saisir inventaire',
    description: 'Stock réel fonderie et encours.',
    to: '/inventaire',
    icon: Warehouse
  },
  {
    label: 'Saisir déchets',
    description: 'Déchets fonderie validés.',
    to: '/dechets-fonderie',
    icon: Flame
  },
  {
    label: 'Ouvrir bilan matière',
    description: 'Bilan Fonderie, Usinage 2 et UAP2.',
    to: '/bilan-matiere',
    icon: Scale
  },
  {
    label: 'Exporter rapport',
    description: 'Consulter les exports disponibles.',
    to: '/rapports',
    icon: FileText
  }
];

const loadData = async (animateCharts = false, successMessage = '') => {
  chartMotionEnabled.value = animateCharts;
  isLoading.value = true;
  loadError.value = '';
  try {
    const dashboardData = await dashboardService.getDashboardData(
      currentPeriod.value.mois,
      currentPeriod.value.dateDebut,
      currentPeriod.value.dateFin
    );
    stats.value = dashboardData.stats;
    alerts.value = dashboardData.alerts;
    listRows.value = dashboardData.rows;
    dashboardSources.value = dashboardData.sources ?? [];
    filterOptions.value = dashboardData.filterOptions ?? filterOptions.value;
    lastSyncLabel.value = new Intl.DateTimeFormat('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(new Date());
    chartAnimationKey.value += 1;
    if (successMessage) toast.success(successMessage);
  } catch (error) {
    console.error('Failed to load dashboard data', error);
    loadError.value =
      error instanceof Error ? error.message : 'Le chargement du tableau de bord a échoué. Veuillez réessayer.';
    if (successMessage) toast.error(loadError.value);
  } finally {
    isLoading.value = false;
  }
};

const refreshDashboard = () => loadData(true, 'Données du tableau de bord actualisées.');

const handleApplyFilters = async () => {
  const nextPeriod = buildPeriod(draftYear.value, draftMonth.value);
  appliedTypeMatiere.value = draftTypeMatiere.value;
  appliedNumeroOF.value = draftNumeroOF.value;

  if (nextPeriod !== currentPeriod.value.mois) {
    currentPeriod.value = resolvePeriodRange(nextPeriod);
    await loadData(true, 'Filtres appliqués.');
    return;
  }

  chartMotionEnabled.value = false;
  await nextTick();
  chartAnimationKey.value += 1;
  chartMotionEnabled.value = true;
  toast.success('Filtres appliqués.');
};

const handleResetFilters = async () => {
  const defaultPeriod = getCurrentDashboardPeriod();
  currentPeriod.value = defaultPeriod;
  draftMonth.value = defaultPeriod.mois.slice(5, 7);
  draftYear.value = defaultPeriod.mois.slice(0, 4);
  draftTypeMatiere.value = 'all';
  draftNumeroOF.value = 'all';
  appliedTypeMatiere.value = 'all';
  appliedNumeroOF.value = 'all';
  await loadData(true, 'Filtres réinitialisés.');
};

onMounted(() => {
  loadData(true);
  refreshInterval = setInterval(() => loadData(false), 60000);
});

onUnmounted(() => {
  if (refreshInterval) clearInterval(refreshInterval);
});
</script>

<template>
  <div class="dashboard-page">
    <DashboardHeader
      :period="currentPeriod.mois"
      :date-debut="currentPeriod.dateDebut"
      :date-fin="currentPeriod.dateFin"
      :last-sync="lastSyncLabel"
      :sync-status="sourceSyncStatus"
      :loading="isLoading"
      @refresh="refreshDashboard"
    />

    <DashboardFilters
      v-model:month="draftMonth"
      v-model:year="draftYear"
      v-model:type-matiere="draftTypeMatiere"
      v-model:numero-o-f="draftNumeroOF"
      :type-matieres="availableTypeMatieres"
      :numeros-o-f="availableNumerosOF"
      :loading="isLoading"
      @apply="handleApplyFilters"
      @reset="handleResetFilters"
    />

    <div v-if="isLoading" class="space-y-6" aria-label="Chargement du tableau de bord">
      <SkeletonLoader variant="cards" />
      <div class="surface-card space-y-4 p-6">
        <span class="skeleton-block block h-4 w-48"></span>
        <span class="skeleton-block block h-64 w-full"></span>
      </div>
      <SkeletonLoader variant="table" :rows="4" />
    </div>

    <div v-else-if="loadError" class="dashboard-state-card surface-card" role="alert">
      <div>
        <h2>Impossible de charger le tableau de bord</h2>
        <p>{{ loadError }}</p>
      </div>
      <AppButton @click="refreshDashboard">
        <template #icon><RefreshCw class="h-4 w-4" aria-hidden="true" /></template>
        Réessayer
      </AppButton>
    </div>

    <template v-else>
      <DashboardSection>
        <div class="dashboard-kpi-grid">
          <KpiCard
            class="dashboard-enter"
            style="--delay: 40ms"
            title="Stock début"
            :value="visibleTotals.stockDebut"
            unit="kg"
            :secondary="`Période : ${stats?.periodeLabel ?? currentPeriod.mois}`"
          >
            <template #icon><Warehouse class="h-[21px] w-[21px]" aria-hidden="true" /></template>
          </KpiCard>

          <KpiCard
            class="dashboard-enter"
            style="--delay: 80ms"
            title="Sorties magasin X3"
            :value="visibleTotals.sortiesMagasinX3"
            unit="kg"
            :secondary="`Source X3 · ${sourceSyncStatus}`"
          >
            <template #icon><ArrowDownToLine class="h-[21px] w-[21px]" aria-hidden="true" /></template>
          </KpiCard>

          <KpiCard
            class="dashboard-enter"
            style="--delay: 120ms"
            title="Réceptions U2"
            :value="visibleTotals.receptionsU2"
            unit="kg"
            :secondary="`${formatNumber(visibleAlerts.filter((item) => item.category === 'weight').length, 0)} alerte(s) poids`"
          >
            <template #icon><Wrench class="h-[21px] w-[21px]" aria-hidden="true" /></template>
          </KpiCard>

          <KpiCard
            class="dashboard-enter"
            style="--delay: 160ms"
            title="Déchets Fonderie"
            :value="visibleTotals.dechets"
            unit="kg"
            :secondary="`Déchets validés sur ${currentPeriod.mois}`"
          >
            <template #icon><Flame class="h-[21px] w-[21px]" aria-hidden="true" /></template>
          </KpiCard>

          <KpiCard
            class="dashboard-enter"
            style="--delay: 200ms"
            title="Stock fin"
            :value="visibleTotals.stockFin"
            unit="kg"
            :secondary="'Inventaire fonderie utilisé dans le bilan'"
          >
            <template #icon><PackageCheck class="h-[21px] w-[21px]" aria-hidden="true" /></template>
          </KpiCard>

          <KpiCard
            class="dashboard-enter"
            style="--delay: 240ms"
            title="Écart du bilan"
            :value="visibleTotals.ecart"
            unit="kg"
            :tone="gapTone"
            :secondary="`Taux : ${formatNumber(visibleTotals.tauxEcart, 2)} %`"
          >
            <template #icon><Scale class="h-[21px] w-[21px]" aria-hidden="true" /></template>
            <template #badge>
              <span class="dashboard-gap-status" :class="`dashboard-gap-status--${gapTone}`">{{ gapStatusLabel }}</span>
            </template>
          </KpiCard>
        </div>
      </DashboardSection>

      <DashboardSection>
        <DashboardComparisonCard
          :key="chartAnimationKey"
          class="dashboard-enter"
          style="--delay: 280ms"
          :series="cardSeries"
          :motion-enabled="chartMotionEnabled"
          :highlight-threshold="BUSINESS_RULES.seuilEcartKgHighlight"
        />
      </DashboardSection>

      <DashboardSection>
        <AlertSummaryCard class="dashboard-alerts-wide dashboard-enter" style="--delay: 320ms" :alerts="visibleAlerts" />
      </DashboardSection>

      <DashboardSection>
        <DashboardTable :rows="visibleRows" :highlight-threshold="BUSINESS_RULES.seuilEcartKgHighlight" />
      </DashboardSection>

      <!--
          Ancienne présentation conservée temporairement en référence de mise en page.
          Elle n'est plus compilée ni affichée.

          <div class="dashboard-comparison-summary" aria-label="Synthèse du comparatif">
            <div class="dashboard-comparison-summary__item dashboard-comparison-summary__item--input">
              <span>Entrées bilan</span>
              <strong>{{ formatNumber(comparisonTotals.input, 0) }} kg</strong>
              <small>Stock début + sorties X3</small>
            </div>
            <div class="dashboard-comparison-summary__item dashboard-comparison-summary__item--output">
              <span>Sorties calculées</span>
              <strong>{{ formatNumber(comparisonTotals.output, 0) }} kg</strong>
              <small>Stock fin + déchets + U2</small>
            </div>
            <div
              class="dashboard-comparison-summary__item dashboard-comparison-summary__item--gap"
              :class="{ 'is-alert': comparisonTotals.tauxEcart > BUSINESS_RULES.seuilEcartPourcentage }"
            >
              <span>Écart entrée - sortie</span>
              <strong>{{ formatNumber(comparisonTotals.difference, 0) }} kg</strong>
              <small>{{ formatNumber(comparisonTotals.tauxEcart, 2) }} %</small>
            </div>
          </div>

          <div class="dashboard-comparison-formula" aria-label="Formule du comparatif">
            <span class="dashboard-comparison-formula__term dashboard-comparison-formula__term--input">
              Stock début + sorties X3
            </span>
            <strong>-</strong>
            <span class="dashboard-comparison-formula__term dashboard-comparison-formula__term--output">
              Stock fin + déchets + réceptions U2
            </span>
            <strong>=</strong>
            <span class="dashboard-comparison-formula__term dashboard-comparison-formula__term--gap">Écart</span>
          </div>

          <div class="dashboard-comparison-legend" aria-label="Légende du comparatif">
            <span><i class="dashboard-legend-dot dashboard-legend-dot--input"></i>Entrées bilan</span>
            <span><i class="dashboard-legend-dot dashboard-legend-dot--output"></i>Sorties calculées</span>
            <span><i class="dashboard-legend-dot dashboard-legend-dot--gap"></i>Écart absolu</span>
            <small>La longueur des barres est proportionnelle au plus grand flux visible.</small>
          </div>

          <div v-if="cardSeries.length" :key="chartAnimationKey" class="dashboard-comparison-chart">
            <div
              v-for="(item, index) in cardSeries"
              :key="item.typeMatiere"
              class="dashboard-chart-row"
              :class="getComparisonTone(item)"
            >
              <div class="dashboard-chart-row__heading">
                <div>
                  <span>Type matière</span>
                  <strong>{{ item.typeMatiere }}</strong>
                </div>
                <div class="dashboard-gap-chip">
                  <span>Écart</span>
                  <strong>{{ formatNumber(item.difference, 0) }} kg</strong>
                  <small>{{ formatNumber(item.tauxEcart, 2) }} %</small>
                </div>
              </div>

              <div class="dashboard-chart-row__bars">
                <div class="dashboard-bar-line">
                  <span><i class="dashboard-legend-dot dashboard-legend-dot--input"></i>Entrées</span>
                  <div class="dashboard-bar-track">
                    <div
                      class="dashboard-bar-fill dashboard-bar-fill--input"
                      :class="{ 'dashboard-bar': chartMotionEnabled }"
                      :style="{ width: getScaledBarWidth(item.input), '--delay': `${index * 60}ms` }"
                    ></div>
                  </div>
                  <strong>{{ formatNumber(item.input, 0) }} kg</strong>
                </div>

                <div class="dashboard-bar-line">
                  <span><i class="dashboard-legend-dot dashboard-legend-dot--output"></i>Sorties</span>
                  <div class="dashboard-bar-track">
                    <div
                      class="dashboard-bar-fill dashboard-bar-fill--output"
                      :class="{ 'dashboard-bar': chartMotionEnabled }"
                      :style="{ width: getScaledBarWidth(item.output), '--delay': `${index * 60 + 40}ms` }"
                    ></div>
                  </div>
                  <strong>{{ formatNumber(item.output, 0) }} kg</strong>
                </div>

                <div class="dashboard-bar-line dashboard-bar-line--gap">
                  <span><i class="dashboard-legend-dot dashboard-legend-dot--gap"></i>Écart</span>
                  <div class="dashboard-bar-track dashboard-bar-track--gap">
                    <div
                      class="dashboard-bar-fill dashboard-bar-fill--gap"
                      :class="{ 'dashboard-bar': chartMotionEnabled }"
                      :style="{ width: getScaledBarWidth(item.difference, 3), '--delay': `${index * 60 + 80}ms` }"
                    ></div>
                  </div>
                  <strong>{{ formatNumber(item.difference, 0) }} kg</strong>
                </div>

                <div class="dashboard-output-composition" aria-label="Décomposition des sorties">
                  <div class="dashboard-output-composition__header">
                    <span>Composition des sorties calculées</span>
                  </div>
                  <div class="dashboard-output-stack">
                    <span
                      class="dashboard-output-stack__part dashboard-output-stack__part--stock"
                      :style="{ width: `${item.output ? Math.max(3, (item.encoursFin / item.output) * 100) : 0}%` }"
                      title="Stock fin"
                    ></span>
                    <span
                      class="dashboard-output-stack__part dashboard-output-stack__part--waste"
                      :style="{ width: `${item.output ? Math.max(3, (item.dechets / item.output) * 100) : 0}%` }"
                      title="Déchets"
                    ></span>
                    <span
                      class="dashboard-output-stack__part dashboard-output-stack__part--u2"
                      :style="{ width: `${item.output ? Math.max(3, (item.usinage2 / item.output) * 100) : 0}%` }"
                      title="Réceptions U2"
                    ></span>
                    <span
                      v-if="item.poidsNonReceptionne > 0"
                      class="dashboard-output-stack__part dashboard-output-stack__part--pending"
                      :style="{
                        width: `${item.output ? Math.max(3, (item.poidsNonReceptionne / item.output) * 100) : 0}%`
                      }"
                      title="Ajustement"
                    ></span>
                  </div>
                  <div class="dashboard-output-breakdown">
                    <span>
                      <i class="dashboard-breakdown-dot dashboard-breakdown-dot--stock"></i>Stock fin
                      {{ formatNumber(item.encoursFin, 0) }}
                    </span>
                    <span>
                      <i class="dashboard-breakdown-dot dashboard-breakdown-dot--waste"></i>Déchets
                      {{ formatNumber(item.dechets, 0) }}
                    </span>
                    <span>
                      <i class="dashboard-breakdown-dot dashboard-breakdown-dot--u2"></i>U2
                      {{ formatNumber(item.usinage2, 0) }}
                    </span>
                    <span v-if="item.poidsNonReceptionne > 0">
                      <i class="dashboard-breakdown-dot dashboard-breakdown-dot--pending"></i>Ajustement
                      {{ formatNumber(item.poidsNonReceptionne, 0) }}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div v-else class="dashboard-chart-empty">
            <ClipboardList class="h-6 w-6" aria-hidden="true" />
            <div>
              <strong>Aucun flux à afficher</strong>
              <p>Les filtres sélectionnés ne retournent aucune ligne de bilan.</p>
            </div>
          </div>

          <template #legend>
            <div class="dashboard-flow-summary">
              <div>
                <span>Stock fin</span><strong>{{ formatNumber(visibleTotals.stockFin, 0) }} kg</strong>
              </div>
              <div>
                <span>Déchets</span><strong>{{ formatNumber(visibleTotals.dechets, 0) }} kg</strong>
              </div>
              <div>
                <span>Réceptions U2</span><strong>{{ formatNumber(visibleTotals.receptionsU2, 0) }} kg</strong>
              </div>
              <div>
                <span>Ajustement</span><strong>{{ formatNumber(visibleTotals.poidsNonReceptionne, 0) }} kg</strong>
              </div>
            </div>
          </template>
      -->

      <DashboardSection
        title="Actions rapides admin"
        description="Raccourcis vers les écrans existants, sans créer de nouvelle route."
      >
        <div class="dashboard-actions-grid">
          <router-link v-for="action in quickActions" :key="action.to" :to="action.to" class="dashboard-action-card">
            <span class="dashboard-action-card__icon">
              <component :is="action.icon" class="h-5 w-5" aria-hidden="true" />
            </span>
            <span>
              <strong>{{ action.label }}</strong>
              <small>{{ action.description }}</small>
            </span>
          </router-link>
        </div>
      </DashboardSection>
    </template>
  </div>
</template>

<style scoped>
.dashboard-page {
  width: 100%;
}

.dashboard-state-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.25rem;
  border-left: 4px solid var(--color-danger);
}

.dashboard-state-card h2 {
  color: var(--color-text);
  font-size: 1rem;
  font-weight: 800;
}

.dashboard-state-card p {
  margin-top: 0.25rem;
  color: var(--color-text-secondary);
  font-size: 0.8rem;
}

.dashboard-kpi-grid {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 1rem;
}

.dashboard-gap-status {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.5rem;
  border-radius: 999px;
  font-size: 0.65rem;
  font-weight: 850;
}

.dashboard-gap-status--success {
  background: var(--color-success-soft);
  color: var(--color-success-strong);
}

.dashboard-gap-status--warning {
  background: var(--color-warning-soft);
  color: var(--color-warning-strong);
}

.dashboard-gap-status--danger {
  background: var(--color-danger-soft);
  color: var(--color-danger-strong);
}

.dashboard-comparison-card {
  width: 100%;
}

.dashboard-comparison-chart {
  display: grid;
  gap: 1rem;
  padding: 1rem;
  border: 1px solid #cbd5e1;
  border-radius: 0.9rem;
  background: #eef2f7;
}

.dashboard-comparison-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.dashboard-comparison-summary__item {
  --summary-accent: #1e40af;
  display: grid;
  position: relative;
  gap: 0.2rem;
  min-height: 5.25rem;
  overflow: hidden;
  padding: 0.875rem 1rem 0.875rem 1.25rem;
  border: 1px solid color-mix(in srgb, var(--summary-accent) 44%, #cbd5e1);
  border-radius: 0.75rem;
  color: #0f172a;
  box-shadow: 0 12px 28px -20px color-mix(in srgb, var(--summary-accent) 58%, transparent);
}

.dashboard-comparison-summary__item::before {
  content: '';
  position: absolute;
  inset: 0 auto 0 0;
  width: 0.35rem;
  background: var(--summary-accent);
}

.dashboard-comparison-summary__item span {
  color: #334155;
  font-size: 0.6875rem;
  font-weight: 850;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.dashboard-comparison-summary__item strong {
  color: var(--summary-accent);
  font-size: 1.35rem;
  font-weight: 850;
  font-variant-numeric: tabular-nums;
}

.dashboard-comparison-summary__item small {
  color: #475569;
  font-size: 0.7rem;
  font-weight: 750;
}

.dashboard-comparison-summary__item--input {
  --summary-accent: #1e40af;
  background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
}

.dashboard-comparison-summary__item--output {
  --summary-accent: #047857;
  background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
}

.dashboard-comparison-summary__item--gap {
  --summary-accent: #b45309;
  background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
}

.dashboard-comparison-summary__item--gap.is-alert {
  --summary-accent: #991b1b;
  background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
}

.dashboard-comparison-formula {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.65rem;
  margin-bottom: 0.75rem;
  padding: 0.75rem 1rem;
  border: 1px solid #cbd5e1;
  border-radius: 0.75rem;
  background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
  color: #1e293b;
  font-size: 0.75rem;
  font-weight: 850;
}

.dashboard-comparison-formula strong {
  color: #64748b;
  font-size: 1rem;
}

.dashboard-comparison-formula__term {
  padding: 0.35rem 0.65rem;
  border-radius: 0.45rem;
  font-size: 0.7rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.dashboard-comparison-formula__term--input {
  background: #bfdbfe;
  color: #1e3a8a;
}

.dashboard-comparison-formula__term--output {
  background: #bbf7d0;
  color: #065f46;
}

.dashboard-comparison-formula__term--gap {
  background: #fecaca;
  color: #7f1d1d;
}

.dashboard-comparison-legend {
  display: flex;
  align-items: center;
  gap: 0.75rem 1.25rem;
  margin-bottom: 1rem;
  padding: 0.875rem 1rem;
  border: 1px solid #94a3b8;
  border-radius: 0.75rem;
  background: #f8fafc;
  box-shadow: 0 12px 28px -24px rgba(15, 23, 42, 0.55);
}

.dashboard-comparison-legend span,
.dashboard-bar-line > span {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
}

.dashboard-comparison-legend span {
  color: var(--color-text);
  font-size: 0.75rem;
  font-weight: 800;
}

.dashboard-comparison-legend small {
  margin-left: auto;
  color: var(--color-text-secondary);
  font-size: 0.6875rem;
}

.dashboard-legend-dot {
  display: inline-block;
  width: 0.75rem;
  height: 0.75rem;
  flex: 0 0 auto;
  border-radius: 0.2rem;
  box-shadow: 0 0 0 2px #ffffff, 0 0 0 3px rgba(148, 163, 184, 0.45);
}

.dashboard-legend-dot--input {
  background: #1e40af;
}

.dashboard-legend-dot--output {
  background: #047857;
}

.dashboard-legend-dot--gap {
  background: #991b1b;
}

.dashboard-chart-row {
  --row-accent: #1e40af;
  --row-accent-soft: #dbeafe;
  --gap-color: #047857;
  display: grid;
  grid-template-columns: minmax(9rem, 0.16fr) minmax(42rem, 1fr);
  align-items: stretch;
  overflow: hidden;
  border: 1px solid color-mix(in srgb, var(--row-accent) 30%, #cbd5e1);
  border-left: 0.35rem solid var(--row-accent);
  border-radius: 0.8rem;
  background: #ffffff;
  box-shadow: 0 18px 36px -30px rgba(15, 23, 42, 0.62);
}

.dashboard-chart-row.is-watch {
  --row-accent: #b45309;
  --row-accent-soft: #fef3c7;
  --gap-color: #b45309;
}

.dashboard-chart-row.is-alert {
  --row-accent: #991b1b;
  --row-accent-soft: #fee2e2;
  --gap-color: #991b1b;
}

.dashboard-chart-row.is-balanced {
  --row-accent: #047857;
  --row-accent-soft: #d1fae5;
  --gap-color: #047857;
}

.dashboard-chart-row__heading {
  display: flex;
  min-width: 0;
  flex-direction: column;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.9rem;
  padding: 1rem;
  border-right: 1px solid color-mix(in srgb, var(--row-accent) 18%, #e2e8f0);
  background: linear-gradient(180deg, color-mix(in srgb, var(--row-accent-soft) 80%, #ffffff) 0%, #ffffff 100%);
  color: var(--color-text);
  font-size: 0.8125rem;
}

.dashboard-chart-row__heading > div {
  display: grid;
  gap: 0.15rem;
}

.dashboard-chart-row__heading > div > span {
  color: var(--color-text-muted);
  font-size: 0.625rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.dashboard-chart-row__heading strong {
  color: var(--color-text);
  font-family: var(--font-mono);
  font-size: 1rem;
}

.dashboard-gap-chip {
  display: grid;
  gap: 0.1rem;
  width: 100%;
  min-width: 0;
  padding: 0.625rem 0.75rem;
  border: 1px solid color-mix(in srgb, var(--row-accent) 22%, transparent);
  border-radius: 0.5rem;
  background: color-mix(in srgb, var(--row-accent-soft) 86%, #ffffff);
}

.dashboard-gap-chip span {
  color: #475569;
  font-size: 0.625rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.dashboard-gap-chip strong {
  color: color-mix(in srgb, var(--row-accent) 82%, #111827);
  font-size: 0.95rem;
}

.dashboard-gap-chip small {
  color: #475569;
  font-size: 0.7rem;
  font-weight: 800;
}

.dashboard-chart-row__bars {
  display: grid;
  gap: 0.85rem;
  padding: 1rem;
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
}

.dashboard-bar-line {
  display: grid;
  grid-template-columns: 6.5rem minmax(28rem, 1fr) 7.5rem;
  align-items: center;
  gap: 0.875rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
  font-weight: 650;
}

.dashboard-bar-line > span {
  min-width: 0;
  color: #334155;
  font-weight: 850;
}

.dashboard-bar-line > strong {
  color: var(--color-text);
  font-size: 0.8125rem;
  font-weight: 750;
  font-variant-numeric: tabular-nums;
  text-align: right;
}

.dashboard-bar-track {
  height: 1.15rem;
  overflow: hidden;
  border: 1px solid #94a3b8;
  border-radius: 0.35rem;
  background: #e2e8f0;
}

.dashboard-bar-fill {
  height: 100%;
  min-width: 2px;
  border-radius: inherit;
}

.dashboard-bar-fill--input {
  background: linear-gradient(90deg, #3b82f6 0%, #1e3a8a 100%);
}

.dashboard-bar-fill--output {
  background: linear-gradient(90deg, #10b981 0%, #065f46 100%);
}

.dashboard-bar-fill--gap {
  background: linear-gradient(90deg, color-mix(in srgb, var(--gap-color) 62%, #ffffff) 0%, var(--gap-color) 100%);
}

.dashboard-bar-line--gap {
  padding-top: 0.2rem;
  border-top: 1px dashed #cbd5e1;
}

.dashboard-bar-track--gap {
  background: repeating-linear-gradient(135deg, #e2e8f0 0 7px, #cbd5e1 7px 14px);
}

.dashboard-output-composition {
  display: grid;
  gap: 0.55rem;
  margin-left: 7.375rem;
  padding: 0.75rem;
  border: 1px solid #e2e8f0;
  border-radius: 0.7rem;
  background: #ffffff;
}

.dashboard-output-composition__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: #475569;
  font-size: 0.65rem;
  font-weight: 850;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.dashboard-output-stack {
  display: flex;
  height: 0.7rem;
  overflow: hidden;
  border: 1px solid #e2e8f0;
  border-radius: 0.3rem;
  background: #f8fafc;
}

.dashboard-output-stack__part {
  min-width: 0;
}

.dashboard-output-stack__part--stock {
  background: #334155;
}

.dashboard-output-stack__part--waste {
  background: #b91c1c;
}

.dashboard-output-stack__part--u2 {
  background: #0e7490;
}

.dashboard-output-stack__part--pending {
  background: #b45309;
}

.dashboard-output-breakdown {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem 0.75rem;
  color: var(--color-text-secondary);
  font-size: 0.68rem;
  font-weight: 750;
}

.dashboard-output-breakdown span {
  display: inline-flex;
  align-items: center;
  gap: 0.32rem;
}

.dashboard-breakdown-dot {
  width: 0.45rem;
  height: 0.45rem;
  border-radius: 999px;
}

.dashboard-breakdown-dot--stock {
  background: #334155;
}

.dashboard-breakdown-dot--waste {
  background: #b91c1c;
}

.dashboard-breakdown-dot--u2 {
  background: #0e7490;
}

.dashboard-breakdown-dot--pending {
  background: #b45309;
}

.dashboard-flow-summary {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.75rem;
}

.dashboard-flow-summary > div {
  display: grid;
  gap: 0.2rem;
  padding: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-surface-soft);
}

.dashboard-flow-summary span {
  color: var(--color-text-secondary);
  font-size: 0.6875rem;
}

.dashboard-flow-summary strong {
  color: var(--color-text);
  font-size: 0.8125rem;
  font-variant-numeric: tabular-nums;
}

.dashboard-chart-empty {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem;
  border: 1px solid var(--color-info-border);
  border-radius: 0.75rem;
  background: var(--color-info-soft);
  color: var(--color-info-strong);
}

.dashboard-chart-empty strong {
  color: var(--color-text);
  font-size: 0.85rem;
}

.dashboard-chart-empty p {
  margin-top: 0.2rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
}

.dashboard-actions-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.85rem;
}

.dashboard-action-card {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  min-height: 6rem;
  padding: 1rem;
  border: 1px solid #cbd5e1;
  border-radius: 0.8rem;
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  color: inherit;
  text-decoration: none;
  box-shadow: 0 16px 34px -30px rgba(15, 23, 42, 0.68);
  transition:
    transform var(--motion-duration-fast) ease,
    border-color var(--motion-duration-fast) ease,
    box-shadow var(--motion-duration-fast) ease;
}

.dashboard-action-card:hover {
  transform: translateY(-2px);
  border-color: #1e40af;
  box-shadow: 0 18px 40px -28px rgba(30, 64, 175, 0.76);
}

.dashboard-action-card__icon {
  display: grid;
  width: 2.35rem;
  height: 2.35rem;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 0.65rem;
  background: #1e3a8a;
  color: #ffffff;
}

.dashboard-action-card strong {
  display: block;
  color: var(--color-text);
  font-size: 0.82rem;
  font-weight: 850;
}

.dashboard-action-card small {
  display: block;
  margin-top: 0.2rem;
  color: var(--color-text-secondary);
  font-size: 0.72rem;
  line-height: 1.4;
}

@media (max-width: 1320px) {
  .dashboard-kpi-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .dashboard-actions-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 980px) {
  .dashboard-kpi-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .dashboard-actions-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 820px) {
  .dashboard-comparison-summary {
    grid-template-columns: minmax(0, 1fr);
  }

  .dashboard-comparison-formula {
    align-items: stretch;
    flex-direction: column;
    text-align: center;
  }

  .dashboard-flow-summary {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .dashboard-comparison-legend {
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .dashboard-comparison-legend small {
    width: 100%;
    margin-left: 0;
  }

  .dashboard-chart-row {
    grid-template-columns: minmax(0, 1fr);
  }

  .dashboard-chart-row__heading {
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    border-right: 0;
    border-bottom: 1px solid color-mix(in srgb, var(--row-accent) 18%, #e2e8f0);
  }

  .dashboard-output-composition {
    margin-left: 0;
  }
}

@media (max-width: 640px) {
  .dashboard-state-card {
    align-items: stretch;
    flex-direction: column;
  }

  .dashboard-kpi-grid,
  .dashboard-actions-grid,
  .dashboard-flow-summary {
    grid-template-columns: minmax(0, 1fr);
  }

  .dashboard-bar-line {
    grid-template-columns: minmax(0, 1fr);
    gap: 0.4rem;
  }

  .dashboard-bar-line > strong {
    text-align: left;
  }

  .dashboard-chart-row__heading {
    align-items: flex-start;
    flex-direction: column;
  }

  .dashboard-comparison-chart {
    padding: 0.75rem;
  }
}
</style>
