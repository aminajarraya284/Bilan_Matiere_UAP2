<script setup lang="ts">
import { computed, type Component } from 'vue';
import { Info, CheckCircle2, AlertTriangle, AlertOctagon } from 'lucide-vue-next';

type AlertType = 'info' | 'success' | 'warning' | 'attention' | 'danger';
const props = withDefaults(defineProps<{ type?: AlertType; title?: string; message?: string }>(), {
  type: 'info',
  title: '',
  message: ''
});

const alertStyles: Record<AlertType, { icon: Component; container: string }> = {
  info: { icon: Info, container: 'alert-box--info' },
  success: { icon: CheckCircle2, container: 'alert-box--success' },
  warning: { icon: AlertTriangle, container: 'alert-box--warning' },
  attention: { icon: AlertTriangle, container: 'alert-box--attention' },
  danger: { icon: AlertOctagon, container: 'alert-box--danger' }
};
const currentAlert = computed(() => alertStyles[props.type]);
const accessibilityRole = computed(() => (props.type === 'danger' || props.type === 'warning' ? 'alert' : 'status'));
</script>

<template>
  <div
    class="motion-alert flex items-start gap-3 rounded-lg border p-4 text-sm"
    :class="currentAlert.container"
    :role="accessibilityRole"
    :aria-live="type === 'danger' ? 'assertive' : 'polite'"
  >
    <component :is="currentAlert.icon" class="mt-0.5 h-5 w-5 shrink-0" aria-hidden="true" />
    <div class="min-w-0 space-y-1 leading-relaxed">
      <h5 v-if="title" class="text-sm font-semibold text-current">{{ title }}</h5>
      <p v-if="$slots.default || message" class="text-[13px] text-current/85">
        <slot>{{ message }}</slot>
      </p>
    </div>
  </div>
</template>
