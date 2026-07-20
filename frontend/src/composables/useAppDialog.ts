import { reactive } from 'vue';

type DialogTone = 'info' | 'success' | 'warning' | 'danger';
type DialogMode = 'alert' | 'confirm';

type DialogOptions = {
  title?: string;
  message: string;
  details?: string;
  tone?: DialogTone;
  confirmLabel?: string;
  cancelLabel?: string;
};

const dialogState = reactive({
  isOpen: false,
  mode: 'alert' as DialogMode,
  tone: 'info' as DialogTone,
  title: '',
  message: '',
  details: '',
  confirmLabel: 'OK',
  cancelLabel: 'Annuler',
  resolve: undefined as ((confirmed: boolean) => void) | undefined
});

const openDialog = (mode: DialogMode, options: DialogOptions): Promise<boolean> => {
  if (dialogState.resolve) {
    dialogState.resolve(false);
  }

  dialogState.isOpen = true;
  dialogState.mode = mode;
  dialogState.tone = options.tone || 'info';
  dialogState.title = options.title || (mode === 'confirm' ? 'Confirmation' : 'Information');
  dialogState.message = options.message;
  dialogState.details = options.details || '';
  dialogState.confirmLabel = options.confirmLabel || (mode === 'confirm' ? 'Confirmer' : 'OK');
  dialogState.cancelLabel = options.cancelLabel || 'Annuler';

  return new Promise((resolve) => {
    dialogState.resolve = resolve;
  });
};

const closeDialog = (confirmed: boolean) => {
  const resolve = dialogState.resolve;
  dialogState.isOpen = false;
  dialogState.resolve = undefined;
  resolve?.(confirmed);
};

export const useAppDialog = () => {
  return {
    dialogState,
    alert: (message: string, options: Omit<DialogOptions, 'message'> = {}) =>
      openDialog('alert', { ...options, message }).then(() => undefined),
    confirm: (message: string, options: Omit<DialogOptions, 'message'> = {}) =>
      openDialog('confirm', { ...options, message }),
    closeDialog
  };
};
