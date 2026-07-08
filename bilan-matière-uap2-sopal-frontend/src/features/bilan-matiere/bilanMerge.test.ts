import { describe, expect, it } from 'vitest';
import { mergeBilansForPeriod } from './bilanMerge';
import type { BilanMatiereReport } from './bilan.types';

const makeBilan = (id: number, periode: string, typeMatiere: string): BilanMatiereReport => ({
  id,
  periode,
  typeMatiere,
  encoursDebut: 0,
  sortiesX3: 0,
  inputTotal: 0,
  encoursFinReel: 0,
  dechetsFonderie: 0,
  receptionsUsinage2: 0,
  poidsNonReceptionne: 0,
  nombrePiecesNonReceptionnees: 0,
  outputTotal: 0,
  ecartKg: 0,
  tauxEcart: 0,
  statut: 'Brouillon',
  justification: '',
  dateCalcul: '2026-07-03'
});

describe('mergeBilansForPeriod', () => {
  it('remplace seulement la période recalculée et préserve les autres périodes', () => {
    const current = [
      makeBilan(1, '2026-06', 'M3010L0'),
      makeBilan(2, '2026-05', 'M3010L0'),
      makeBilan(3, '2026-04', 'M3000L0')
    ];
    const periodBilans = [
      { ...makeBilan(4, '2026-06', 'M3010L0'), ecartKg: 12 },
      { ...makeBilan(5, '2026-06', 'M3000L0'), ecartKg: -4 }
    ];

    const merged = mergeBilansForPeriod(current, '2026-06', periodBilans);

    expect(merged.map((b) => `${b.periode}:${b.typeMatiere}:${b.id}`)).toEqual([
      '2026-05:M3010L0:2',
      '2026-04:M3000L0:3',
      '2026-06:M3010L0:4',
      '2026-06:M3000L0:5'
    ]);
  });
});
