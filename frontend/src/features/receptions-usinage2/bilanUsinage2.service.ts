import api from '@/services/api';
import type { BilanUsinage2 } from './bilanUsinage2.types';
import { isApiSource } from '@/config/dataSources';

// Le Bilan Usinage 2 en pieces est calcule server-side (jamais duplique en Vue) et n'a pas
// d'equivalent dans le jeu de donnees mock existant : il n'est disponible qu'en mode API reelle
// En source API. En mode mock, l'appelant doit afficher un état « indisponible ».
export const bilanUsinage2Service = {
  isAvailable(): boolean {
    return isApiSource('bilanU2');
  },

  async getBilan(periode: string): Promise<BilanUsinage2> {
    const res = await api.get<BilanUsinage2>(`/v1/bilans/usinage2/${encodeURIComponent(periode)}`);
    return res.data;
  }
};
