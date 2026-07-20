<script setup lang="ts">
defineProps<{
  title?: string;
  description?: string;
}>();
</script>

<template>
  <section class="dashboard-section">
    <header v-if="title || description" class="dashboard-section__header">
      <h2 v-if="title">{{ title }}</h2>
      <p v-if="description">{{ description }}</p>
    </header>
    <slot />
  </section>
</template>

<style scoped>
.dashboard-section + .dashboard-section {
  margin-top: 1.75rem;
}

.dashboard-section__header {
  margin-bottom: 1rem;
}

.dashboard-section__header h2 {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  color: var(--color-text);
  font-size: 1rem;
  font-weight: 700;
}

.dashboard-section__header h2::before {
  width: 3px;
  height: 1.125rem;
  border-radius: 999px;
  background: var(--color-primary-600);
  content: '';
}

.dashboard-section__header p {
  margin-top: 0.25rem;
  color: var(--color-text-secondary);
  font-size: 0.8125rem;
}
</style>
