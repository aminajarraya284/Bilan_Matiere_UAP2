<script setup lang="ts">
import { computed } from 'vue';
import { CheckCircle2 } from 'lucide-vue-next';
import { formatNumber } from '@/utils/formatters';
import type { DashboardBilanRow } from '../dashboard.types';

const props = defineProps<{
  rows: DashboardBilanRow[];
  highlightThreshold: number;
}>();

const totals = computed(() =>
  props.rows.reduce(
    (acc, row) => {
      acc.stockDebut += row.stockDebut ?? 0;
      acc.sortiesX3 += row.sortiesX3 ?? Math.max(row.inputX3 - (row.stockDebut ?? 0), 0);
      acc.dechets += row.dechets;
      acc.usinage2 += row.usinage2;
      acc.encoursFin += row.encoursFin;
      acc.ecartKg += row.ecartKg;
      return acc;
    },
    {
      stockDebut: 0,
      sortiesX3: 0,
      dechets: 0,
      usinage2: 0,
      encoursFin: 0,
      ecartKg: 0
    }
  )
);

const resolveStatus = (row: DashboardBilanRow) => {
  const absoluteGap = Math.abs(row.ecartKg);
  if (absoluteGap > props.highlightThreshold || row.statut === 'AvecEcart') {
    return { label: absoluteGap > props.highlightThreshold ? 'Critique' : 'À vérifier', tone: 'warning' };
  }
  if (absoluteGap > 0.001) return { label: 'À vérifier', tone: 'watch' };
  return { label: 'Conforme', tone: 'success' };
};
</script>

<template>
  <section class="dashboard-table dashboard-card surface-card">
    <header class="dashboard-table__header">
      <div>
        <h2>Résumé par type de matière</h2>
        <p>M3000L0, M3010L0, M3060L0 et total global du bilan matière.</p>
      </div>
      <router-link to="/bilan-matiere">Voir le bilan complet →</router-link>
    </header>

    <div v-if="rows.length" class="overflow-x-auto">
      <table>
        <thead>
          <tr>
            <th>Type matière</th>
            <th class="text-right">Stock début (kg)</th>
            <th class="text-right">Sorties magasin X3 (kg)</th>
            <th class="text-right">Déchets (kg)</th>
            <th class="text-right">Réceptions U2 (kg)</th>
            <th class="text-right">Stock fin (kg)</th>
            <th class="text-right">Écart (kg)</th>
            <th class="text-right">Statut</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.typeMatiere">
            <td class="technical-value font-semibold">{{ row.typeMatiere }}</td>
            <td class="technical-value text-right">{{ formatNumber(row.stockDebut ?? 0, 0) }}</td>
            <td class="technical-value text-right">
              {{ formatNumber(row.sortiesX3 ?? Math.max(row.inputX3 - (row.stockDebut ?? 0), 0), 0) }}
            </td>
            <td class="technical-value text-right">{{ formatNumber(row.dechets, 0) }}</td>
            <td class="technical-value text-right">{{ formatNumber(row.usinage2, 0) }}</td>
            <td class="technical-value text-right">{{ formatNumber(row.encoursFin, 0) }}</td>
            <td
              class="technical-value text-right font-bold"
              :class="Math.abs(row.ecartKg) > highlightThreshold ? 'text-rose-800' : 'text-slate-800'"
            >
              {{ formatNumber(row.ecartKg, 0) }}
            </td>
            <td class="text-right">
              <span class="dashboard-status-badge" :class="`dashboard-status-badge--${resolveStatus(row).tone}`">
                {{ resolveStatus(row).label }}
              </span>
            </td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <th>Total</th>
            <td class="technical-value text-right">{{ formatNumber(totals.stockDebut, 0) }}</td>
            <td class="technical-value text-right">{{ formatNumber(totals.sortiesX3, 0) }}</td>
            <td class="technical-value text-right">{{ formatNumber(totals.dechets, 0) }}</td>
            <td class="technical-value text-right">{{ formatNumber(totals.usinage2, 0) }}</td>
            <td class="technical-value text-right">{{ formatNumber(totals.encoursFin, 0) }}</td>
            <td class="technical-value text-right">{{ formatNumber(totals.ecartKg, 0) }}</td>
            <td class="text-right">—</td>
          </tr>
        </tfoot>
      </table>
    </div>

    <div v-else class="dashboard-table__empty">
      <CheckCircle2 class="h-6 w-6" aria-hidden="true" />
      <div>
        <strong>Aucune donnée pour les filtres sélectionnés</strong>
        <p>Changez le mois, l’année, le type matière ou l’OF puis relancez le calcul.</p>
      </div>
    </div>
  </section>
</template>

<style scoped>
.dashboard-table {
  overflow: hidden;
}

.dashboard-table__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid #cbd5e1;
}

.dashboard-table__header h2 {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  color: var(--color-text);
  font-size: 0.9375rem;
  font-weight: 750;
}

.dashboard-table__header h2::before {
  width: 3px;
  height: 1.125rem;
  border-radius: 999px;
  background: #1e3a8a;
  content: '';
}

.dashboard-table__header p {
  margin-top: 0.25rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
}

.dashboard-table__header a {
  flex: 0 0 auto;
  color: #1e40af;
  font-size: 0.75rem;
  font-weight: 750;
  text-decoration: none;
  transition: color var(--motion-duration-fast) ease;
}

.dashboard-table__header a:hover {
  color: #172554;
}

.dashboard-table table {
  min-width: 72rem;
}

.dashboard-table th,
.dashboard-table td {
  padding: 0.875rem 1rem;
}

.dashboard-table tfoot {
  border-top: 2px solid #94a3b8;
  background: #e2e8f0;
  color: var(--color-text);
  font-weight: 850;
}

.dashboard-status-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 6.5rem;
  padding: 0.35rem 0.6rem;
  border-radius: 999px;
  font-size: 0.7rem;
  font-weight: 850;
}

.dashboard-status-badge--success {
  border: 1px solid #10b981;
  background: #d1fae5;
  color: #065f46;
}

.dashboard-status-badge--watch {
  border: 1px solid #f59e0b;
  background: #fef3c7;
  color: #92400e;
}

.dashboard-status-badge--warning {
  border: 1px solid #dc2626;
  background: #fee2e2;
  color: #7f1d1d;
}

.dashboard-table__empty {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  margin: 1.25rem;
  padding: 1rem;
  border: 1px solid var(--color-info-border);
  border-radius: 0.75rem;
  background: var(--color-info-soft);
  color: var(--color-info-strong);
}

.dashboard-table__empty strong {
  color: var(--color-text);
  font-size: 0.85rem;
}

.dashboard-table__empty p {
  margin-top: 0.2rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
}

@media (max-width: 640px) {
  .dashboard-table__header {
    flex-direction: column;
    padding: 1.25rem;
  }
}
</style>
