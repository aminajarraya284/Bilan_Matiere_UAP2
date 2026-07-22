<script setup lang="ts">
import { computed } from 'vue';
import { AlertTriangle, CheckCircle2, Info, XCircle } from 'lucide-vue-next';
import { useAppDialog } from '@/composables/useAppDialog';
import AppButton from './AppButton.vue';

const { dialogState, closeDialog } = useAppDialog();

const icon = computed(() => {
  if (dialogState.tone === 'success') return CheckCircle2;
  if (dialogState.tone === 'warning') return AlertTriangle;
  if (dialogState.tone === 'danger') return XCircle;
  return Info;
});

const toneClass = computed(() => {
  if (dialogState.tone === 'success') return 'semantic-success';
  if (dialogState.tone === 'warning') return 'semantic-warning';
  if (dialogState.tone === 'danger') return 'semantic-danger';
  return 'semantic-info';
});
</script>

<template>
  <Teleport to="body">
    <transition name="dialog-motion">
      <div
        v-if="dialogState.isOpen"
        class="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/45 px-4 py-6"
        role="dialog"
        aria-modal="true"
      >
        <div
          class="app-dialog-panel w-full max-w-md rounded-xl border border-[var(--color-border)] bg-white shadow-[var(--shadow-dialog)]"
        >
          <div class="flex items-start gap-3 p-5">
            <div :class="['rounded-xl border p-2 shrink-0', toneClass]">
              <component :is="icon" class="w-5 h-5" />
            </div>
            <div class="min-w-0 flex-1">
              <h2 class="text-lg font-semibold text-primary-900">{{ dialogState.title }}</h2>
              <p class="mt-2 text-sm leading-6 text-[var(--color-text-secondary)]">{{ dialogState.message }}</p>
              <pre
                v-if="dialogState.details"
                class="mt-3 max-h-60 overflow-auto rounded-xl border border-slate-100 bg-slate-50 p-3 text-xs leading-5 text-slate-700"
                >{{ dialogState.details }}</pre>
            </div>
          </div>

          <div
            class="flex justify-end gap-2 border-t border-[var(--color-border)] bg-[var(--color-surface-soft)] px-5 py-4"
          >
            <AppButton
              v-if="dialogState.mode === 'confirm'"
              type="button"
              variant="secondary"
              @click="closeDialog(false)"
            >
              {{ dialogState.cancelLabel }}
            </AppButton>
            <AppButton
              type="button"
              :variant="dialogState.tone === 'danger' ? 'danger' : 'primary'"
              @click="closeDialog(true)"
            >
              {{ dialogState.confirmLabel }}
            </AppButton>
          </div>
        </div>
      </div>
    </transition>
  </Teleport>
</template>
