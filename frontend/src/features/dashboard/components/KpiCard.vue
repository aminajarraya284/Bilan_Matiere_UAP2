<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { formatNumber } from '@/utils/formatters';

const props = withDefaults(
  defineProps<{
    title: string;
    value: number | null;
    unit?: string;
    secondary: string;
    decimals?: number;
    nullLabel?: string;
    tone?: 'default' | 'warning' | 'danger' | 'success';
  }>(),
  {
    unit: '',
    decimals: 0,
    nullLabel: 'Non disponible',
    tone: 'default'
  }
);

const displayedValue = ref<number | null>(props.value);
let animationFrame = 0;

const formattedValue = computed(() =>
  displayedValue.value === null ? props.nullLabel : formatNumber(displayedValue.value, props.decimals)
);

const animateValue = (target: number | null) => {
  cancelAnimationFrame(animationFrame);
  if (target === null) {
    displayedValue.value = null;
    return;
  }
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (reduceMotion) {
    displayedValue.value = target;
    return;
  }

  const startValue = displayedValue.value ?? 0;
  const startedAt = performance.now();
  const duration = 500;
  const tick = (now: number) => {
    const progress = Math.min(1, (now - startedAt) / duration);
    const eased = 1 - Math.pow(1 - progress, 3);
    displayedValue.value = startValue + (target - startValue) * eased;
    if (progress < 1) animationFrame = requestAnimationFrame(tick);
  };
  animationFrame = requestAnimationFrame(tick);
};

onMounted(() => animateValue(props.value));
watch(() => props.value, animateValue);
onBeforeUnmount(() => cancelAnimationFrame(animationFrame));
</script>

<template>
  <article class="dashboard-kpi dashboard-card" :class="`dashboard-kpi--${tone}`">
    <div class="dashboard-kpi__topline">
      <div class="dashboard-kpi__icon"><slot name="icon" /></div>
      <slot name="badge" />
    </div>
    <div>
      <h2 class="dashboard-kpi__title">{{ title }}</h2>
      <p class="dashboard-kpi__value">
        <span>{{ formattedValue }}</span>
        <small v-if="unit && displayedValue !== null">{{ unit }}</small>
      </p>
    </div>
    <div v-if="$slots.default" class="dashboard-kpi__details"><slot /></div>
    <p class="dashboard-kpi__secondary">{{ secondary }}</p>
  </article>
</template>

<style scoped>
.dashboard-kpi {
  display: flex;
  min-height: 11.75rem;
  flex-direction: column;
  justify-content: space-between;
  padding: 1.25rem;
  border: 1px solid #cbd5e1;
  border-top: 3px solid #1e3a8a;
  border-radius: 0.75rem;
  background: var(--color-surface);
  box-shadow: 0 18px 38px -32px rgba(15, 23, 42, 0.72);
}

.dashboard-kpi::after {
  position: absolute;
  top: 0;
  right: 1rem;
  width: 2.5rem;
  height: 3px;
  border-radius: 0 0 999px 999px;
  background: #2563eb;
  content: '';
}

.dashboard-kpi--warning {
  border-color: #f59e0b;
  border-top-color: #b45309;
  background: var(--color-surface);
}

.dashboard-kpi--warning::after {
  background: #b45309;
}

.dashboard-kpi--danger {
  border-color: #dc2626;
  border-top-color: #991b1b;
  background: var(--color-surface);
}

.dashboard-kpi--danger::after {
  background: #991b1b;
}

.dashboard-kpi--success {
  border-color: #10b981;
  border-top-color: #047857;
  background: var(--color-surface);
}

.dashboard-kpi--success::after {
  background: #047857;
}

.dashboard-kpi__topline {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
}

.dashboard-kpi__icon {
  display: grid;
  width: 2.625rem;
  height: 2.625rem;
  place-items: center;
  border-radius: 0.625rem;
  background: #1e3a8a;
  color: var(--color-surface);
}

.dashboard-kpi--warning .dashboard-kpi__icon {
  background: #fef3c7;
  color: #92400e;
}

.dashboard-kpi--danger .dashboard-kpi__icon {
  background: #fee2e2;
  color: #7f1d1d;
}

.dashboard-kpi--success .dashboard-kpi__icon {
  background: #d1fae5;
  color: #065f46;
}

.dashboard-kpi__title {
  margin-top: 1rem;
  color: var(--color-text-secondary);
  font-size: 0.8125rem;
  font-weight: 700;
  letter-spacing: 0.005em;
}

.dashboard-kpi__value {
  display: flex;
  align-items: baseline;
  gap: 0.4rem;
  margin-top: 0.35rem;
  color: var(--color-text);
  font-variant-numeric: tabular-nums;
}

.dashboard-kpi__value span {
  font-size: 1.875rem;
  font-weight: 750;
  letter-spacing: -0.035em;
  line-height: 1.1;
}

.dashboard-kpi__value small {
  color: var(--color-text-secondary);
  font-size: 0.8125rem;
  font-weight: 650;
}

.dashboard-kpi--warning .dashboard-kpi__value {
  color: var(--color-warning-strong);
}

.dashboard-kpi--danger .dashboard-kpi__value {
  color: var(--color-danger-strong);
}

.dashboard-kpi--success .dashboard-kpi__value {
  color: var(--color-success-strong);
}

.dashboard-kpi__secondary {
  margin-top: 0.75rem;
  color: var(--color-text-secondary);
  font-size: 0.75rem;
  line-height: 1.4;
}

.dashboard-kpi__details {
  color: var(--color-text-secondary);
  font-size: 0.8125rem;
}
</style>
