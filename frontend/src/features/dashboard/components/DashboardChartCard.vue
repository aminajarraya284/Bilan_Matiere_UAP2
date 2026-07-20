<script setup lang="ts">
defineProps<{
  title: string;
  description?: string;
}>();
</script>

<template>
  <section class="dashboard-chart-card dashboard-card surface-card">
    <header class="dashboard-chart-card__header">
      <div>
        <h2>{{ title }}</h2>
        <p v-if="description">{{ description }}</p>
      </div>
      <div v-if="$slots.filter" class="dashboard-chart-card__filter"><slot name="filter" /></div>
    </header>
    <div class="dashboard-chart-card__body"><slot /></div>
    <footer v-if="$slots.legend" class="dashboard-chart-card__legend"><slot name="legend" /></footer>
  </section>
</template>

<style scoped>
.dashboard-chart-card {
  padding: 1.5rem;
}

.dashboard-chart-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--color-border);
}

.dashboard-chart-card__header h2 {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  color: var(--color-text);
  font-size: 0.9375rem;
  font-weight: 700;
}

.dashboard-chart-card__header h2::before {
  width: 3px;
  height: 1.125rem;
  border-radius: 999px;
  background: var(--color-primary-600);
  content: '';
}

.dashboard-chart-card__header p {
  max-width: 42rem;
  margin-top: 0.3rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
  line-height: 1.45;
}

.dashboard-chart-card__body {
  margin-top: 1.25rem;
}

.dashboard-chart-card__legend {
  margin-top: 1.25rem;
  padding-top: 1rem;
  border-top: 1px solid var(--color-border);
}

@media (max-width: 640px) {
  .dashboard-chart-card {
    padding: 1.25rem;
  }

  .dashboard-chart-card__header {
    flex-direction: column;
  }
}
</style>
