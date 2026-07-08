<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  status: string;
}>();

const config = computed(() => {
  const normalized = props.status.toLowerCase().replace(/é/g, 'e').replace(/\s/g, '');

  switch (normalized) {
    case 'normal':
    case 'valide':
    case 'synchronise':
    case 'actif':
    case 'ok':
      return { bg: 'bg-emerald-50 border-emerald-100 text-emerald-700', label: props.status };
    case 'averifier':
    case 'encontrole':
    case 'brouillon':
    case 'avec_ecart': // from bilan
    case 'avececart':
      return { bg: 'bg-amber-50 border-amber-100 text-amber-700', label: props.status };
    case 'critique':
    case 'inactif':
    case 'anomalie':
      return { bg: 'bg-rose-50 border-rose-100 text-rose-700', label: props.status };
    case 'cloture':
      return { bg: 'bg-slate-100 border-slate-200 text-slate-700', label: props.status };
    case 'justifie':
      return { bg: 'bg-blue-50 border-blue-100 text-blue-700', label: props.status };
    case 'regularise':
      return { bg: 'bg-teal-50 border-teal-100 text-teal-700', label: props.status };
    default:
      return { bg: 'bg-slate-50 border-slate-100 text-slate-600', label: props.status };
  }
});
</script>

<template>
  <span
    class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[10px] font-bold border"
    :class="config.bg"
  >
    <span
      class="w-1.5 h-1.5 rounded-full"
      :class="{
        'bg-emerald-500': config.bg.includes('emerald'),
        'bg-amber-500': config.bg.includes('amber'),
        'bg-rose-500': config.bg.includes('rose'),
        'bg-slate-400': config.bg.includes('slate'),
        'bg-blue-500': config.bg.includes('blue'),
        'bg-teal-500': config.bg.includes('teal')
      }"
    ></span>
    {{ config.label }}
  </span>
</template>
