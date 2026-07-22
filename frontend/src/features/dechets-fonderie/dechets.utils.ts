export const getLocalIsoDate = (date = new Date()): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

export const getCurrentPeriod = (date = new Date()): string => getLocalIsoDate(date).slice(0, 7);

export const getIsoWeek = (isoDate: string): number => {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(isoDate)) return 0;
  const [year, month, day] = isoDate.split('-').map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));
  if (Number.isNaN(date.getTime())) return 0;
  const dayOfWeek = date.getUTCDay() || 7;
  date.setUTCDate(date.getUTCDate() + 4 - dayOfWeek);
  const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
  return Math.ceil(((date.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
};

export const formatWeight = (value: number): string =>
  value.toLocaleString('fr-FR', { minimumFractionDigits: 0, maximumFractionDigits: 3 });
