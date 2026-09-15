import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn(), download: vi.fn() }));
vi.mock('@/services/api', () => ({ default: { get: mocks.get, post: mocks.post } }));
vi.mock('@/features/rapports/exporter', () => ({ reportExporter: { download: mocks.download } }));

import { archivageService } from './archivage.service';

describe('archivageService', () => {
  beforeEach(() => vi.clearAllMocks());

  it('demande au backend de recalculer et archiver la période', async () => {
    const detail = { metadata: { periode: '2026-07' }, snapshot: { periode: '2026-07' } };
    mocks.post.mockResolvedValue({ data: detail });
    await expect(archivageService.enregistrer('2026-07')).resolves.toBe(detail);
    expect(mocks.post).toHaveBeenCalledWith('/v1/periodes-bilan/2026-07/enregistrer');
  });

  it('lit le détail exclusivement via la route archive', async () => {
    mocks.get.mockResolvedValue({ data: { metadata: {}, snapshot: {} } });
    await archivageService.get('2026-07');
    expect(mocks.get).toHaveBeenCalledWith('/v1/periodes-bilan/2026-07');
  });

  it('télécharge le rapport archivé depuis le snapshot backend', async () => {
    const blob = new Blob(['rapport']);
    mocks.get.mockResolvedValue({ data: blob });
    await archivageService.telechargerRapport('2026-07');
    expect(mocks.get).toHaveBeenCalledWith('/v1/periodes-bilan/2026-07/rapport', { responseType: 'blob' });
    expect(mocks.download).toHaveBeenCalledWith({ blob, fileName: 'rapport-bilan-matiere-2026-07.md' });
  });
});
