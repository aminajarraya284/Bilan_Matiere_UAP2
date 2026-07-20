<script setup lang="ts">
import { CheckCircle2, X, XCircle } from 'lucide-vue-next';
import { useAppToast } from '@/composables/useAppToast';

const { toastState, closeToast } = useAppToast();
</script>

<template>
  <Teleport to="body">
    <Transition name="toast-motion">
      <div
        v-if="toastState.isOpen"
        class="fixed right-4 top-4 z-[120] flex w-[min(380px,calc(100vw-2rem))] items-start gap-3 rounded-xl border bg-white p-4 shadow-[var(--shadow-dialog)]"
        :class="toastState.tone === 'success' ? 'app-toast--success' : 'app-toast--danger'"
        :role="toastState.tone === 'danger' ? 'alert' : 'status'"
        :aria-live="toastState.tone === 'danger' ? 'assertive' : 'polite'"
      >
        <CheckCircle2
          v-if="toastState.tone === 'success'"
          class="mt-0.5 h-5 w-5 shrink-0 text-[var(--color-success-strong)]"
        />
        <XCircle v-else class="mt-0.5 h-5 w-5 shrink-0 text-[var(--color-danger-strong)]" />
        <p class="min-w-0 flex-1 text-sm font-medium leading-5 text-[var(--color-text)]">{{ toastState.message }}</p>
        <button
          type="button"
          class="rounded-md p-1 text-[var(--color-text-muted)] hover:bg-primary-50 hover:text-primary-900"
          aria-label="Fermer la notification"
          @click="closeToast"
        >
          <X class="h-4 w-4" aria-hidden="true" />
        </button>
      </div>
    </Transition>
  </Teleport>
</template>
