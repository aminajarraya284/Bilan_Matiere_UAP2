import { beforeEach, describe, expect, it, vi } from 'vitest';
const mocks = vi.hoisted(() => ({ get: vi.fn() }));
vi.mock('./api', () => ({ default: mocks }));
import { fetchAllPages } from './pagination';

describe('complete paginated lists', () => {
  beforeEach(() => vi.resetAllMocks());

  it('returns an empty list without fetching another page', async () => {
    mocks.get.mockResolvedValueOnce({ data: { items: [], total: 0 } });
    expect(await fetchAllPages('/test', 50)).toEqual([]);
    expect(mocks.get).toHaveBeenCalledOnce();
  });

  it('propagates a later page failure rather than returning a partial list', async () => {
    mocks.get
      .mockResolvedValueOnce({ data: { items: [1], total: 2 } })
      .mockRejectedValueOnce(new Error('Service indisponible'));
    await expect(fetchAllPages('/test', 1)).rejects.toThrow('Service indisponible');
  });

  it('stops when the server declares missing results but returns an empty page', async () => {
    mocks.get
      .mockResolvedValueOnce({ data: { items: [1], total: 2 } })
      .mockResolvedValueOnce({ data: { items: [], total: 2 } });
    await expect(fetchAllPages('/test', 1)).rejects.toThrow('incomplète');
    expect(mocks.get).toHaveBeenCalledTimes(2);
  });
});
