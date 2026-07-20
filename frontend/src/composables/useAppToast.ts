import { reactive } from 'vue';

type ToastTone = 'success' | 'danger';

const toastState = reactive({
  isOpen: false,
  message: '',
  tone: 'success' as ToastTone
});

let closeTimer: ReturnType<typeof setTimeout> | undefined;

const showToast = (message: string, tone: ToastTone, duration = 4000) => {
  if (closeTimer) clearTimeout(closeTimer);
  toastState.message = message;
  toastState.tone = tone;
  toastState.isOpen = true;
  closeTimer = setTimeout(() => {
    toastState.isOpen = false;
    closeTimer = undefined;
  }, duration);
};

const closeToast = () => {
  if (closeTimer) clearTimeout(closeTimer);
  closeTimer = undefined;
  toastState.isOpen = false;
};

export const useAppToast = () => ({
  toastState,
  success: (message: string, duration?: number) => showToast(message, 'success', duration),
  error: (message: string, duration?: number) => showToast(message, 'danger', duration),
  closeToast
});
