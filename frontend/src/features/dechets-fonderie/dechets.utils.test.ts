import { describe, expect, it } from 'vitest';
import { getCurrentPeriod, getIsoWeek, getLocalIsoDate } from './dechets.utils';

describe('waste date utilities', () => {
  it('calculates the ISO week from the declaration date', () => {
    expect(getIsoWeek('2026-06-30')).toBe(27);
    expect(getIsoWeek('2026-07-06')).toBe(28);
  });

  it('builds dynamic local defaults', () => {
    const date = new Date(2026, 6, 22, 8, 0, 0);
    expect(getLocalIsoDate(date)).toBe('2026-07-22');
    expect(getCurrentPeriod(date)).toBe('2026-07');
  });
});
