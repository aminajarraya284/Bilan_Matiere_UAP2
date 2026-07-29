<script setup lang="ts">
import { computed } from 'vue';
import { getDataSourceMode, type DataVertical } from '@/config/dataSources';

const props = defineProps<{
  vertical: DataVertical;
}>();

const mode = computed(() => getDataSourceMode(props.vertical));
const label = computed(() =>
  mode.value === 'mock'
    ? 'Mode démonstration'
    : mode.value === 'disabled'
      ? 'Fonctionnalité non disponible'
      : 'API réelle'
);
</script>

<template>
  <div
    v-if="mode !== 'api'"
    class="inline-flex w-fit items-center gap-2 rounded-full border px-3 py-1.5 text-xs font-semibold"
    :class="
      mode === 'mock'
        ? 'border-amber-200 bg-amber-50 text-amber-800'
        : 'border-slate-200 bg-slate-100 text-slate-600'
    "
    role="status"
  >
    <span
      class="h-2 w-2 rounded-full"
      :class="mode === 'mock' ? 'bg-amber-500' : 'bg-slate-400'"
      aria-hidden="true"
    ></span>
    {{ label }}
  </div>
</template>
