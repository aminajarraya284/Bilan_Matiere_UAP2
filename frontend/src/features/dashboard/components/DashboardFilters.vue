<script setup lang="ts">
import { computed } from 'vue';
import { Filter, RotateCcw } from 'lucide-vue-next';
import AppButton from '@/components/ui/AppButton.vue';

const props = defineProps<{
  month: string;
  year: string;
  typeMatiere: string;
  numeroOF: string;
  typeMatieres: string[];
  numerosOF: string[];
  loading?: boolean;
}>();

const emit = defineEmits<{
  'update:month': [value: string];
  'update:year': [value: string];
  'update:typeMatiere': [value: string];
  'update:numeroOF': [value: string];
  apply: [];
  reset: [];
}>();

const years = computed(() => {
  const current = Number(props.year) || new Date().getFullYear();
  return Array.from(new Set([current - 1, current, current + 1])).sort((a, b) => b - a);
});

const months = [
  { value: '01', label: 'Janvier' },
  { value: '02', label: 'Février' },
  { value: '03', label: 'Mars' },
  { value: '04', label: 'Avril' },
  { value: '05', label: 'Mai' },
  { value: '06', label: 'Juin' },
  { value: '07', label: 'Juillet' },
  { value: '08', label: 'Août' },
  { value: '09', label: 'Septembre' },
  { value: '10', label: 'Octobre' },
  { value: '11', label: 'Novembre' },
  { value: '12', label: 'Décembre' }
];
</script>

<template>
  <form class="dashboard-filters dashboard-card" aria-label="Filtres du tableau de bord" @submit.prevent="emit('apply')">
    <div class="dashboard-filters__title">
      <Filter class="h-4 w-4" aria-hidden="true" />
      <span>Filtres</span>
    </div>

    <label class="dashboard-filter-field">
      <span>Mois</span>
      <select :value="month" :disabled="loading" @change="emit('update:month', ($event.target as HTMLSelectElement).value)">
        <option v-for="item in months" :key="item.value" :value="item.value">{{ item.label }}</option>
      </select>
    </label>

    <label class="dashboard-filter-field">
      <span>Année</span>
      <select :value="year" :disabled="loading" @change="emit('update:year', ($event.target as HTMLSelectElement).value)">
        <option v-for="item in years" :key="item" :value="String(item)">{{ item }}</option>
      </select>
    </label>

    <label class="dashboard-filter-field">
      <span>Type matière</span>
      <select
        :value="typeMatiere"
        :disabled="loading"
        @change="emit('update:typeMatiere', ($event.target as HTMLSelectElement).value)"
      >
        <option value="all">Toutes les matières</option>
        <option v-for="item in typeMatieres" :key="item" :value="item">{{ item }}</option>
      </select>
    </label>

    <label class="dashboard-filter-field">
      <span>OF</span>
      <select
        :value="numeroOF"
        :disabled="loading"
        @change="emit('update:numeroOF', ($event.target as HTMLSelectElement).value)"
      >
        <option value="all">Tous les OF</option>
        <option v-for="item in numerosOF" :key="item" :value="item">{{ item }}</option>
      </select>
    </label>

    <div class="dashboard-filters__actions">
      <AppButton type="submit" size="sm" :loading="loading">Appliquer</AppButton>
      <AppButton type="button" size="sm" variant="secondary" :disabled="loading" @click="emit('reset')">
        <template #icon><RotateCcw class="h-3.5 w-3.5" aria-hidden="true" /></template>
        Réinitialiser
      </AppButton>
    </div>
  </form>
</template>

<style scoped>
.dashboard-filters {
  display: grid;
  grid-template-columns: auto repeat(4, minmax(8.5rem, 1fr)) auto;
  gap: 0.75rem;
  align-items: end;
  margin-bottom: 1.25rem;
  padding: 1rem;
  border: 1px solid #1e3a8a;
  border-radius: 0.9rem;
  background: linear-gradient(135deg, #0f172a 0%, #111827 58%, #172554 100%);
  box-shadow: 0 18px 44px -36px rgba(15, 23, 42, 0.95);
}

.dashboard-filters__title {
  display: inline-flex;
  min-height: 2.35rem;
  align-items: center;
  gap: 0.45rem;
  color: #bfdbfe;
  font-size: 0.75rem;
  font-weight: 850;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.dashboard-filter-field {
  display: grid;
  gap: 0.3rem;
}

.dashboard-filter-field span {
  color: #cbd5e1;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.dashboard-filter-field select {
  min-height: 2.35rem;
  width: 100%;
  border: 1px solid #334155;
  border-radius: 0.55rem;
  background: #0b1220;
  color: #f8fafc;
  font-size: 0.78rem;
  font-weight: 650;
}

.dashboard-filter-field select:disabled {
  cursor: not-allowed;
  opacity: 0.7;
}

.dashboard-filters__actions {
  display: flex;
  gap: 0.5rem;
  justify-content: flex-end;
}

@media (max-width: 1180px) {
  .dashboard-filters {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .dashboard-filters__title,
  .dashboard-filters__actions {
    grid-column: 1 / -1;
  }
}

@media (max-width: 640px) {
  .dashboard-filters {
    grid-template-columns: minmax(0, 1fr);
  }

  .dashboard-filters__actions {
    flex-direction: column;
  }
}
</style>
