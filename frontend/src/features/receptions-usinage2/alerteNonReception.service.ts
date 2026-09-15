import api from '@/services/api';
import type { AlerteNonReception } from './alerteNonReception.types';

/**
 * Alerte journaliere des pieces non receptionnees.
 *
 * Calculee integralement par le backend a partir des Sorties MP de la date : aucune
 * agregation ni formule metier n'est reproduite ici. En cas d'erreur API, l'erreur est
 * propagee — jamais de repli sur des donnees de demonstration.
 */
export const alerteNonReceptionService = {
  isAvailable(): boolean {
    return true;
  },

  async getParDate(date: string, page = 1, pageSize = 50): Promise<AlerteNonReception> {
    const { data } = await api.get<AlerteNonReception>('/v1/receptions-usinage2/non-receptionnees', {
      params: { date, page, pageSize }
    });
    return data;
  }
};
