<script setup lang="ts">
import type { Component } from 'vue';
import { computed, onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import {
  AlertTriangle,
  Boxes,
  CheckCircle2,
  Clock3,
  Database,
  FileWarning,
  Flame,
  RefreshCw,
  Scale,
  Wrench
} from 'lucide-vue-next';
import AppButton from '@/components/ui/AppButton.vue';
import DataSourceBadge from '@/components/ui/DataSourceBadge.vue';
import SkeletonLoader from '@/components/ui/SkeletonLoader.vue';
import { useAuthStore } from '@/features/auth/auth.store';
import { formatNumber } from '@/utils/formatters';
import DashboardChartCard from './components/DashboardChartCard.vue';
import DashboardSection from './components/DashboardSection.vue';
import KpiCard from './components/KpiCard.vue';
import { useDashboardStore } from './dashboard.store';
import type { DashboardBilanSummary, DashboardStatus } from './dashboard.types';

const dashboardStore = useDashboardStore();
const authStore = useAuthStore();
const { data, loading, error, periode } = storeToRefs(dashboardStore);
const draftPeriod = ref(periode.value);

const statusLabels: Record<DashboardStatus, string> = {
  OK: 'OK',
  AVerifier: 'À vérifier',
  Anomalie: 'Anomalie',
  Partiel: 'Calcul partiel',
  NonDisponible: 'Non disponible'
};

const statusTone = (status: DashboardStatus): 'default' | 'warning' | 'danger' | 'success' => {
  if (status === 'OK') return 'success';
  if (status === 'Anomalie') return 'danger';
  if (status === 'AVerifier' || status === 'Partiel') return 'warning';
  return 'default';
};

const statusClass = (status: DashboardStatus): string => `status-badge--${status.toLowerCase()}`;
const unitLabel = (unit: DashboardBilanSummary['unite']): string => (unit === 'pieces' ? 'pièces' : 'kg');
const nullValueLabel = (summary: DashboardBilanSummary): string =>
  summary.statut === 'Partiel' ? 'Donnée manquante' : 'Non disponible';

const formatDateTime = (value: string | null): string => {
  if (!value) return 'Non disponible';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Non disponible';
  return new Intl.DateTimeFormat('fr-FR', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'UTC'
  }).format(date);
};

interface BilanCard {
  key: 'FONDERIE' | 'USINAGE2' | 'UAP2';
  title: string;
  summary: DashboardBilanSummary;
  icon: Component;
  to: string;
}

const bilanCards = computed<BilanCard[]>(() =>
  data.value
    ? [
        {
          key: 'FONDERIE',
          title: 'Écart Fonderie',
          summary: data.value.fonderie,
          icon: Flame,
          to: '/bilan-matiere?tab=fonderie'
        },
        {
          key: 'USINAGE2',
          title: 'Écart Usinage 2',
          summary: data.value.usinage2,
          icon: Wrench,
          to: '/bilan-matiere?tab=usinage2'
        },
        {
          key: 'UAP2',
          title: 'Écart UAP2',
          summary: data.value.uap2,
          icon: Scale,
          to: '/bilan-matiere?tab=uap2'
        }
      ]
    : []
);

const canOpenBilan = (code: BilanCard['key']): boolean => {
  if (authStore.roles.includes('Admin')) return true;
  if (code === 'FONDERIE') return authStore.roles.includes('ResponsableFonderie');
  return authStore.roles.includes('ResponsableUsinage2');
};

const cardDetails = (summary: DashboardBilanSummary): string => {
  const scope = summary.nombreOf === null ? `${summary.nombreLignes} ligne(s)` : `${summary.nombreOf} OF`;
  return `${scope} · ${summary.nombreAVerifier} à vérifier · ${summary.nombreAnomalies} anomalie(s)`;
};

const statusDistribution = computed(() => {
  const summaries = bilanCards.value.map((card) => card.summary);
  const statuses: DashboardStatus[] = ['OK', 'AVerifier', 'Anomalie', 'Partiel', 'NonDisponible'];
  const counts: Record<DashboardStatus, number> = {
    OK: summaries.reduce((total, summary) => total + summary.nombreOk, 0),
    AVerifier: summaries.reduce((total, summary) => total + summary.nombreAVerifier, 0),
    Anomalie: summaries.reduce((total, summary) => total + summary.nombreAnomalies, 0),
    Partiel: 0,
    NonDisponible: 0
  };
  const total = counts.OK + counts.AVerifier + counts.Anomalie;
  return statuses.map((status) => ({
    status,
    count: counts[status],
    width: total ? (counts[status] / total) * 100 : 0
  }));
});

const statusTotal = computed(() => statusDistribution.value.reduce((total, item) => total + item.count, 0));
const statusOkPercent = computed(() =>
  statusTotal.value ? Math.round(((statusDistribution.value.find((item) => item.status === 'OK')?.count ?? 0) / statusTotal.value) * 100) : 0
);
const statusDonutStyle = computed(() => {
  const colors = ['#16a34a', '#f59e0b', '#e11d48', '#fb923c', '#94a3b8'];
  let cursor = 0;
  const stops = statusDistribution.value.map((item, index) => {
    const start = cursor;
    cursor += item.width;
    return `${colors[index]} ${start}% ${cursor}%`;
  });
  return { background: stops.length ? `conic-gradient(${stops.join(', ')})` : '#e2e8f0' };
});

const materialQuantityChart = computed(() => {
  const types = data.value?.fonderieDetail.types ?? [];
  const values = types.flatMap((type) => [type.inputKg ?? 0, type.outputKg ?? 0, Math.abs(type.ecartKg ?? 0)]);
  const maximum = Math.max(...values, 0);
  return types.map((type) => ({
    code: type.codeTypeMatiere,
    input: type.inputKg,
    output: type.outputKg,
    ecart: type.ecartKg,
    inputWidth: maximum ? ((type.inputKg ?? 0) / maximum) * 100 : 0,
    outputWidth: maximum ? ((type.outputKg ?? 0) / maximum) * 100 : 0,
    ecartWidth: maximum ? (Math.abs(type.ecartKg ?? 0) / maximum) * 100 : 0
  }));
});

const formatQuantity = (value: number | null): string =>
  value === null ? 'N/D' : `${formatNumber(value, 2)} kg`;

const fonderieValue = (value: number | null, decimals = 2): string =>
  value === null ? '—' : `${formatNumber(value, decimals)} kg`;

const fonderieSummaryItems = computed(() => {
  const detail = data.value?.fonderieDetail;
  return [
    { label: 'Total input', value: fonderieValue(detail?.totalInputKg ?? null) },
    { label: 'Total output', value: fonderieValue(detail?.totalOutputKg ?? null) },
    { label: 'Écart Fonderie', value: fonderieValue(detail?.totalEcartKg ?? null) },
    { label: "Taux d'écart", value: detail?.tauxEcartPourcent == null ? '—' : `${formatNumber(detail.tauxEcartPourcent, 2)} %` },
    { label: 'Types complets', value: formatNumber(detail?.nombreTypesComplets ?? 0, 0) },
    { label: 'Types avec anomalie ou donnée manquante', value: formatNumber(detail?.nombreTypesAvecDonneesManquantesOuAnomalies ?? 0, 0) }
  ];
});

const fonderieColumns = [
  { key: 'stockDebutKg', label: 'Stock début' },
  { key: 'sortiesMagasinX3Kg', label: 'Sorties magasin X3' },
  { key: 'inputKg', label: 'INPUT' },
  { key: 'stockFinKg', label: 'Stock fin' },
  { key: 'dechetsFonderieKg', label: 'Déchets' },
  { key: 'receptionsUsinage2Kg', label: 'Réceptions U2' },
  { key: 'outputKg', label: 'OUTPUT' },
  { key: 'ecartKg', label: 'Écart' }
] as const;

const fonderieCell = (type: (typeof fonderieColumns)[number]['key'] extends never ? never : NonNullable<typeof data.value>['fonderieDetail']['types'][number], key: (typeof fonderieColumns)[number]['key']): string =>
  fonderieValue(type[key]);

const operationalItems = computed(() =>
  data.value
    ? [
        { label: 'Inventaires brouillon', value: data.value.operations.inventairesBrouillon },
        { label: 'Inventaires validés', value: data.value.operations.inventairesValides },
        { label: 'OF non réceptionnés', value: data.value.operations.receptionsNonReceptionnees },
        { label: 'Réceptions validées', value: data.value.operations.receptionsValidees },
        { label: 'Contrôles poids ambigus', value: data.value.operations.controlesPoidsAmbigus },
        { label: 'OF avec référence manquante', value: data.value.operations.ofReferencesManquantes }
      ]
    : []
);

const importItems = computed(() =>
  data.value
    ? [
        { label: 'Réussis', value: data.value.imports.reussis, tone: 'success' },
        { label: 'Partiels', value: data.value.imports.partiels, tone: 'warning' },
        { label: 'Échoués', value: data.value.imports.echoues, tone: 'danger' },
        { label: 'Fichiers identiques', value: data.value.imports.fichiersIdentiques, tone: 'neutral' }
      ]
    : []
);

const qualityItems = computed(() =>
  data.value
    ? [
        { label: 'Anomalies', value: data.value.qualiteDonnees.nombreAnomalies },
        { label: 'Données manquantes', value: data.value.qualiteDonnees.nombreDonneesManquantes },
        { label: 'Conversions impossibles', value: data.value.qualiteDonnees.nombreConversionsImpossibles },
        { label: 'Références manquantes', value: data.value.qualiteDonnees.nombreReferencesManquantes }
      ]
    : []
);

const isEmpty = computed(
  () => data.value !== null && bilanCards.value.every((card) => card.summary.statut === 'NonDisponible')
);

const loadSelectedPeriod = async (): Promise<void> => {
  await dashboardStore.load(draftPeriod.value);
};

const refresh = async (): Promise<void> => {
  draftPeriod.value = periode.value;
  await dashboardStore.refresh();
};

onMounted(() => dashboardStore.load());
</script>

<template>
  <main class="dashboard-page">
    <header class="dashboard-hero surface-card">
      <div>
        <div class="dashboard-eyebrow"><Database class="h-4 w-4" aria-hidden="true" /> Synthèse SQL réelle</div>
        <h1>Tableau de bord matière</h1>
        <p>Bilans Fonderie, Usinage 2 et UAP2, sans mélange entre kilogrammes et pièces.</p>
      </div>
      <div class="dashboard-hero__meta">
        <DataSourceBadge vertical="dashboard" />
        <span v-if="data" class="status-badge" :class="statusClass(data.statutGlobal)">
          Statut global : {{ statusLabels[data.statutGlobal] }}
        </span>
      </div>
    </header>

    <section class="dashboard-toolbar surface-card" aria-label="Filtrer le tableau de bord">
      <label>
        <span>Période obligatoire</span>
        <input v-model="draftPeriod" type="month" required :disabled="loading" aria-label="Période du Dashboard" />
      </label>
      <AppButton :disabled="loading" @click="loadSelectedPeriod">Afficher</AppButton>
      <AppButton variant="secondary" :disabled="loading" @click="refresh">
        <template #icon><RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" aria-hidden="true" /></template>
        Actualiser
      </AppButton>
      <dl v-if="data" class="dashboard-calculation-meta">
        <div><dt>Période</dt><dd>{{ data.periode }}</dd></div>
        <div><dt>Calcul backend</dt><dd>{{ formatDateTime(data.calculeLeUtc) }} UTC</dd></div>
      </dl>
    </section>

    <section v-if="loading" aria-label="Chargement du tableau de bord" class="dashboard-loading">
      <SkeletonLoader variant="cards" />
      <SkeletonLoader variant="table" :rows="4" />
    </section>

    <section v-else-if="error" class="dashboard-state dashboard-state--error surface-card" role="alert">
      <AlertTriangle class="h-7 w-7" aria-hidden="true" />
      <div>
        <h2>Impossible de charger les données réelles</h2>
        <p>{{ error }}</p>
        <p>Aucune ancienne donnée ni donnée de démonstration n’est affichée.</p>
      </div>
      <AppButton @click="refresh">Réessayer</AppButton>
    </section>

    <template v-else-if="data">
      <section v-if="isEmpty" class="dashboard-state surface-card" role="status">
        <Boxes class="h-7 w-7" aria-hidden="true" />
        <div>
          <h2>Aucune donnée disponible pour cette période.</h2>
          <p>Les valeurs restent « Non disponible » ; aucun zéro n’est substitué à un écart absent.</p>
        </div>
      </section>

      <DashboardSection
        title="Bilans matière"
        description="Chaque carte conserve l’unité et le statut du service métier qui l’a produite."
      >
        <div class="dashboard-bilan-grid" data-testid="dashboard-bilan-cards">
          <KpiCard
            v-for="card in bilanCards"
            :key="card.key"
            :title="card.title"
            :value="card.summary.ecart"
            :unit="unitLabel(card.summary.unite)"
            :null-label="nullValueLabel(card.summary)"
            :tone="statusTone(card.summary.statut)"
            :secondary="cardDetails(card.summary)"
            :decimals="2"
          >
            <template #icon><component :is="card.icon" class="h-5 w-5" aria-hidden="true" /></template>
            <template #badge>
              <span class="status-badge" :class="statusClass(card.summary.statut)">
                {{ statusLabels[card.summary.statut] }}
              </span>
            </template>
            <router-link
              v-if="canOpenBilan(card.key)"
              :to="card.to"
              :data-destination="card.to"
              class="dashboard-detail-link"
            >
              Ouvrir le {{ card.title.toLowerCase() }}
            </router-link>
            <span v-else class="dashboard-detail-unavailable">Détail non accessible avec ce rôle</span>
          </KpiCard>
        </div>
      </DashboardSection>

      <div class="dashboard-two-columns">
        <DashboardChartCard
          title="Répartition des statuts"
            :description="`Période ${data.periode} · unité : nombre de lignes`"
        >
          <div class="status-visual" role="img" aria-label="Répartition des statuts des lignes des trois bilans">
            <div class="status-donut" :style="statusDonutStyle">
              <div class="status-donut__center">
                <strong>{{ statusOkPercent }}%</strong>
                <span>conformes</span>
              </div>
            </div>
            <div class="status-chart">
              <div v-for="item in statusDistribution" :key="item.status" class="status-chart__row">
                <span class="status-chart__label"><i :class="statusClass(item.status)"></i>{{ statusLabels[item.status] }}</span>
                <div class="status-chart__track"><i :class="statusClass(item.status)" :style="{ width: `${item.width}%` }"></i></div>
                <strong>{{ item.count }}</strong>
              </div>
            </div>
          </div>
          <template #legend>Source : GET /api/v1/dashboard · aucune période antérieure fabriquée.</template>
        </DashboardChartCard>

        <DashboardChartCard
          title="Répartition des quantités de matières"
          :description="`Période ${data.periode} · Fonderie · échelle relative au plus grand flux`"
        >
          <div v-if="materialQuantityChart.length" class="material-chart" role="img" aria-label="Répartition des quantités de matières par type">
            <div class="material-chart__legend">
              <span><i class="material-chart__swatch material-chart__swatch--input"></i>Input</span>
              <span><i class="material-chart__swatch material-chart__swatch--output"></i>Output</span>
              <span><i class="material-chart__swatch material-chart__swatch--ecart"></i>Écart</span>
            </div>
            <div v-for="item in materialQuantityChart" :key="item.code" class="material-chart__row">
              <strong>{{ item.code }}</strong>
              <div class="material-chart__bars">
                <div class="material-chart__bar-line"><span>IN</span><i class="material-chart__bar material-chart__bar--input" :style="{ width: `${item.inputWidth}%` }"></i><em>{{ formatQuantity(item.input) }}</em></div>
                <div class="material-chart__bar-line"><span>OUT</span><i class="material-chart__bar material-chart__bar--output" :style="{ width: `${item.outputWidth}%` }"></i><em>{{ formatQuantity(item.output) }}</em></div>
                <div class="material-chart__bar-line"><span>ÉC</span><i class="material-chart__bar material-chart__bar--ecart" :style="{ width: `${item.ecartWidth}%` }"></i><em>{{ formatQuantity(item.ecart) }}</em></div>
              </div>
            </div>
          </div>
          <p v-else class="dashboard-chart-empty">Aucune quantité matière disponible pour cette période.</p>
          <template #legend>Les couleurs permettent de comparer rapidement les flux par type matière.</template>
        </DashboardChartCard>

        <DashboardSection title="Synthèse opérationnelle" description="Compteurs SQL de la période sélectionnée.">
          <dl class="summary-grid">
            <div v-for="item in operationalItems" :key="item.label">
              <dt>{{ item.label }}</dt>
              <dd :class="{ 'is-unavailable': item.value === null }">
                {{ item.value === null ? 'Non disponible' : formatNumber(item.value, 0) }}
              </dd>
            </div>
          </dl>
        </DashboardSection>
      </div>

      <DashboardSection title="Résumé Bilan Fonderie" description="Totaux réels du bilan Fonderie, exclusivement en kilogrammes.">
        <div class="summary-grid" data-testid="fonderie-summary">
          <div v-for="(item, index) in fonderieSummaryItems" :key="item.label" :class="{ 'summary-card--primary': index === 0 }">
            <span v-if="index === 0" class="summary-card__eyebrow">Flux matière entrant</span>
            <dt>{{ item.label }}</dt>
            <dd>{{ item.value }}</dd>
          </div>
        </div>
      </DashboardSection>

      <DashboardSection title="Analyse Fonderie par type matière" description="INPUT = stock début + sorties X3 · OUTPUT = stock fin + déchets + réceptions U2.">
        <div class="table-wrapper" data-testid="fonderie-by-type">
          <table class="data-table">
            <thead>
              <tr>
                <th>Type matière</th>
                <th v-for="column in fonderieColumns" :key="column.key">{{ column.label }} (kg)</th>
                <th>Statut</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="type in data.fonderieDetail.types" :key="type.codeTypeMatiere">
                <th>{{ type.codeTypeMatiere }}</th>
                <td v-for="column in fonderieColumns" :key="column.key">{{ fonderieCell(type, column.key) }}</td>
                <td>
                  <span class="status-badge" :class="statusClass(type.statut === 'Complet' ? 'OK' : 'Partiel')">
                    {{ type.statut === 'Complet' ? 'Complet' : 'Calcul partiel' }}
                  </span>
                  <small v-if="type.anomalies.length" class="dashboard-table-anomalies">{{ type.anomalies.join(', ') }}</small>
                </td>
              </tr>
              <tr v-if="!data.fonderieDetail.types.length">
                <td :colspan="fonderieColumns.length + 2">Aucune donnée disponible pour cette période.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </DashboardSection>

      <div class="dashboard-two-columns">
        <DashboardSection title="Imports" description="Lots créés pendant la même période mensuelle.">
          <div class="import-grid">
            <article v-for="item in importItems" :key="item.label" :class="`import-card--${item.tone}`">
              <span>{{ item.label }}</span><strong>{{ formatNumber(item.value, 0) }}</strong>
            </article>
          </div>
          <p class="dashboard-footnote">
            <Clock3 class="h-4 w-4" aria-hidden="true" />
            Dernier import : {{ formatDateTime(data.imports.dernierImportUtc) }}
          </p>
        </DashboardSection>

        <DashboardSection title="Qualité des données" description="Comptages explicites, sans score synthétique inventé.">
          <div class="quality-header">
            <FileWarning class="h-5 w-5" aria-hidden="true" />
            <span class="status-badge" :class="statusClass(data.qualiteDonnees.statutGlobal)">
              {{ statusLabels[data.qualiteDonnees.statutGlobal] }}
            </span>
          </div>
          <dl class="summary-grid summary-grid--quality">
            <div v-for="item in qualityItems" :key="item.label">
              <dt>{{ item.label }}</dt><dd>{{ formatNumber(item.value, 0) }}</dd>
            </div>
          </dl>
        </DashboardSection>
      </div>

      <DashboardSection title="Avertissements" description="Causes remontées par les bilans et les agrégats SQL.">
        <ul v-if="data.avertissements.length" class="warning-list">
          <li v-for="warning in data.avertissements" :key="`${warning.source}-${warning.code}`">
            <AlertTriangle class="h-5 w-5" aria-hidden="true" />
            <div><strong>{{ warning.source }} · {{ statusLabels[warning.statut] }}</strong><p>{{ warning.message }}</p></div>
          </li>
        </ul>
        <div v-else class="dashboard-state dashboard-state--compact surface-card">
          <CheckCircle2 class="h-6 w-6" aria-hidden="true" />
          <p>Aucun avertissement pour cette période.</p>
        </div>
      </DashboardSection>
    </template>

    <section v-else class="dashboard-state surface-card">
      <p>Aucune donnée chargée.</p>
    </section>
  </main>
</template>

<style scoped>
.dashboard-page {
  display: grid;
  gap: 1.5rem;
  width: 100%;
}

.dashboard-hero,
.dashboard-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.25rem;
  padding: 1.25rem 1.5rem;
}

.dashboard-hero {
  color: white;
  background: linear-gradient(135deg, #0f2742, #164e63);
}

.dashboard-hero h1 {
  margin-top: 0.35rem;
  font-size: clamp(1.5rem, 2vw, 2rem);
  font-weight: 800;
  letter-spacing: -0.035em;
}

.dashboard-hero p {
  margin-top: 0.35rem;
  color: rgb(226 232 240 / 0.88);
  font-size: 0.875rem;
}

.dashboard-eyebrow {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  color: #a5f3fc;
  font-size: 0.72rem;
  font-weight: 750;
  letter-spacing: 0.09em;
  text-transform: uppercase;
}

.dashboard-hero__meta {
  display: flex;
  align-items: flex-end;
  flex-direction: column;
  gap: 0.75rem;
}

.dashboard-toolbar {
  justify-content: flex-start;
}

.dashboard-toolbar label {
  display: grid;
  gap: 0.35rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
  font-weight: 700;
}

.dashboard-toolbar input {
  min-height: 2.5rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.6rem;
  color: var(--color-text);
  background: white;
}

.dashboard-calculation-meta {
  display: flex;
  gap: 1.5rem;
  margin-left: auto;
}

.dashboard-calculation-meta div {
  display: grid;
  gap: 0.2rem;
}

.dashboard-calculation-meta dt {
  color: var(--color-text-secondary);
  font-size: 0.68rem;
  font-weight: 700;
  text-transform: uppercase;
}

.dashboard-calculation-meta dd {
  color: var(--color-text);
  font-size: 0.8rem;
  font-weight: 700;
}

.dashboard-loading {
  display: grid;
  gap: 1.25rem;
}

.dashboard-state {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.25rem;
  color: var(--color-text-secondary);
}

.dashboard-state h2 {
  color: var(--color-text);
  font-size: 1rem;
  font-weight: 750;
}

.dashboard-state p + p {
  margin-top: 0.25rem;
  font-size: 0.8rem;
}

.dashboard-state .app-button {
  margin-left: auto;
}

.dashboard-state--error {
  color: #9f1239;
  border-color: #fecdd3;
  background: #fff1f2;
}

.dashboard-state--compact {
  padding: 1rem;
}

.dashboard-bilan-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1rem;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  width: fit-content;
  padding: 0.28rem 0.55rem;
  border: 1px solid #cbd5e1;
  border-radius: 999px;
  color: #475569;
  background: #f8fafc;
  font-size: 0.67rem;
  font-weight: 800;
  white-space: nowrap;
}

.status-badge--ok {
  color: #166534;
  border-color: #bbf7d0;
  background: #f0fdf4;
}

.status-badge--averifier,
.status-badge--partiel {
  color: #9a3412;
  border-color: #fed7aa;
  background: #fff7ed;
}

.status-badge--anomalie {
  color: #9f1239;
  border-color: #fecdd3;
  background: #fff1f2;
}

.status-badge--nondisponible {
  color: #475569;
  border-color: #cbd5e1;
  background: #f1f5f9;
}

.dashboard-detail-link {
  color: var(--color-primary-700);
  font-weight: 750;
  text-decoration: none;
}

.dashboard-detail-link:hover {
  text-decoration: underline;
}

.dashboard-detail-unavailable {
  color: var(--color-text-secondary);
  font-style: italic;
}

.dashboard-two-columns {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1.25rem;
}

.status-chart {
  display: grid;
  gap: 0.8rem;
}

.status-visual {
  display: grid;
  grid-template-columns: 9.5rem minmax(0, 1fr);
  align-items: center;
  gap: 1.5rem;
}

.status-donut {
  display: grid;
  width: 9rem;
  height: 9rem;
  padding: 0.85rem;
  place-items: center;
  border-radius: 50%;
  box-shadow: inset 0 0 0 1px rgb(15 23 42 / 0.06), 0 12px 28px -20px rgb(15 23 42 / 0.7);
}

.status-donut__center {
  display: grid;
  width: 100%;
  height: 100%;
  align-content: center;
  justify-items: center;
  border-radius: 50%;
  background: white;
}

.status-donut__center strong {
  color: #0f2742;
  font-size: 1.65rem;
  line-height: 1;
}

.status-donut__center span {
  margin-top: 0.3rem;
  color: #64748b;
  font-size: 0.68rem;
  font-weight: 700;
  text-transform: uppercase;
}

.status-chart__row {
  display: grid;
  grid-template-columns: 7rem minmax(0, 1fr) 1.5rem;
  align-items: center;
  gap: 0.75rem;
  color: var(--color-text-secondary);
  font-size: 0.78rem;
}

.status-chart__label {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
}

.status-chart__label i {
  width: 0.55rem;
  height: 0.55rem;
  flex: 0 0 auto;
  border-radius: 50%;
  background: #94a3b8;
}

.status-chart__label .status-badge--ok { background: #22c55e; }
.status-chart__label .status-badge--averifier,
.status-chart__label .status-badge--partiel { background: #f59e0b; }
.status-chart__label .status-badge--anomalie { background: #e11d48; }

.status-chart__track {
  overflow: hidden;
  height: 0.65rem;
  border-radius: 999px;
  background: #e2e8f0;
}

.status-chart__track i {
  display: block;
  min-width: 0;
  height: 100%;
  border: 0;
  border-radius: inherit;
  background: #94a3b8;
}

.status-chart__track .status-badge--ok { background: #22c55e; }
.status-chart__track .status-badge--averifier,
.status-chart__track .status-badge--partiel { background: #f59e0b; }
.status-chart__track .status-badge--anomalie { background: #e11d48; }
.status-chart__track .status-badge--nondisponible { background: #94a3b8; }

.material-chart {
  display: grid;
  gap: 1rem;
}

.material-chart__legend {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  color: var(--color-text-secondary);
  font-size: 0.72rem;
  font-weight: 700;
}

.material-chart__legend span {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

.material-chart__swatch {
  width: 0.7rem;
  height: 0.7rem;
  border-radius: 0.2rem;
}

.material-chart__swatch--input,
.material-chart__bar--input { background: #2563eb; }
.material-chart__swatch--output,
.material-chart__bar--output { background: #16a34a; }
.material-chart__swatch--ecart,
.material-chart__bar--ecart { background: #f97316; }

.material-chart__row {
  display: grid;
  grid-template-columns: 6.5rem minmax(0, 1fr);
  gap: 0.75rem;
  align-items: start;
}

.material-chart__row > strong {
  padding-top: 0.18rem;
  color: var(--color-text);
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.75rem;
}

.material-chart__bars {
  display: grid;
  gap: 0.35rem;
}

.material-chart__bar-line {
  display: grid;
  grid-template-columns: 2.2rem minmax(2rem, 1fr) auto;
  gap: 0.45rem;
  align-items: center;
  color: var(--color-text-secondary);
  font-size: 0.68rem;
}

.material-chart__bar-line > span { font-weight: 800; }
.material-chart__bar-line > em { min-width: 4.8rem; font-style: normal; text-align: right; }
.material-chart__bar { display: block; min-width: 0.15rem; height: 0.55rem; border-radius: 999px; }
.dashboard-chart-empty { color: var(--color-text-secondary); font-size: 0.8rem; }

.summary-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.75rem;
}

.table-wrapper {
  overflow-x: auto;
}

.data-table {
  width: 100%;
  min-width: 980px;
  border-collapse: collapse;
  font-size: 0.78rem;
}

.data-table th,
.data-table td {
  padding: 0.7rem 0.8rem;
  border-bottom: 1px solid #e2e8f0;
  text-align: right;
  white-space: nowrap;
}

.data-table th:first-child,
.data-table td:first-child {
  text-align: left;
}

.data-table thead th {
  color: #475569;
  font-size: 0.68rem;
  text-transform: uppercase;
}

.dashboard-table-anomalies {
  display: block;
  max-width: 220px;
  color: #be123c;
  white-space: normal;
}

.summary-grid div,
.import-grid article {
  padding: 0.9rem;
  border: 1px solid var(--color-border);
  border-radius: 0.75rem;
  background: white;
}

.summary-grid .summary-card--primary {
  position: relative;
  overflow: hidden;
  padding: 1.15rem 1.2rem;
  border-color: #93c5fd;
  background: linear-gradient(135deg, #eff6ff 0%, #ffffff 72%);
  box-shadow: 0 8px 18px rgb(37 99 235 / 0.1);
}

.summary-card--primary::after {
  position: absolute;
  top: 0;
  right: 0;
  width: 0.3rem;
  height: 100%;
  background: #2563eb;
  content: '';
}

.summary-card__eyebrow {
  display: block;
  margin-bottom: 0.45rem;
  color: #1d4ed8;
  font-size: 0.65rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.summary-card--primary dd {
  color: #1e3a8a;
  font-size: clamp(1.55rem, 2.6vw, 2rem);
}

.summary-grid dt,
.import-grid span {
  color: var(--color-text-secondary);
  font-size: 0.72rem;
  font-weight: 650;
}

.summary-grid dd,
.import-grid strong {
  display: block;
  margin-top: 0.25rem;
  color: var(--color-text);
  font-size: 1.35rem;
  font-weight: 800;
}

.summary-grid dd.is-unavailable {
  font-size: 0.85rem;
  color: #64748b;
}

.import-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.6rem;
}

.import-card--success { border-color: #bbf7d0 !important; }
.import-card--warning { border-color: #fed7aa !important; }
.import-card--danger { border-color: #fecdd3 !important; }

.dashboard-footnote {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  margin-top: 0.8rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
}

.quality-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.75rem;
}

.warning-list {
  display: grid;
  gap: 0.65rem;
}

.warning-list li {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.9rem 1rem;
  border: 1px solid #fed7aa;
  border-radius: 0.75rem;
  color: #9a3412;
  background: #fff7ed;
}

.warning-list strong {
  font-size: 0.78rem;
}

.warning-list p {
  margin-top: 0.2rem;
  color: #7c2d12;
  font-size: 0.78rem;
}

@media (max-width: 1024px) {
  .dashboard-bilan-grid,
  .dashboard-two-columns {
    grid-template-columns: 1fr;
  }

  .dashboard-calculation-meta {
    width: 100%;
    margin-left: 0;
  }

  .dashboard-toolbar {
    flex-wrap: wrap;
  }
}

@media (max-width: 640px) {
  .dashboard-hero,
  .dashboard-toolbar {
    align-items: stretch;
    flex-direction: column;
  }

  .dashboard-hero__meta {
    align-items: flex-start;
  }

  .summary-grid,
  .import-grid {
    grid-template-columns: 1fr;
  }

  .status-visual {
    grid-template-columns: 1fr;
    justify-items: center;
  }

  .status-chart {
    width: 100%;
  }
}
</style>
