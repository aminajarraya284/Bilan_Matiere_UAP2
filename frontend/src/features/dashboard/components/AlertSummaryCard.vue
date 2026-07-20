<script setup lang="ts">
import { AlertTriangle, CheckCircle2, ChevronRight } from 'lucide-vue-next';
import { formatNumber } from '@/utils/formatters';
import type { DashboardAlert } from '../dashboard.types';

defineProps<{ alerts: DashboardAlert[] }>();

const categoryLabel = (alert: DashboardAlert) => {
  if (alert.category === 'bilan') return 'Écart bilan';
  if (alert.category === 'weight') return 'Contrôle poids';
  return 'Information';
};
</script>

<template>
  <aside class="dashboard-alerts surface-card">
    <header class="dashboard-alerts__header">
      <div>
        <h2>Alertes importantes</h2>
        <p>Uniquement les écarts poids et bilans à traiter en priorité.</p>
      </div>
      <span class="dashboard-alerts__count">{{ alerts.length }}</span>
    </header>

    <div v-if="alerts.length" class="dashboard-alerts__list">
      <article
        v-for="(alert, index) in alerts"
        :key="alert.id"
        class="dashboard-alert motion-alert"
        :class="`dashboard-alert--${alert.type}`"
        :style="{ '--delay': `${index * 50}ms` }"
      >
        <AlertTriangle class="dashboard-alert__icon" aria-hidden="true" />
        <div class="dashboard-alert__content">
          <div class="dashboard-alert__topline">
            <span>{{ categoryLabel(alert) }}</span>
            <small v-if="alert.statut">{{ alert.statut }}</small>
          </div>
          <h3>{{ alert.title }}</h3>
          <p>{{ alert.description }}</p>

          <dl class="dashboard-alert__details">
            <div v-if="alert.typeMatiere">
              <dt>Type matière</dt>
              <dd>{{ alert.typeMatiere }}</dd>
            </div>
            <div v-if="alert.numeroOF">
              <dt>OF</dt>
              <dd>{{ alert.numeroOF }}</dd>
            </div>
            <div v-if="alert.codeArticle">
              <dt>Article</dt>
              <dd>{{ alert.codeArticle }}</dd>
            </div>
            <div v-if="alert.ecartKg !== undefined">
              <dt>Écart</dt>
              <dd>{{ formatNumber(alert.ecartKg, 0) }} kg</dd>
            </div>
            <div v-if="alert.poidsTheoriqueUnitaireKg !== undefined">
              <dt>Poids théorique</dt>
              <dd>{{ formatNumber(alert.poidsTheoriqueUnitaireKg, 3) }} kg</dd>
            </div>
            <div v-if="alert.poidsPhysiqueUnitaireKg !== undefined">
              <dt>Poids physique</dt>
              <dd>{{ formatNumber(alert.poidsPhysiqueUnitaireKg, 3) }} kg</dd>
            </div>
            <div v-if="alert.tauxEcart !== undefined">
              <dt>Taux écart</dt>
              <dd>{{ formatNumber(alert.tauxEcart, 2) }} %</dd>
            </div>
            <div v-if="alert.seuil !== undefined">
              <dt>Seuil</dt>
              <dd>{{ formatNumber(alert.seuil, 2) }} %</dd>
            </div>
          </dl>

          <router-link :to="alert.actionTo ?? '/alertes'">
            {{ alert.actionLabel ?? 'Consulter' }}
            <ChevronRight class="h-3.5 w-3.5" aria-hidden="true" />
          </router-link>
        </div>
      </article>
    </div>

    <div v-else class="dashboard-alerts__empty">
      <CheckCircle2 class="h-6 w-6" aria-hidden="true" />
      <div>
        <strong>Aucune alerte importante active</strong>
        <p>Pas d’écart poids ou bilan critique pour la période sélectionnée.</p>
      </div>
    </div>
  </aside>
</template>

<style scoped>
.dashboard-alerts {
  display: flex;
  min-height: 100%;
  flex-direction: column;
  padding: 1.25rem;
}

.dashboard-alerts__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #cbd5e1;
}

.dashboard-alerts__header h2 {
  color: var(--color-text);
  font-size: 0.9375rem;
  font-weight: 750;
}

.dashboard-alerts__header p,
.dashboard-alert p,
.dashboard-alerts__empty p {
  margin-top: 0.25rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
  line-height: 1.45;
}

.dashboard-alerts__count {
  display: grid;
  min-width: 1.75rem;
  height: 1.75rem;
  place-items: center;
  border-radius: 0.5rem;
  background: #fef3c7;
  color: #92400e;
  font-size: 0.75rem;
  font-weight: 850;
}

.dashboard-alerts__list {
  display: grid;
  gap: 0.75rem;
  margin-top: 1rem;
}

.dashboard-alert {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 0.75rem;
  padding: 0.95rem;
  border: 1px solid #f59e0b;
  border-left: 4px solid #b45309;
  border-radius: 0.7rem;
  background: #fef3c7;
}

.dashboard-alert--danger {
  border-color: #dc2626;
  border-left-color: #991b1b;
  background: #fee2e2;
}

.dashboard-alert--info {
  border-color: #2563eb;
  border-left-color: #1e40af;
  background: #dbeafe;
}

.dashboard-alert__icon {
  width: 1.125rem;
  height: 1.125rem;
  margin-top: 0.1rem;
  color: #92400e;
}

.dashboard-alert--danger .dashboard-alert__icon {
  color: #7f1d1d;
}

.dashboard-alert--info .dashboard-alert__icon {
  color: #1e3a8a;
}

.dashboard-alert__content {
  min-width: 0;
}

.dashboard-alert__topline {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.45rem;
  margin-bottom: 0.25rem;
}

.dashboard-alert__topline span,
.dashboard-alert__topline small {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.45rem;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.7);
  color: var(--color-text-secondary);
  font-size: 0.62rem;
  font-weight: 850;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.dashboard-alert h3 {
  color: var(--color-text);
  font-size: 0.85rem;
  font-weight: 800;
}

.dashboard-alert__details {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.5rem;
  margin-top: 0.75rem;
}

.dashboard-alert__details div {
  display: grid;
  gap: 0.1rem;
  padding: 0.5rem;
  border-radius: 0.5rem;
  background: rgba(255, 255, 255, 0.72);
}

.dashboard-alert__details dt {
  color: var(--color-text-muted);
  font-size: 0.58rem;
  font-weight: 850;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

.dashboard-alert__details dd {
  min-width: 0;
  overflow: hidden;
  color: var(--color-text);
  font-family: var(--font-mono);
  font-size: 0.72rem;
  font-weight: 750;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dashboard-alert a {
  display: inline-flex;
  align-items: center;
  margin-top: 0.75rem;
  color: #1e40af;
  font-size: 0.75rem;
  font-weight: 750;
}

.dashboard-alerts__empty {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  margin-top: 1rem;
  padding: 1rem;
  border: 1px solid var(--color-success-border);
  border-radius: 0.65rem;
  background: var(--color-success-soft);
  color: var(--color-success-strong);
}

.dashboard-alerts__empty strong {
  color: var(--color-text);
  font-size: 0.85rem;
}

.dashboard-alerts-wide {
  min-height: auto;
}

.dashboard-alerts-wide .dashboard-alerts__list {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

@media (max-width: 980px) {
  .dashboard-alert__details {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 820px) {
  .dashboard-alerts-wide .dashboard-alerts__list,
  .dashboard-alert__details {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
