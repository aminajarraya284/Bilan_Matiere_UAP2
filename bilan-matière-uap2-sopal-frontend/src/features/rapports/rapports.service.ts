import api from '@/services/api';
import { RapportsListType } from './rapports.types';

const mockRapports: RapportsListType[] = [
  {
    id: 1,
    titre: 'Rapport Bilan Matière Mensuel',
    description: 'Entrées, sorties et écarts.',
    module: 'Bilan',
    dateGeneration: '2026-06-30',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 2,
    titre: 'Rapport Déchets Fonderie',
    description: 'Bavures, poudre et causes.',
    module: 'Déchets',
    dateGeneration: '2026-06-30',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 3,
    titre: 'Rapport Réceptions Usinage 2',
    description: 'Entrées U2 origine Fonderie.',
    module: 'Réceptions',
    dateGeneration: '2026-06-29',
    formatDisponible: ['PDF']
  },
  {
    id: 4,
    titre: 'Rapport Synchronisation X3',
    description: 'Sorties magasin X3.',
    module: 'Sage X3',
    dateGeneration: '2026-06-28',
    formatDisponible: ['Excel']
  },
  {
    id: 5,
    titre: 'Rapport Inventaire physique',
    description: 'Inventaire fin de mois.',
    module: 'Inventaire',
    dateGeneration: '2026-06-30',
    formatDisponible: ['PDF', 'Excel']
  }
];

export const rapportsService = {
  async getRapports(periode = '2026-06', dateDebut?: string, dateFin?: string): Promise<RapportsListType[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return mockRapports.map((rapport) => ({
        ...rapport,
        dateGeneration: dateFin || rapport.dateGeneration
      }));
    }
    const res = await api.get<RapportsListType[]>('/rapports', { params: { periode, dateDebut, dateFin } });
    return res.data;
  }
};
