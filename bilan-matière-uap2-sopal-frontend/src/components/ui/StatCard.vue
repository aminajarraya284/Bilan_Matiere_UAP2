<script setup lang="ts">
withDefaults(
  defineProps<{
    title: string;
    value: string | number;
    unit?: string;
    colorClass?: string;
    trend?: string;
    trendType?: 'up' | 'down' | 'neutral' | 'custom-amber' | 'custom-red' | 'custom-green' | 'custom-gray';
    borderClass?: string;
    tone?: 'default' | 'success' | 'warning' | 'danger';
  }>(),
  {
    colorClass: 'bg-blue-50 text-blue-700',
    trendType: 'neutral',
    tone: 'default'
  }
);
</script>

<template>
  <article
    class="surface-card stat-card motion-card relative overflow-hidden p-5"
    :class="[`stat-card--${tone}`, borderClass]"
  >
    <div class="mb-3 flex items-start justify-between gap-3">
      <span class="text-sm font-semibold text-[var(--color-text-secondary)]">{{ title }}</span>
      <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg" :class="colorClass" aria-hidden="true">
        <slot name="icon" />
      </div>
    </div>

    <div class="flex items-baseline gap-1.5">
      <span class="numeric-value text-kpi tracking-tight text-[var(--color-text)]">{{ value }}</span>
      <span v-if="unit" class="text-sm font-medium text-[var(--color-text-secondary)]">{{ unit }}</span>
    </div>

    <div v-if="trend" class="mt-3">
      <span
        class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold"
        :class="{
          'bg-emerald-50 text-emerald-700': trendType === 'up' || trendType === 'custom-green',
          'bg-rose-50 text-rose-700': trendType === 'down' || trendType === 'custom-red',
          'bg-slate-100 text-slate-700': trendType === 'neutral' || trendType === 'custom-gray',
          'bg-amber-50 text-amber-800': trendType === 'custom-amber'
        }"
      >
        <span
          class="h-1.5 w-1.5 rounded-full"
          :class="{
            'bg-emerald-500': trendType === 'up' || trendType === 'custom-green',
            'bg-rose-500': trendType === 'down' || trendType === 'custom-red',
            'bg-slate-500': trendType === 'neutral' || trendType === 'custom-gray',
            'bg-amber-500': trendType === 'custom-amber'
          }"
          aria-hidden="true"
        ></span>
        {{ trend }}
      </span>
    </div>
  </article>
</template>
