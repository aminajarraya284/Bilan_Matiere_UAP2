<script setup lang="ts">
import { computed, onMounted, reactive } from 'vue';
import { storeToRefs } from 'pinia';
import { Eye, RefreshCw, RotateCcw, Search, X } from 'lucide-vue-next';
import AppButton from '@/components/ui/AppButton.vue';
import DataSourceBadge from '@/components/ui/DataSourceBadge.vue';
import EmptyState from '@/components/ui/EmptyState.vue';
import ErrorState from '@/components/ui/ErrorState.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import PageHeader from '@/components/ui/PageHeader.vue';
import { useAuditStore } from './audit.store';
import type { AuditAction, AuditDomaine, AuditFilters, AuditJsonValue } from './audit.types';

const auditStore = useAuditStore();
const {
  items,
  selected,
  filters,
  page,
  total,
  totalPages,
  derniereActiviteUtc,
  loading,
  loadingDetail,
  error
} = storeToRefs(auditStore);

const draftFilters = reactive<AuditFilters>({ ...filters.value });

const actions: AuditAction[] = [
  'Creation',
  'Modification',
  'Validation',
  'Annulation',
  'ImportReussi',
  'ImportPartiel',
  'ImportEchoue',
  'ImportDuplicate',
  'Acquittement',
  'Resolution',
  'Ignorance',
  'Recalcul'
];

const domaines: AuditDomaine[] = [
  'InventaireFonderie',
  'DechetFonderie',
  'ControlePoidsFonderie',
  'ReceptionUsinage2',
  'RebutUsinage2',
  'ImportX3',
  'Alerte'
];

const formatDateTime = (value: string | null): string => {
  if (!value) return 'Non disponible';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Non disponible';
  return new Intl.DateTimeFormat('fr-FR', {
    dateStyle: 'short',
    timeStyle: 'medium',
    timeZone: 'UTC'
  }).format(date);
};

const formatValue = (value: AuditJsonValue | undefined): string => {
  if (value === undefined || value === null) return 'Non renseigné';
  if (Array.isArray(value)) return value.length ? value.map(formatValue).join(', ') : 'Liste vide';
  if (typeof value === 'object') {
    const values = Object.entries(value).map(([key, child]) => `${key} : ${formatValue(child)}`);
    return values.length ? values.join(' · ') : 'Objet vide';
  }
  if (typeof value === 'boolean') return value ? 'Oui' : 'Non';
  return String(value);
};

const comparisonRows = computed(() => {
  const before = selected.value?.anciennesValeurs ?? {};
  const after = selected.value?.nouvellesValeurs ?? {};
  const keys = [...new Set([...Object.keys(before), ...Object.keys(after)])].sort((a, b) => a.localeCompare(b));
  return keys.map((key) => ({ key, before: before[key], after: after[key] }));
});

const informationRows = computed(() => Object.entries(selected.value?.informations ?? {}));

const applyFilters = (): Promise<void> => auditStore.setFilters({ ...draftFilters });

const resetFilters = async (): Promise<void> => {
  await auditStore.resetFilters();
  Object.assign(draftFilters, filters.value);
};

onMounted(() => auditStore.load());
</script>

<template>
  <main class="audit-page space-y-5">
    <PageHeader
      title="Journal d'audit"
      description="Historique persistant et immuable des mutations métier et des imports."
    >
      <template #actions>
        <AppButton variant="secondary" :disabled="loading" @click="auditStore.load">
          <template #icon><RefreshCw class="h-4 w-4" aria-hidden="true" /></template>
          Actualiser
        </AppButton>
      </template>
    </PageHeader>

    <DataSourceBadge vertical="audit" />

    <section class="grid gap-3 sm:grid-cols-2" aria-label="Résumé du journal">
      <article class="surface-card p-4">
        <span class="text-xs font-semibold text-slate-500 uppercase">Nombre total</span>
        <strong class="mt-1 block text-2xl text-slate-900">{{ total }}</strong>
      </article>
      <article class="surface-card p-4">
        <span class="text-xs font-semibold text-slate-500 uppercase">Dernière activité</span>
        <strong class="mt-1 block text-sm text-slate-900">{{ formatDateTime(derniereActiviteUtc) }} UTC</strong>
      </article>
    </section>

    <form class="audit-filters surface-card" aria-label="Filtres du journal d'audit" @submit.prevent="applyFilters">
      <label><span>Depuis</span><input v-model="draftFilters.dateDepuis" type="date" :disabled="loading" /></label>
      <label><span>Jusqu'au</span><input v-model="draftFilters.dateJusqua" type="date" :disabled="loading" /></label>
      <label><span>Utilisateur</span><input v-model.trim="draftFilters.utilisateur" type="search" :disabled="loading" /></label>
      <label><span>Rôle</span><input v-model.trim="draftFilters.role" type="search" :disabled="loading" /></label>
      <label>
        <span>Action</span>
        <select v-model="draftFilters.action" :disabled="loading">
          <option value="">Toutes</option>
          <option v-for="action in actions" :key="action" :value="action">{{ action }}</option>
        </select>
      </label>
      <label>
        <span>Domaine</span>
        <select v-model="draftFilters.domaine" :disabled="loading">
          <option value="">Tous</option>
          <option v-for="domaine in domaines" :key="domaine" :value="domaine">{{ domaine }}</option>
        </select>
      </label>
      <label><span>Entité</span><input v-model.trim="draftFilters.entite" type="search" :disabled="loading" /></label>
      <label>
        <span>Référence métier</span>
        <input v-model.trim="draftFilters.referenceMetier" type="search" :disabled="loading" />
      </label>
      <label>
        <span>Correlation ID</span>
        <input v-model.trim="draftFilters.correlationId" type="search" :disabled="loading" />
      </label>
      <label class="audit-filters__search">
        <span>Recherche</span>
        <input v-model.trim="draftFilters.recherche" type="search" placeholder="Résumé, source, identifiant…" :disabled="loading" />
      </label>
      <div class="audit-filters__actions">
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

    <LoadingState v-if="loading" />
    <ErrorState
      v-else-if="error"
      title="Journal d'audit indisponible"
      :message="error"
      @retry="auditStore.load"
    />
    <EmptyState
      v-else-if="!items.length"
      title="Aucune trace pour ces filtres"
      message="Le journal ne contient aucune donnée fictive. Modifiez les filtres ou réalisez une action métier."
    />

    <section v-else class="surface-card overflow-hidden">
      <div class="overflow-x-auto">
        <table class="audit-table">
          <thead>
            <tr>
              <th>Date UTC</th>
              <th>Utilisateur</th>
              <th>Rôle</th>
              <th>Action</th>
              <th>Domaine</th>
              <th>Entité</th>
              <th>Référence</th>
              <th>Résumé</th>
              <th>Correlation ID</th>
              <th class="text-right">Détail</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="trace in items" :key="trace.id">
              <td class="whitespace-nowrap font-mono text-xs">{{ formatDateTime(trace.dateUtc) }}</td>
              <td class="font-semibold">{{ trace.utilisateurNom }}</td>
              <td>{{ trace.role }}</td>
              <td><span class="audit-chip">{{ trace.action }}</span></td>
              <td>{{ trace.domaine }}</td>
              <td><strong>{{ trace.entite }}</strong><small>{{ trace.entiteId }}</small></td>
              <td>{{ trace.referenceMetier ?? '—' }}</td>
              <td class="min-w-64">{{ trace.resume ?? '—' }}</td>
              <td><code>{{ trace.correlationId }}</code></td>
              <td class="text-right">
                <AppButton size="sm" variant="ghost" :disabled="loadingDetail" @click="auditStore.loadDetail(trace.id)">
                  <template #icon><Eye class="h-4 w-4" aria-hidden="true" /></template>
                  Détail
                </AppButton>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <footer class="flex items-center justify-between border-t border-slate-200 px-4 py-3 text-sm">
        <span>Page {{ page }} sur {{ totalPages }}</span>
        <div class="flex gap-2">
          <AppButton size="sm" variant="secondary" :disabled="page <= 1 || loading" @click="auditStore.changePage(page - 1)">
            Précédent
          </AppButton>
          <AppButton
            size="sm"
            variant="secondary"
            :disabled="page >= totalPages || loading"
            @click="auditStore.changePage(page + 1)"
          >
            Suivant
          </AppButton>
        </div>
      </footer>
    </section>

    <div v-if="selected" class="audit-dialog-backdrop" role="presentation" @click.self="auditStore.closeDetail">
      <section class="audit-dialog" role="dialog" aria-modal="true" aria-label="Détail de la trace d'audit">
        <header class="flex items-start justify-between gap-4 border-b border-slate-200 p-5">
          <div>
            <p class="text-xs font-bold text-blue-800 uppercase">{{ selected.action }} · {{ selected.domaine }}</p>
            <h2 class="mt-1 text-lg font-bold text-slate-900">{{ selected.entite }}</h2>
            <p class="mt-1 text-sm text-slate-500">{{ selected.resume ?? 'Aucun résumé' }}</p>
          </div>
          <button class="rounded-lg p-2 text-slate-500 hover:bg-slate-100" aria-label="Fermer" @click="auditStore.closeDetail">
            <X class="h-5 w-5" aria-hidden="true" />
          </button>
        </header>

        <div class="space-y-6 overflow-y-auto p-5">
          <section>
            <h3>Métadonnées</h3>
            <dl class="audit-metadata">
              <div><dt>Date UTC</dt><dd>{{ formatDateTime(selected.dateUtc) }}</dd></div>
              <div><dt>Utilisateur</dt><dd>{{ selected.utilisateurNom }} ({{ selected.utilisateurId }})</dd></div>
              <div><dt>Email</dt><dd>{{ selected.utilisateurEmail ?? 'Non renseigné' }}</dd></div>
              <div><dt>Rôle</dt><dd>{{ selected.role }}</dd></div>
              <div><dt>Entité ID</dt><dd>{{ selected.entiteId }}</dd></div>
              <div><dt>Référence</dt><dd>{{ selected.referenceMetier ?? 'Non renseignée' }}</dd></div>
              <div><dt>Source</dt><dd>{{ selected.source }}</dd></div>
              <div><dt>Correlation ID</dt><dd><code>{{ selected.correlationId }}</code></dd></div>
            </dl>
          </section>

          <section>
            <h3>Valeurs avant / après</h3>
            <div v-if="comparisonRows.length" class="overflow-x-auto rounded-lg border border-slate-200">
              <table class="audit-comparison">
                <thead><tr><th>Champ</th><th>Avant</th><th>Après</th></tr></thead>
                <tbody>
                  <tr v-for="row in comparisonRows" :key="row.key">
                    <td class="font-semibold">{{ row.key }}</td>
                    <td>{{ formatValue(row.before) }}</td>
                    <td>{{ formatValue(row.after) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <p v-else class="audit-empty-detail">Aucune valeur avant/après pour cet événement.</p>
          </section>

          <section>
            <h3>Informations complémentaires</h3>
            <dl v-if="informationRows.length" class="audit-information">
              <div v-for="([key, value]) in informationRows" :key="key">
                <dt>{{ key }}</dt><dd>{{ formatValue(value) }}</dd>
              </div>
            </dl>
            <p v-else class="audit-empty-detail">Aucune information complémentaire.</p>
          </section>
        </div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.audit-filters {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(170px, 1fr));
  gap: 0.9rem;
  padding: 1rem;
}

.audit-filters label {
  display: grid;
  gap: 0.35rem;
  color: #475569;
  font-size: 0.75rem;
  font-weight: 700;
}

.audit-filters input,
.audit-filters select {
  min-height: 2.5rem;
  border: 1px solid #cbd5e1;
  border-radius: 0.5rem;
  background: white;
  padding: 0.5rem 0.65rem;
  color: #0f172a;
  font-size: 0.82rem;
}

.audit-filters__search { grid-column: span 2; }
.audit-filters__actions { display: flex; align-items: end; gap: 0.5rem; }

.audit-table,
.audit-comparison { width: 100%; border-collapse: collapse; }
.audit-table th,
.audit-table td,
.audit-comparison th,
.audit-comparison td { padding: 0.75rem; border-bottom: 1px solid #e2e8f0; text-align: left; vertical-align: top; }
.audit-table th,
.audit-comparison th { background: #f8fafc; color: #475569; font-size: 0.68rem; letter-spacing: 0.04em; text-transform: uppercase; }
.audit-table td { color: #475569; font-size: 0.78rem; }
.audit-table small { display: block; margin-top: 0.2rem; color: #94a3b8; font-family: monospace; }
.audit-table code { overflow-wrap: anywhere; font-size: 0.7rem; }
.audit-chip { display: inline-flex; border-radius: 999px; background: #eff6ff; padding: 0.2rem 0.55rem; color: #1d4ed8; font-weight: 700; }

.audit-dialog-backdrop { position: fixed; inset: 0; z-index: 50; display: flex; justify-content: flex-end; background: rgb(15 23 42 / 0.45); }
.audit-dialog { display: flex; width: min(780px, 100%); height: 100%; flex-direction: column; background: white; box-shadow: -12px 0 30px rgb(15 23 42 / 0.18); }
.audit-dialog h3 { margin-bottom: 0.65rem; color: #0f172a; font-size: 0.85rem; font-weight: 800; text-transform: uppercase; }
.audit-metadata,
.audit-information { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0.65rem; }
.audit-metadata div,
.audit-information div { border-radius: 0.55rem; background: #f8fafc; padding: 0.7rem; }
.audit-metadata dt,
.audit-information dt { color: #64748b; font-size: 0.68rem; font-weight: 700; text-transform: uppercase; }
.audit-metadata dd,
.audit-information dd { margin-top: 0.25rem; overflow-wrap: anywhere; color: #1e293b; font-size: 0.8rem; }
.audit-comparison td { color: #334155; font-size: 0.78rem; }
.audit-empty-detail { border-radius: 0.5rem; background: #f8fafc; padding: 0.8rem; color: #64748b; font-size: 0.8rem; }

@media (max-width: 700px) {
  .audit-filters__search { grid-column: auto; }
  .audit-metadata,
  .audit-information { grid-template-columns: 1fr; }
}
</style>
