import api from './api';

/** Charge une liste complete sans exposer une premiere page comme un total. */
export async function fetchAllPages<T>(url: string, pageSize: number): Promise<T[]> {
  const items: T[] = [];
  for (let page = 1; ; page += 1) {
    const { data } = await api.get<{ items: T[]; total: number }>(url, {
      params: { page, pageSize }
    });
    if (!Array.isArray(data.items) || !Number.isSafeInteger(data.total) || data.total < 0) {
      throw new Error('La liste reçue est invalide. Veuillez réessayer.');
    }
    items.push(...data.items);
    if (items.length >= data.total) return items;
    if (data.items.length === 0) {
      throw new Error('La liste reçue est incomplète. Veuillez réessayer.');
    }
  }
}
