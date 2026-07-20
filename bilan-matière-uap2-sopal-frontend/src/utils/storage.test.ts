import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getStorageItem } from './storage';

describe('getStorageItem', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  afterEach(() => vi.restoreAllMocks());

  it('restores the default value when stored JSON is invalid', () => {
    localStorage.setItem('invalid-json', '{');

    expect(getStorageItem('invalid-json', [1, 2])).toEqual([1, 2]);
    expect(JSON.parse(localStorage.getItem('invalid-json') ?? 'null')).toEqual([1, 2]);
  });

  it('restores an array default when valid JSON has the wrong shape', () => {
    localStorage.setItem('wrong-array-shape', JSON.stringify({ rows: [] }));

    expect(getStorageItem('wrong-array-shape', ['default'])).toEqual(['default']);
    expect(JSON.parse(localStorage.getItem('wrong-array-shape') ?? 'null')).toEqual(['default']);
  });

  it('restores an object default when the stored value is an array', () => {
    localStorage.setItem('wrong-object-shape', JSON.stringify([]));

    expect(getStorageItem('wrong-object-shape', { enabled: true })).toEqual({ enabled: true });
  });

  it('keeps a stored value with a compatible shape', () => {
    localStorage.setItem('valid-array', JSON.stringify(['stored']));

    expect(getStorageItem('valid-array', ['default'])).toEqual(['stored']);
  });
});
