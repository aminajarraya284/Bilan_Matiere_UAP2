import api from '@/services/api';
import {
  calculateInventoryReportSummary,
  calculateBilanFonderie,
  calculateBilanUsinage2,
  calculateBilanUap2
} from '@/services/bilanCalculation.service';
import { testDataRepository } from '@/services/testDataRepository.service';
import { RapportsListType } from './rapports.types';
import { isMockSource } from '@/config/dataSources';

const REPORT_DEFINITIONS: Omit<RapportsListType, 'dateGeneration'>[] = [
  {
    id: 1,
    titre: 'Rapport Bilan Fonderie',
    description: 'Entrées, sorties et écarts par type matière.',
    module: 'Bilan',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 2,
    titre: 'Rapport Bilan Usinage 2',
    description: 'Contrôle EMR et réceptions U2 par OF et article.',
    module: 'Bilan',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 3,
    titre: 'Rapport Bilan UAP2',
    description: 'Bilan par OF avec mouvements nets en kilogrammes.',
    module: 'Bilan',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 4,
    titre: 'Rapport Inventaire Fonderie',
    description: 'Conteneurs, catégories et fours visibles dans l’inventaire.',
    module: 'Inventaire',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 5,
    titre: 'Rapport Alertes',
    description: 'Alertes poids et alertes bilan calculées.',
    module: 'Alertes',
    formatDisponible: ['PDF']
  }
];

export const rapportsService = {
  async getRapports(periode = '2026-06', dateDebut?: string, dateFin?: string): Promise<RapportsListType[]> {
    if (isMockSource('rapports')) {
      const dataset = testDataRepository.getDataset();
      // Execute the same calculation services used by the dashboard and the bilans
      // before exposing the report catalogue. This keeps report data derived from the repository.
      calculateBilanFonderie(dataset, periode);
      calculateBilanUsinage2(dataset, periode);
      calculateBilanUap2(dataset, periode);
      calculateInventoryReportSummary(dataset, periode);
      return REPORT_DEFINITIONS.map((rapport) => ({
        ...rapport,
        description: `${rapport.description} Période ${periode}.`,
        dateGeneration: dateFin || dataset.dateFin
      }));
    }
    const res = await api.get<RapportsListType[]>('/rapports', { params: { periode, dateDebut, dateFin } });
    return res.data;
  }
};
