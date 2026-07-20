<script setup lang="ts">
import { computed } from 'vue';

type BadgeTone = 'info' | 'success' | 'warning' | 'attention' | 'danger' | 'neutral' | 'closed';

const props = defineProps<{ status: string; variant?: BadgeTone }>();

const normalizedStatus = computed(() =>
  props.status
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[\s_-]/g, '')
);

const tone = computed(() => {
  if (props.variant) return props.variant;
  if (
    [
      'normal',
      'conforme',
      'actif',
      'ok',
      'regularise',
      'valide',
      'validee',
      'synchronise',
      'reussi',
      'succes',
      'importe'
    ].includes(normalizedStatus.value)
  )
    return 'success';
  if (
    [
      'averifier',
      'encontrole',
      'alerte',
      'avececart',
      'saisiemanuelle',
      'synchronisationpartielle',
      'partiel',
      'acompleter',
      'avalider',
      'asynchroniser',
      'surcharge'
    ].includes(normalizedStatus.value)
  ) {
    return 'warning';
  }
  if (['provisoire', 'attention'].includes(normalizedStatus.value)) return 'attention';
  if (['critique', 'anomalie', 'erreur', 'echec', 'supprime', 'annule'].includes(normalizedStatus.value))
    return 'danger';
  if (normalizedStatus.value === 'cloture') return 'closed';
  if (['information', 'encours', 'lance', 'justifie', 'excel', 'x3api'].includes(normalizedStatus.value)) return 'info';
  if (['brouillon', 'inactif', 'nondisponible'].includes(normalizedStatus.value)) return 'neutral';
  return 'neutral';
});
</script>

<template>
  <span class="status-badge" :class="`status-badge--${tone}`">
    <span class="status-badge__dot" aria-hidden="true"></span>
    {{ status }}
  </span>
</template>
