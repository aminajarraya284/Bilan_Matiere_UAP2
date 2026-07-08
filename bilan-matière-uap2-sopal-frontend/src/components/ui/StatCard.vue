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
  }>(),
  {
    colorClass: 'bg-blue-50 text-blue-600'
  }
);
</script>

<template>
  <div
    class="group relative overflow-hidden bg-white/95 rounded-xl border p-5 shadow-sm hover:shadow-lg transition-all duration-200 dashboard-card"
    :class="borderClass || 'border-slate-100'"
  >
    <div
      class="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-sky-200 via-emerald-200 to-amber-200 opacity-90"
    ></div>
    <div class="flex items-center justify-between mb-2">
      <span class="text-[11px] font-bold uppercase tracking-wider text-slate-400">{{ title }}</span>
      <div
        class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 transition-transform duration-200 group-hover:scale-110"
        :class="colorClass"
      >
        <slot name="icon"></slot>
      </div>
    </div>

    <div class="flex items-baseline gap-1">
      <span class="text-2xl font-black text-slate-800 tracking-tight font-display">{{ value }}</span>
      <span v-if="unit" class="text-xs font-semibold text-slate-400 font-mono">{{ unit }}</span>
    </div>

    <!-- Trend Badge -->
    <div v-if="trend" class="mt-2.5">
      <span
        class="inline-flex items-center gap-1 text-[10px] font-bold px-2 py-0.5 rounded-md"
        :class="{
          'text-emerald-600 bg-emerald-50': trendType === 'up' || trendType === 'custom-green',
          'text-rose-600 bg-rose-50': trendType === 'down' || trendType === 'custom-red',
          'text-slate-600 bg-slate-100': trendType === 'neutral' || trendType === 'custom-gray',
          'text-amber-600 bg-amber-50': trendType === 'custom-amber'
        }"
      >
        <span
          v-if="trendType === 'up' || trendType === 'custom-green'"
          class="w-1 h-1 rounded-full bg-emerald-300"
        ></span>
        <span v-if="trendType === 'down' || trendType === 'custom-red'" class="w-1 h-1 rounded-full bg-rose-300"></span>
        {{ trend }}
      </span>
    </div>
  </div>
</template>
