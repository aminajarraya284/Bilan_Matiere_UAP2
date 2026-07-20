<script setup lang="ts">
import { CalendarDays, Clock3, DatabaseZap, RefreshCw } from 'lucide-vue-next';
import AppButton from '@/components/ui/AppButton.vue';

defineProps<{
  period: string;
  dateDebut: string;
  dateFin: string;
  lastSync: string;
  syncStatus: string;
  loading?: boolean;
}>();

const emit = defineEmits<{
  refresh: [];
}>();
</script>

<template>
  <header class="dashboard-header dashboard-enter" style="--delay: 0ms">
    <div class="dashboard-header__content">
      <div class="dashboard-header__eyebrow">
        <span>Administration</span>
      </div>
      <h1 class="dashboard-header__title">Tableau de bord</h1>
    </div>

    <div class="dashboard-header__meta" aria-label="Contexte du tableau de bord">
      <div class="dashboard-header__pill">
        <CalendarDays class="h-4 w-4" aria-hidden="true" />
        <span>
          <strong>Période sélectionnée</strong>
          {{ period }}
        </span>
      </div>

      <div class="dashboard-header__pill">
        <CalendarDays class="h-4 w-4" aria-hidden="true" />
        <span>
          <strong>Plage de dates</strong>
          {{ dateDebut }} → {{ dateFin }}
        </span>
      </div>

      <div class="dashboard-header__pill">
        <Clock3 class="h-4 w-4" aria-hidden="true" />
        <span>
          <strong>Dernière mise à jour</strong>
          {{ lastSync }}
        </span>
      </div>

      <div class="dashboard-header__pill dashboard-header__pill--sync">
        <DatabaseZap class="h-4 w-4" aria-hidden="true" />
        <span>
          <strong>Synchronisation X3</strong>
          {{ syncStatus }}
        </span>
      </div>

      <AppButton :loading="loading" @click="emit('refresh')">
        <template #icon><RefreshCw class="h-4 w-4" aria-hidden="true" /></template>
        Actualiser les données
      </AppButton>
    </div>
  </header>
</template>

<style scoped>
.dashboard-header {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 1.5rem;
  margin-bottom: 1.25rem;
  padding: 1.4rem;
  border: 1px solid rgba(59, 130, 246, 0.28);
  border-radius: 1rem;
  background:
    radial-gradient(circle at top left, rgba(37, 99, 235, 0.34), transparent 34rem),
    linear-gradient(135deg, #0f172a 0%, #172554 52%, #0b1220 100%);
  box-shadow: 0 24px 60px -38px rgba(15, 23, 42, 0.9);
}

.dashboard-header__content {
  min-width: 0;
}

.dashboard-header__eyebrow {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  color: #93c5fd;
  font-size: 0.7rem;
  font-weight: 850;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.dashboard-header__title {
  margin-top: 0.55rem;
  color: #f8fafc;
  font-size: clamp(1.45rem, 2.2vw, 2rem);
  font-weight: 850;
  letter-spacing: -0.035em;
  line-height: 1.1;
}

.dashboard-header__meta {
  display: grid;
  grid-template-columns: repeat(2, minmax(12rem, 1fr));
  gap: 0.75rem;
  align-items: stretch;
}

.dashboard-header__pill {
  display: flex;
  min-height: 3.25rem;
  align-items: center;
  gap: 0.65rem;
  padding: 0.7rem 0.85rem;
  border: 1px solid rgba(148, 163, 184, 0.24);
  border-radius: 0.75rem;
  background: rgba(15, 23, 42, 0.62);
  color: #93c5fd;
  box-shadow: 0 12px 30px -26px rgba(0, 0, 0, 0.7);
}

.dashboard-header__pill--sync {
  color: #5eead4;
}

.dashboard-header__pill span {
  display: grid;
  min-width: 0;
  color: #e2e8f0;
  font-size: 0.75rem;
  font-weight: 750;
  line-height: 1.25;
}

.dashboard-header__pill strong {
  color: #94a3b8;
  font-size: 0.62rem;
  font-weight: 850;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.dashboard-header__meta :deep(.app-button) {
  grid-column: 1 / -1;
  justify-content: center;
}

@media (max-width: 1180px) {
  .dashboard-header {
    grid-template-columns: minmax(0, 1fr);
  }

  .dashboard-header__meta {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 640px) {
  .dashboard-header {
    padding: 1.1rem;
  }

  .dashboard-header__meta {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
