import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import SortiesX3View from './SortiesX3View.vue';

const mocks = vi.hoisted(() => ({
  getMouvements: vi.fn(),
  importExcelFiles: vi.fn(),
  success: vi.fn(),
  error: vi.fn()
}));

vi.mock('./sortiesX3.service', () => ({
  sortiesX3Service: {
    getMouvements: mocks.getMouvements,
    importExcelFiles: mocks.importExcelFiles
  }
}));

vi.mock('@/composables/useAppToast', () => ({
  useAppToast: () => ({
    success: mocks.success,
    error: mocks.error
  })
}));

describe('SortiesX3View', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    mocks.getMouvements.mockReset();
    mocks.importExcelFiles.mockReset();
    mocks.success.mockReset();
    mocks.error.mockReset();
    mocks.getMouvements.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 100 });
  });

  it('recherche les sorties magasin par numéro OF normalisé', async () => {
    const wrapper = mount(SortiesX3View, {
      global: {
        stubs: {
          PageHeader: true,
          LoadingState: true,
          ProgressSteps: true
        }
      }
    });
    await flushPromises();
    mocks.getMouvements.mockClear();

    const input = wrapper.get('#sorties-of-search');
    expect(input.attributes('placeholder')).toBe('Rechercher par OF exact');

    await input.setValue(' of 12345 ');
    await vi.advanceTimersByTimeAsync(300);
    await flushPromises();

    expect(mocks.getMouvements).toHaveBeenCalledTimes(1);
    expect(mocks.getMouvements).toHaveBeenCalledWith(
      expect.objectContaining({
        numeroOF: 'OF12345',
        signal: expect.any(AbortSignal)
      })
    );

    wrapper.unmount();
  });
});
