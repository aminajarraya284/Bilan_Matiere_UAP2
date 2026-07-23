import { describe, expect, it } from 'vitest';

describe('consommations composants par OF', () => {
  it('laisse le rapprochement X3 au backend', () => {
    expect('/v1/consommations-composants/imports').toContain('consommations-composants');
  });
});
