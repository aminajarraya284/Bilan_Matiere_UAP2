import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { BUSINESS_RULES } from '@/config/businessRules';
import { reportExporter } from './exporter';
import { rapportsService } from './rapports.service';
import type {
  RapportCatalogueItem,
  RapportCode,
  RapportPreview,
  RapportsFilters
} from './rapports.types';

const today = (): string => new Date().toISOString().slice(0, 10);

const defaultFilters = (): RapportsFilters => ({
  periode: BUSINESS_RULES.periodeParDefaut,
  numeroOF: '',
  date: today(),
  statut: '',
  severite: '',
  codeTypeMatiere: '',
  dateDepuis: BUSINESS_RULES.dateDebutParDefaut,
  dateJusqua: BUSINESS_RULES.dateFinParDefaut
});

const errorMessage = (cause: unknown): string =>
  cause instanceof Error && cause.message.trim()
    ? cause.message
    : 'Le chargement du rapport a échoué. Veuillez réessayer.';

export const useRapportsStore = defineStore('rapports', () => {
  const catalogue = ref<RapportCatalogueItem[]>([]);
  const selectedCode = ref<RapportCode | null>(null);
  const filters = ref<RapportsFilters>(defaultFilters());
  const preview = ref<RapportPreview | null>(null);
  const page = ref(1);
  const pageSize = ref(50);
  const loadingCatalogue = ref(false);
  const loadingPreview = ref(false);
  const exporting = ref(false);
  const error = ref<string | null>(null);

  const selectedReport = computed(
    () => catalogue.value.find((item) => item.code === selectedCode.value) ?? null
  );
  const totalPages = computed(() => Math.max(1, Math.ceil((preview.value?.total ?? 0) / pageSize.value)));

  const loadCatalogue = async (): Promise<void> => {
    if (loadingCatalogue.value) return;
    loadingCatalogue.value = true;
    error.value = null;
    try {
      catalogue.value = await rapportsService.getCatalogue();
      if (!selectedCode.value || !catalogue.value.some((item) => item.code === selectedCode.value)) {
        selectedCode.value = catalogue.value[0]?.code ?? null;
      }
    } catch (cause) {
      catalogue.value = [];
      selectedCode.value = null;
      error.value = errorMessage(cause);
    } finally {
      loadingCatalogue.value = false;
    }
  };

  const select = (code: RapportCode): void => {
    if (selectedCode.value === code) return;
    selectedCode.value = code;
    preview.value = null;
    error.value = null;
    page.value = 1;
  };

  const loadPreview = async (): Promise<boolean> => {
    if (!selectedCode.value || loadingPreview.value) return false;
    loadingPreview.value = true;
    error.value = null;
    try {
      preview.value = await rapportsService.preview({
        code: selectedCode.value,
        filters: filters.value,
        page: page.value,
        pageSize: pageSize.value
      });
      return true;
    } catch (cause) {
      preview.value = null;
      error.value = errorMessage(cause);
      return false;
    } finally {
      loadingPreview.value = false;
    }
  };

  const exportCsv = async (): Promise<boolean> => {
    if (!selectedCode.value || exporting.value) return false;
    exporting.value = true;
    error.value = null;
    try {
      reportExporter.download(await rapportsService.exportCsv(selectedCode.value, filters.value));
      return true;
    } catch (cause) {
      error.value = errorMessage(cause);
      return false;
    } finally {
      exporting.value = false;
    }
  };

  const goToPage = async (nextPage: number): Promise<void> => {
    page.value = Math.min(Math.max(nextPage, 1), totalPages.value);
    await loadPreview();
  };

  const resetFilters = (): void => {
    filters.value = defaultFilters();
    preview.value = null;
    page.value = 1;
  };

  return {
    catalogue,
    selectedCode,
    selectedReport,
    filters,
    preview,
    page,
    pageSize,
    totalPages,
    loadingCatalogue,
    loadingPreview,
    exporting,
    error,
    loadCatalogue,
    select,
    loadPreview,
    exportCsv,
    goToPage,
    resetFilters
  };
});
