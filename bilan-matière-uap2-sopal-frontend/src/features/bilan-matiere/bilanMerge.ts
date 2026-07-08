import type { BilanMatiereReport } from './bilan.types';

export const mergeBilansForPeriod = (
  currentBilans: BilanMatiereReport[],
  periode: string,
  periodBilans: BilanMatiereReport[]
): BilanMatiereReport[] => {
  const otherPeriods = currentBilans.filter((b) => b.periode !== periode);
  return [...otherPeriods, ...periodBilans].sort((a, b) => a.id - b.id);
};
