import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { alertesService } from './alertes.service';
import type {
  AlerteDetail,
  AlerteListItem,
  AlerteRecalculResult,
  AlerteSummary,
  AlertesFilters
} from './alertes.types';

/**
 * Aucune période par défaut : les alertes persistantes couvrent plusieurs mois et
 * le mois courant n'est pas nécessairement celui qui porte des données. Filtrer
 * d'office sur le mois courant masquerait toutes les alertes existantes et ferait
 * passer une page peuplée pour une page vide. La période reste un filtre explicite.
 */
const defaultFilters = (): AlertesFilters => ({
  periode: '',
  statut: '',
  severite: '',
  source: '',
  numeroOF: '',
  codeArticle: '',
  recherche: ''
});

const emptySummary = (): AlerteSummary => ({ total: 0, ouvertes: 0, acquittees: 0, resolues: 0, ignorees: 0 });

const errorMessage = (cause: unknown): string =>
  cause instanceof Error && cause.message.trim()
    ? cause.message
    : 'Le chargement des alertes a échoué. Veuillez réessayer.';

export const useAlertesStore = defineStore('alertes', () => {
  const items = ref<AlerteListItem[]>([]);
  const selected = ref<AlerteDetail | null>(null);
  const filters = ref<AlertesFilters>(defaultFilters());
  const page = ref(1);
  const pageSize = ref(50);
  const total = ref(0);
  const summary = ref<AlerteSummary>(emptySummary());
  const derniereDetectionLeUtc = ref<string | null>(null);
  const lastRecalculation = ref<AlerteRecalculResult | null>(null);
  const loading = ref(false);
  const loadingDetail = ref(false);
  const processing = ref(false);
  const recalculating = ref(false);
  const error = ref<string | null>(null);
  let activeRequest = 0;

  const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize.value)));

  const load = async (): Promise<void> => {
    const requestId = ++activeRequest;
    loading.value = true;
    error.value = null;
    items.value = [];
    total.value = 0;
    summary.value = emptySummary();
    derniereDetectionLeUtc.value = null;
    try {
      const response = await alertesService.search({
        filters: filters.value,
        page: page.value,
        pageSize: pageSize.value
      });
      if (requestId !== activeRequest) return;
      items.value = response.items;
      total.value = response.total;
      summary.value = response.summary;
      derniereDetectionLeUtc.value = response.derniereDetectionLeUtc;
    } catch (cause) {
      if (requestId !== activeRequest) return;
      error.value = errorMessage(cause);
    } finally {
      if (requestId === activeRequest) loading.value = false;
    }
  };

  const loadDetail = async (id: string): Promise<void> => {
    if (loadingDetail.value) return;
    loadingDetail.value = true;
    error.value = null;
    selected.value = null;
    try {
      selected.value = await alertesService.get(id);
    } catch (cause) {
      error.value = errorMessage(cause);
    } finally {
      loadingDetail.value = false;
    }
  };

  const traiter = async (action: () => Promise<AlerteDetail>): Promise<boolean> => {
    if (processing.value) return false;
    processing.value = true;
    error.value = null;
    try {
      selected.value = await action();
      await load();
      return true;
    } catch (cause) {
      error.value = errorMessage(cause);
      return false;
    } finally {
      processing.value = false;
    }
  };

  const acquitter = (id: string, commentaire: string | null): Promise<boolean> =>
    traiter(() => alertesService.acquitter(id, { commentaire }));

  const resoudre = (id: string, commentaire: string | null): Promise<boolean> =>
    traiter(() => alertesService.resoudre(id, { commentaire }));

  const ignorer = (id: string, commentaire: string): Promise<boolean> =>
    traiter(() => alertesService.ignorer(id, { commentaire }));

  const recalculer = async (): Promise<boolean> => {
    if (recalculating.value) return false;
    // Le recalcul backend porte sur une période précise : sans période sélectionnée,
    // il n'y a rien à recalculer. On le dit explicitement plutôt que de laisser
    // partir un appel voué à un 400.
    if (!filters.value.periode.trim()) {
      error.value = 'Sélectionnez une période avant de lancer un recalcul.';
      return false;
    }
    recalculating.value = true;
    error.value = null;
    try {
      lastRecalculation.value = await alertesService.recalculer(filters.value.periode);
      page.value = 1;
      await load();
      return true;
    } catch (cause) {
      error.value = errorMessage(cause);
      return false;
    } finally {
      recalculating.value = false;
    }
  };

  const applyFilters = async (nextFilters: AlertesFilters): Promise<void> => {
    filters.value = { ...nextFilters };
    page.value = 1;
    await load();
  };

  const resetFilters = async (): Promise<void> => {
    filters.value = defaultFilters();
    page.value = 1;
    await load();
  };

  const goToPage = async (nextPage: number): Promise<void> => {
    page.value = Math.min(Math.max(nextPage, 1), totalPages.value);
    await load();
  };

  const closeDetail = (): void => {
    selected.value = null;
  };

  return {
    items,
    selected,
    filters,
    page,
    pageSize,
    total,
    summary,
    totalPages,
    derniereDetectionLeUtc,
    lastRecalculation,
    loading,
    loadingDetail,
    processing,
    recalculating,
    error,
    load,
    loadDetail,
    acquitter,
    resoudre,
    ignorer,
    recalculer,
    applyFilters,
    resetFilters,
    goToPage,
    closeDetail
  };
});
