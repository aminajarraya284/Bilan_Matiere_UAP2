<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { Eye, ExternalLink, RefreshCw, RotateCcw, Search, ShieldCheck, X } from 'lucide-vue-next';
import AppButton from '@/components/ui/AppButton.vue';
import DataSourceBadge from '@/components/ui/DataSourceBadge.vue';
import EmptyState from '@/components/ui/EmptyState.vue';
import ErrorState from '@/components/ui/ErrorState.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import PageHeader from '@/components/ui/PageHeader.vue';
import { useAuthStore } from '@/features/auth/auth.store';
import { getAlerteSourceRoute } from './alertes.navigation';
import { useAlertesStore } from './alertes.store';
import type { AlerteSeverite, AlerteStatut, AlertesFilters } from './alertes.types';

const alertesStore = useAlertesStore();
const authStore = useAuthStore();
const {
  items,
  selected,
  filters,
  page,
  total,
  summary,
  totalPages,
  derniereDetectionLeUtc,
  lastRecalculation,
  loading,
  loadingDetail,
  processing,
  recalculating,
  error
} = storeToRefs(alertesStore);

const draftFilters = reactive<AlertesFilters>({ ...filters.value });
const commentaire = ref('');
const validationMessage = ref('');

const statusLabels: Record<AlerteStatut, string> = {
  Ouverte: 'Ouverte',
  Acquittee: 'Acquittée',
  Resolue: 'Résolue',
  Ignoree: 'Ignorée'
};

const severityLabels: Record<AlerteSeverite, string> = {
  Information: 'Information',
  Avertissement: 'Avertissement',
  Erreur: 'Erreur'
};

const sourceOptions = ['Bilan Fonderie', 'Bilan Usinage 2', 'Bilan UAP2', 'Import X3'];

const formatDateTime = (value: string | null): string => {
  if (!value) return 'Non disponible';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Non disponible';
  return new Intl.DateTimeFormat('fr-FR', {
    dateStyle: 'short',
    timeStyle: 'short',
    timeZone: 'UTC'
  }).format(date);
};

const sourceRoute = computed(() => (selected.value ? getAlerteSourceRoute(selected.value, authStore.roles) : null));
const associatedEntries = computed(() => Object.entries(selected.value?.donneesAssociees ?? {}));

const applyFilters = (): Promise<void> => alertesStore.applyFilters({ ...draftFilters });

const resetFilters = async (): Promise<void> => {
  await alertesStore.resetFilters();
  Object.assign(draftFilters, filters.value);
};

const openDetail = async (id: string): Promise<void> => {
  commentaire.value = '';
  validationMessage.value = '';
  await alertesStore.loadDetail(id);
};

const closeDetail = (): void => {
  commentaire.value = '';
  validationMessage.value = '';
  alertesStore.closeDetail();
};

const acquitter = async (): Promise<void> => {
  if (!selected.value) return;
  validationMessage.value = '';
  const success = await alertesStore.acquitter(selected.value.id, commentaire.value.trim() || null);
  if (success) commentaire.value = '';
};

const resoudre = async (): Promise<void> => {
  if (!selected.value) return;
  validationMessage.value = '';
  const success = await alertesStore.resoudre(selected.value.id, commentaire.value.trim() || null);
  if (success) commentaire.value = '';
};

const ignorer = async (): Promise<void> => {
  if (!selected.value) return;
  const justification = commentaire.value.trim();
  if (!justification) {
    validationMessage.value = 'Une justification est obligatoire pour ignorer une alerte.';
    return;
  }
  validationMessage.value = '';
  const success = await alertesStore.ignorer(selected.value.id, justification);
  if (success) commentaire.value = '';
};

onMounted(() => alertesStore.load());
</script>

<template>
  <main class="alertes-page">
    <PageHeader
      title="Alertes métier"
      description="Détections persistantes issues des bilans et données SQL, avec cycle de traitement tracé."
    >
      <template #actions>
        <AppButton variant="secondary" :disabled="loading || recalculating" @click="alertesStore.load">
          <template #icon><RefreshCw class="h-4 w-4" aria-hidden="true" /></template>
          Actualiser
        </AppButton>
        <AppButton
          :loading="recalculating"
          :disabled="loading || !filters.periode"
          :title="filters.periode ? `Recalculer les alertes de ${filters.periode}` : 'Sélectionnez une période pour recalculer'"
          @click="alertesStore.recalculer"
        >
          <template #icon><ShieldCheck class="h-4 w-4" aria-hidden="true" /></template>
          Recalculer
        </AppButton>
      </template>
    </PageHeader>

    <DataSourceBadge vertical="alertes" />

    <section class="alertes-context surface-card" aria-label="Contexte des alertes">
      <div>
        <span>Période active</span>
        <strong>{{ filters.periode || 'Toutes périodes' }}</strong>
      </div>
      <div>
        <span>Dernière détection</span>
        <strong>{{ formatDateTime(derniereDetectionLeUtc) }} UTC</strong>
      </div>
      <div v-if="lastRecalculation">
        <span>Dernier recalcul</span>
        <strong>
          {{ lastRecalculation.nombreDetections }} détection(s) · {{ lastRecalculation.nombreCreees }} créée(s)
        </strong>
      </div>
    </section>

    <form class="alertes-filters surface-card" aria-label="Filtres des alertes" @submit.prevent="applyFilters">
      <label>
        <span>Période</span>
        <input
          v-model="draftFilters.periode"
          type="month"
          :disabled="loading"
          title="Laisser vide pour afficher toutes les périodes"
        />
      </label>
      <label>
        <span>Statut</span>
        <select v-model="draftFilters.statut" :disabled="loading">
          <option value="">Tous</option>
          <option v-for="(label, value) in statusLabels" :key="value" :value="value">{{ label }}</option>
        </select>
      </label>
      <label>
        <span>Gravité</span>
        <select v-model="draftFilters.severite" :disabled="loading">
          <option value="">Toutes</option>
          <option v-for="(label, value) in severityLabels" :key="value" :value="value">{{ label }}</option>
        </select>
      </label>
      <label>
        <span>Source</span>
        <select v-model="draftFilters.source" :disabled="loading">
          <option value="">Toutes</option>
          <option v-for="source in sourceOptions" :key="source" :value="source">{{ source }}</option>
        </select>
      </label>
      <label>
        <span>OF</span>
        <input v-model.trim="draftFilters.numeroOF" type="search" placeholder="OF…" :disabled="loading" />
      </label>
      <label>
        <span>Article</span>
        <input v-model.trim="draftFilters.codeArticle" type="search" placeholder="Article…" :disabled="loading" />
      </label>
      <label class="alertes-filters__search">
        <span>Recherche</span>
        <input
          v-model.trim="draftFilters.recherche"
          type="search"
          placeholder="Code, titre, description…"
          :disabled="loading"
        />
      </label>
      <div class="alertes-filters__actions">
        <AppButton type="submit" size="sm" :loading="loading">
          <template #icon><Search class="h-4 w-4" aria-hidden="true" /></template>
          Filtrer
        </AppButton>
        <AppButton type="button" size="sm" variant="secondary" :disabled="loading" @click="resetFilters">
          <template #icon><RotateCcw class="h-4 w-4" aria-hidden="true" /></template>
          Réinitialiser
        </AppButton>
      </div>
    </form>

    <section class="alertes-summary" aria-label="Résumé des statuts">
      <article>
        <span>Total</span>
        <strong>{{ summary.total }}</strong>
      </article>
      <article class="is-open">
        <span>Ouvertes</span>
        <strong>{{ summary.ouvertes }}</strong>
      </article>
      <article class="is-acknowledged">
        <span>Acquittées</span>
        <strong>{{ summary.acquittees }}</strong>
      </article>
      <article class="is-resolved">
        <span>Résolues</span>
        <strong>{{ summary.resolues }}</strong>
      </article>
      <article class="is-ignored">
        <span>Ignorées</span>
        <strong>{{ summary.ignorees }}</strong>
      </article>
    </section>

    <LoadingState v-if="loading" />
    <ErrorState
      v-else-if="error"
      title="Impossible de charger les alertes réelles"
      :message="error"
      @retry="alertesStore.load"
    />
    <EmptyState
      v-else-if="!items.length"
      title="Aucune alerte pour ces filtres"
      message="Aucune donnée fictive n’est affichée. Lancez un recalcul ou modifiez les filtres."
    />

    <section v-else class="alertes-table surface-card">
      <div class="overflow-x-auto">
        <table>
          <thead>
            <tr>
              <th>Gravité</th>
              <th>Code et titre</th>
              <th>Source</th>
              <th>Période</th>
              <th>OF</th>
              <th>Article</th>
              <th>Statut</th>
              <th>Première détection</th>
              <th>Dernière détection</th>
              <th class="text-right">Action</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="alerte in items" :key="alerte.id">
              <td>
                <span class="alert-badge" :class="`alert-badge--severity-${alerte.severite.toLowerCase()}`">
                  {{ severityLabels[alerte.severite] }}
                </span>
              </td>
              <td>
                <strong class="alertes-table__title">{{ alerte.titre }}</strong>
                <code>{{ alerte.code }}</code>
              </td>
              <td>{{ alerte.source }}</td>
              <td>{{ alerte.periode ?? '—' }}</td>
              <td class="technical-value">{{ alerte.numeroOF ?? '—' }}</td>
              <td class="technical-value">{{ alerte.codeArticle ?? '—' }}</td>
              <td>
                <span class="alert-badge" :class="`alert-badge--status-${alerte.statut.toLowerCase()}`">
                  {{ statusLabels[alerte.statut] }}
                </span>
              </td>
              <td>{{ formatDateTime(alerte.detecteeLeUtc) }}</td>
              <td>{{ formatDateTime(alerte.derniereDetectionLeUtc) }}</td>
              <td class="text-right">
                <AppButton size="sm" variant="ghost" @click="openDetail(alerte.id)">
                  <template #icon><Eye class="h-4 w-4" aria-hidden="true" /></template>
                  Détail
                </AppButton>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <footer class="alertes-pagination">
        <span>{{ total }} résultat(s) · page {{ page }} / {{ totalPages }}</span>
        <div>
          <AppButton size="sm" variant="secondary" :disabled="page <= 1" @click="alertesStore.goToPage(page - 1)">
            Précédent
          </AppButton>
          <AppButton
            size="sm"
            variant="secondary"
            :disabled="page >= totalPages"
            @click="alertesStore.goToPage(page + 1)"
          >
            Suivant
          </AppButton>
        </div>
      </footer>
    </section>

    <div v-if="selected || loadingDetail" class="alertes-detail-backdrop" role="presentation" @click.self="closeDetail">
      <aside class="alertes-detail" role="dialog" aria-modal="true" aria-label="Détail de l’alerte">
        <header class="alertes-detail__header">
          <div>
            <span>Détail de l’alerte</span>
            <h2>{{ selected?.titre ?? 'Chargement…' }}</h2>
          </div>
          <button type="button" aria-label="Fermer le détail" @click="closeDetail">
            <X class="h-5 w-5" aria-hidden="true" />
          </button>
        </header>

        <LoadingState v-if="loadingDetail" />
        <template v-else-if="selected">
          <div class="alertes-detail__badges">
            <span class="alert-badge" :class="`alert-badge--severity-${selected.severite.toLowerCase()}`">
              {{ severityLabels[selected.severite] }}
            </span>
            <span class="alert-badge" :class="`alert-badge--status-${selected.statut.toLowerCase()}`">
              {{ statusLabels[selected.statut] }}
            </span>
            <code>{{ selected.code }}</code>
          </div>

          <p class="alertes-detail__description">{{ selected.description }}</p>

          <dl class="alertes-detail__grid">
            <div>
              <dt>Source</dt>
              <dd>{{ selected.source }}</dd>
            </div>
            <div>
              <dt>Période</dt>
              <dd>{{ selected.periode ?? 'Non renseignée' }}</dd>
            </div>
            <div>
              <dt>OF</dt>
              <dd>{{ selected.numeroOF ?? 'Non renseigné' }}</dd>
            </div>
            <div>
              <dt>Article</dt>
              <dd>{{ selected.codeArticle ?? 'Non renseigné' }}</dd>
            </div>
            <div>
              <dt>Type matière</dt>
              <dd>{{ selected.typeMatiere ?? 'Non renseigné' }}</dd>
            </div>
            <div>
              <dt>Résolution automatique</dt>
              <dd>{{ selected.resolutionAutomatique ? 'Oui' : 'Non' }}</dd>
            </div>
            <div>
              <dt>Première détection</dt>
              <dd>{{ formatDateTime(selected.detecteeLeUtc) }} UTC</dd>
            </div>
            <div>
              <dt>Dernière détection</dt>
              <dd>{{ formatDateTime(selected.derniereDetectionLeUtc) }} UTC</dd>
            </div>
          </dl>

          <section v-if="associatedEntries.length" class="alertes-detail__associated">
            <h3>Données associées</h3>
            <dl>
              <div v-for="[key, value] in associatedEntries" :key="key">
                <dt>{{ key }}</dt>
                <dd>{{ value ?? 'Non disponible' }}</dd>
              </div>
            </dl>
          </section>

          <section
            v-if="selected.commentaireTraitement || selected.acquitteePar || selected.resoluePar || selected.ignoreePar"
            class="alertes-detail__history"
          >
            <h3>Traçabilité du traitement</h3>
            <p v-if="selected.acquitteePar">
              Acquittée par {{ selected.acquitteePar }} le {{ formatDateTime(selected.acquitteeLeUtc) }} UTC
            </p>
            <p v-if="selected.resoluePar">
              Résolue par {{ selected.resoluePar }} le {{ formatDateTime(selected.resolueLeUtc) }} UTC
            </p>
            <p v-if="selected.ignoreePar">
              Ignorée par {{ selected.ignoreePar }} le {{ formatDateTime(selected.ignoreeLeUtc) }} UTC
            </p>
            <p v-if="selected.commentaireTraitement">
              <strong>Commentaire :</strong> {{ selected.commentaireTraitement }}
            </p>
          </section>

          <router-link v-if="sourceRoute" :to="sourceRoute" class="alertes-source-link">
            Ouvrir la donnée source
            <ExternalLink class="h-4 w-4" aria-hidden="true" />
          </router-link>

          <section
            v-if="selected.statut === 'Ouverte' || selected.statut === 'Acquittee'"
            class="alertes-detail__actions"
          >
            <label>
              <span>Commentaire de traitement</span>
              <textarea
                v-model="commentaire"
                rows="3"
                maxlength="1000"
                placeholder="Facultatif pour acquitter/résoudre, obligatoire pour ignorer"
                :disabled="processing"
              ></textarea>
            </label>
            <p v-if="validationMessage" class="alertes-detail__validation" role="alert">{{ validationMessage }}</p>
            <div>
              <AppButton
                v-if="selected.statut === 'Ouverte'"
                variant="secondary"
                :loading="processing"
                @click="acquitter"
              >
                Acquitter
              </AppButton>
              <AppButton variant="success" :loading="processing" @click="resoudre">Résoudre</AppButton>
              <AppButton variant="warning" :loading="processing" @click="ignorer">Ignorer</AppButton>
            </div>
          </section>
        </template>
      </aside>
    </div>
  </main>
</template>

<style scoped>
.alertes-page {
  display: grid;
  gap: 1.25rem;
  width: 100%;
}

.alertes-context {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem 2rem;
  padding: 1rem 1.25rem;
}

.alertes-context div {
  display: grid;
  gap: 0.2rem;
}

.alertes-context span,
.alertes-filters label > span {
  color: var(--color-text-muted);
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

.alertes-context strong {
  color: var(--color-text);
  font-size: 0.82rem;
}

.alertes-filters {
  display: grid;
  grid-template-columns: repeat(6, minmax(8rem, 1fr));
  gap: 0.75rem;
  padding: 1rem;
}

.alertes-filters label {
  display: grid;
  gap: 0.35rem;
}

.alertes-filters input,
.alertes-filters select,
.alertes-detail textarea {
  min-height: 2.5rem;
  width: 100%;
  border: 1px solid var(--color-border);
  border-radius: 0.55rem;
  background: white;
  color: var(--color-text);
  font-size: 0.78rem;
}

.alertes-filters__search {
  grid-column: span 2;
}

.alertes-filters__actions {
  display: flex;
  grid-column: span 4;
  align-items: end;
  justify-content: flex-end;
  gap: 0.5rem;
}

.alertes-summary {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 0.75rem;
}

.alertes-summary article {
  padding: 1rem;
  border: 1px solid #cbd5e1;
  border-top: 3px solid #334155;
  border-radius: 0.75rem;
  background: white;
}

.alertes-summary article.is-open {
  border-top-color: #be123c;
}
.alertes-summary article.is-acknowledged {
  border-top-color: #d97706;
}
.alertes-summary article.is-resolved {
  border-top-color: #059669;
}
.alertes-summary article.is-ignored {
  border-top-color: #64748b;
}

.alertes-summary span {
  color: var(--color-text-secondary);
  font-size: 0.72rem;
  font-weight: 750;
}

.alertes-summary strong {
  display: block;
  margin-top: 0.3rem;
  color: var(--color-text);
  font-size: 1.5rem;
  font-weight: 850;
}

.alertes-table {
  overflow: hidden;
}

.alertes-table table {
  min-width: 92rem;
}

.alertes-table th,
.alertes-table td {
  padding: 0.8rem;
  vertical-align: top;
}

.alertes-table__title {
  display: block;
  max-width: 22rem;
  color: var(--color-text);
  font-size: 0.78rem;
}

.alertes-table code,
.alertes-detail code {
  display: inline-block;
  margin-top: 0.25rem;
  color: #475569;
  font-size: 0.67rem;
}

.alert-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.3rem 0.55rem;
  border: 1px solid #cbd5e1;
  border-radius: 999px;
  background: #f8fafc;
  color: #475569;
  font-size: 0.66rem;
  font-weight: 850;
  white-space: nowrap;
}

.alert-badge--severity-information {
  border-color: #7dd3fc;
  background: #f0f9ff;
  color: #075985;
}
.alert-badge--severity-avertissement {
  border-color: #fcd34d;
  background: #fffbeb;
  color: #92400e;
}
.alert-badge--severity-erreur {
  border-color: #fda4af;
  background: #fff1f2;
  color: #9f1239;
}
.alert-badge--status-ouverte {
  border-color: #fda4af;
  background: #fff1f2;
  color: #9f1239;
}
.alert-badge--status-acquittee {
  border-color: #fcd34d;
  background: #fffbeb;
  color: #92400e;
}
.alert-badge--status-resolue {
  border-color: #86efac;
  background: #f0fdf4;
  color: #166534;
}
.alert-badge--status-ignoree {
  border-color: #cbd5e1;
  background: #f1f5f9;
  color: #475569;
}

.alertes-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem 1.25rem;
  border-top: 1px solid var(--color-border);
  color: var(--color-text-secondary);
  font-size: 0.75rem;
}

.alertes-pagination div {
  display: flex;
  gap: 0.5rem;
}

.alertes-detail-backdrop {
  position: fixed;
  z-index: 80;
  inset: 0;
  display: flex;
  justify-content: flex-end;
  background: rgb(15 23 42 / 0.46);
}

.alertes-detail {
  width: min(42rem, 100%);
  height: 100%;
  overflow-y: auto;
  padding: 1.5rem;
  background: white;
  box-shadow: -20px 0 50px -35px rgb(15 23 42 / 0.7);
}

.alertes-detail__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--color-border);
}

.alertes-detail__header span {
  color: var(--color-primary-700);
  font-size: 0.68rem;
  font-weight: 850;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.alertes-detail__header h2 {
  margin-top: 0.3rem;
  color: var(--color-text);
  font-size: 1.25rem;
  font-weight: 850;
}

.alertes-detail__header button {
  display: grid;
  width: 2.25rem;
  height: 2.25rem;
  place-items: center;
  border-radius: 0.5rem;
  color: #475569;
}

.alertes-detail__badges {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  margin-top: 1rem;
}

.alertes-detail__description {
  margin-top: 1rem;
  color: var(--color-text-secondary);
  font-size: 0.85rem;
  line-height: 1.6;
}

.alertes-detail__grid,
.alertes-detail__associated dl {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.65rem;
  margin-top: 1rem;
}

.alertes-detail__grid div,
.alertes-detail__associated dl div {
  padding: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.6rem;
  background: #f8fafc;
}

.alertes-detail dt {
  color: var(--color-text-muted);
  font-size: 0.62rem;
  font-weight: 800;
  text-transform: uppercase;
}

.alertes-detail dd {
  margin-top: 0.2rem;
  color: var(--color-text);
  font-size: 0.78rem;
  overflow-wrap: anywhere;
}

.alertes-detail__associated,
.alertes-detail__history,
.alertes-detail__actions {
  margin-top: 1.25rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--color-border);
}

.alertes-detail h3 {
  color: var(--color-text);
  font-size: 0.82rem;
  font-weight: 800;
}

.alertes-detail__history p {
  margin-top: 0.4rem;
  color: var(--color-text-secondary);
  font-size: 0.76rem;
}

.alertes-source-link {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin-top: 1.25rem;
  color: #1e40af;
  font-size: 0.8rem;
  font-weight: 800;
}

.alertes-detail__actions label {
  display: grid;
  gap: 0.4rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
  font-weight: 750;
}

.alertes-detail textarea {
  min-height: 5rem;
  padding: 0.65rem;
  resize: vertical;
}

.alertes-detail__actions > div {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.75rem;
}

.alertes-detail__validation {
  margin-top: 0.5rem;
  color: #be123c;
  font-size: 0.75rem;
  font-weight: 750;
}

@media (max-width: 1200px) {
  .alertes-filters {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .alertes-filters__actions {
    grid-column: span 1;
  }
}

@media (max-width: 760px) {
  .alertes-summary {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .alertes-filters {
    grid-template-columns: minmax(0, 1fr);
  }

  .alertes-filters__search,
  .alertes-filters__actions {
    grid-column: span 1;
  }

  .alertes-filters__actions,
  .alertes-pagination {
    align-items: stretch;
    flex-direction: column;
  }

  .alertes-detail__grid,
  .alertes-detail__associated dl {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
