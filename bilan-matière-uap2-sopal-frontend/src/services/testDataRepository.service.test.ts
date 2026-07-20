import { beforeEach, describe, expect, it } from 'vitest';
import { TEST_DATA_STORAGE_KEY, TEST_DATA_VERSION } from '@/mocks/sopalTestDataset';
import { testDataRepository } from './testDataRepository.service';

describe('testDataRepository storage recovery', () => {
  beforeEach(() => localStorage.clear());

  it('replaces a versioned but incomplete dataset instead of crashing', () => {
    localStorage.setItem(TEST_DATA_STORAGE_KEY, JSON.stringify({ version: TEST_DATA_VERSION }));

    const dataset = testDataRepository.getDataset();

    expect(dataset.version).toBe(TEST_DATA_VERSION);
    expect(dataset.x3OrdresFabrication.length).toBeGreaterThan(0);
    expect(dataset.receptionsUsinage2.length).toBeGreaterThan(0);
    expect(JSON.parse(localStorage.getItem(TEST_DATA_STORAGE_KEY) ?? '{}').x3OrdresFabrication).toBeInstanceOf(Array);
  });
});
