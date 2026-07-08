<script setup lang="ts">
import { computed } from 'vue';
import { AlertTriangle, CheckCircle2, Info, XCircle } from 'lucide-vue-next';
import { useAppDialog } from '@/composables/useAppDialog';

const { dialogState, closeDialog } = useAppDialog();

const icon = computed(() => {
  if (dialogState.tone === 'success') return CheckCircle2;
  if (dialogState.tone === 'warning') return AlertTriangle;
  if (dialogState.tone === 'danger') return XCircle;
  return Info;
});

const toneClass = computed(() => {
  if (dialogState.tone === 'success') return 'bg-emerald-50 text-emerald-700 border-emerald-100';
  if (dialogState.tone === 'warning') return 'bg-amber-50 text-amber-700 border-amber-100';
  if (dialogState.tone === 'danger') return 'bg-rose-50 text-rose-700 border-rose-100';
  return 'bg-sky-50 text-sky-700 border-sky-100';
});
</script>

<template>
  <Teleport to="body">
    <transition name="fade">
      <div
        v-if="dialogState.isOpen"
        class="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/45 px-4 py-6"
        role="dialog"
        aria-modal="true"
      >
        <div class="w-full max-w-md rounded-2xl border border-white/70 bg-white shadow-2xl shadow-slate-900/20">
          <div class="flex items-start gap-3 p-5">
            <div :class="['rounded-xl border p-2 shrink-0', toneClass]">
              <component :is="icon" class="w-5 h-5" />
            </div>
            <div class="min-w-0 flex-1">
              <h2 class="text-sm font-black uppercase tracking-wider text-slate-800">{{ dialogState.title }}</h2>
              <p class="mt-2 text-sm leading-6 text-slate-600">{{ dialogState.message }}</p>
              <pre
                v-if="dialogState.details"
                class="mt-3 max-h-60 overflow-auto rounded-xl border border-slate-100 bg-slate-50 p-3 text-xs leading-5 text-slate-700"
                >{{ dialogState.details }}</pre>
            </div>
          </div>

          <div class="flex justify-end gap-2 border-t border-slate-100 bg-slate-50/70 px-5 py-4">
            <button
              v-if="dialogState.mode === 'confirm'"
              type="button"
              class="rounded-lg border border-slate-200 bg-white px-4 py-2 text-xs font-bold text-slate-600 hover:bg-slate-50"
              @click="closeDialog(false)"
            >
              {{ dialogState.cancelLabel }}
            </button>
            <button
              type="button"
              class="rounded-lg bg-blue-900 px-4 py-2 text-xs font-bold text-white hover:bg-blue-800"
              @click="closeDialog(true)"
            >
              {{ dialogState.confirmLabel }}
            </button>
          </div>
        </div>
      </div>
    </transition>
  </Teleport>
</template>
