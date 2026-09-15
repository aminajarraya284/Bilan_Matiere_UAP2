import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { auditService } from './audit.service';
import type { AuditDetail, AuditFilters, AuditListItem } from './audit.types';

const defaultFilters = (): AuditFilters => ({
  dateDepuis: '',
  dateJusqua: '',
  utilisateur: '',
  role: '',
  action: '',
  domaine: '',
  entite: '',
  referenceMetier: '',
  correlationId: '',
  recherche: ''
});

const errorMessage = (cause: unknown): string =>
  cause instanceof Error && cause.message.trim()
    ? cause.message
    : "Le chargement du journal d'audit a échoué. Veuillez réessayer.";

export const useAuditStore = defineStore('audit', () => {
  const items = ref<AuditListItem[]>([]);
  const selected = ref<AuditDetail | null>(null);
  const filters = ref<AuditFilters>(defaultFilters());
  const page = ref(1);
  const pageSize = ref(50);
  const total = ref(0);
  const derniereActiviteUtc = ref<string | null>(null);
  const loading = ref(false);
  const loadingDetail = ref(false);
  const error = ref<string | null>(null);
  let activeRequest = 0;

  const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize.value)));

  const load = async (): Promise<void> => {
    const requestId = ++activeRequest;
    loading.value = true;
    error.value = null;
    items.value = [];
    total.value = 0;
    derniereActiviteUtc.value = null;
    try {
      const response = await auditService.search({
        filters: filters.value,
        page: page.value,
        pageSize: pageSize.value
      });
      if (requestId !== activeRequest) return;
      items.value = response.items;
      total.value = response.total;
      derniereActiviteUtc.value = response.derniereActiviteUtc;
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
      selected.value = await auditService.getDetail(id);
    } catch (cause) {
      error.value = errorMessage(cause);
    } finally {
      loadingDetail.value = false;
    }
  };

  const setFilters = async (nextFilters: AuditFilters): Promise<void> => {
    filters.value = { ...nextFilters };
    page.value = 1;
    await load();
  };

  const changePage = async (nextPage: number): Promise<void> => {
    page.value = Math.min(Math.max(nextPage, 1), totalPages.value);
    await load();
  };

  const resetFilters = async (): Promise<void> => {
    filters.value = defaultFilters();
    page.value = 1;
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
    totalPages,
    derniereActiviteUtc,
    loading,
    loadingDetail,
    error,
    load,
    loadDetail,
    setFilters,
    changePage,
    resetFilters,
    closeDetail
  };
});
