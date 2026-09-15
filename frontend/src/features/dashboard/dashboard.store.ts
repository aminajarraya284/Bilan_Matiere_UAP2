import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { dashboardService, validateDashboardPeriod } from './dashboard.service';
import type { DashboardResponse } from './dashboard.types';

const currentPeriod = (): string => {
  const today = new Date();
  return `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;
};

const errorMessage = (error: unknown): string =>
  error instanceof Error && error.message.trim()
    ? error.message
    : 'Le chargement du tableau de bord a échoué. Veuillez réessayer.';

export const useDashboardStore = defineStore('dashboard', () => {
  const periode = ref(currentPeriod());
  const data = ref<DashboardResponse | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const lastLoadedAt = ref<string | null>(null);
  let activeRequest = 0;

  const hasData = computed(() => data.value !== null);

  const load = async (nextPeriod: string = periode.value): Promise<void> => {
    const requestId = ++activeRequest;
    loading.value = true;
    error.value = null;
    data.value = null;
    lastLoadedAt.value = null;

    try {
      const normalizedPeriod = validateDashboardPeriod(nextPeriod);
      periode.value = normalizedPeriod;
      const response = await dashboardService.getDashboard(normalizedPeriod);
      if (requestId !== activeRequest) return;
      data.value = response;
      lastLoadedAt.value = new Date().toISOString();
    } catch (cause) {
      if (requestId !== activeRequest) return;
      data.value = null;
      error.value = errorMessage(cause);
    } finally {
      if (requestId === activeRequest) loading.value = false;
    }
  };

  const refresh = async (): Promise<void> => load(periode.value);

  const setPeriode = (value: string): void => {
    periode.value = validateDashboardPeriod(value);
  };

  const reset = (): void => {
    activeRequest += 1;
    periode.value = currentPeriod();
    data.value = null;
    loading.value = false;
    error.value = null;
    lastLoadedAt.value = null;
  };

  return {
    periode,
    data,
    loading,
    error,
    lastLoadedAt,
    hasData,
    load,
    refresh,
    setPeriode,
    reset
  };
});
