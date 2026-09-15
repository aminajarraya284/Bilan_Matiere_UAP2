import type { DashboardBilanSummary, DashboardResponse } from './dashboard.types';

const bilan = (
  code: DashboardBilanSummary['code'],
  ecart: number | null,
  unite: DashboardBilanSummary['unite'],
  nombreOf: number | null
): DashboardBilanSummary => ({
  code,
  ecart,
  unite,
  nombreLignes: 2,
  nombreOf,
  nombreOk: 2,
  nombreAVerifier: 0,
  nombreAnomalies: 0,
  nombreDonneesManquantes: 0,
  statut: 'OK'
});

export const createDashboardResponse = (
  overrides: Partial<DashboardResponse> = {}
): DashboardResponse => ({
  periode: '2026-01',
  calculeLeUtc: '2026-01-31T10:30:00Z',
  statutGlobal: 'OK',
  fonderie: bilan('FONDERIE', 12.5, 'kg', null),
  fonderieDetail: {
    totalInputKg: 24.5,
    totalOutputKg: 12,
    totalEcartKg: 12.5,
    tauxEcartPourcent: 51.02,
    nombreTypesComplets: 1,
    nombreTypesAvecDonneesManquantesOuAnomalies: 0,
    types: [
      {
        codeTypeMatiere: 'M3000L0', stockDebutKg: 10, sortiesMagasinX3Kg: 2, inputKg: 12,
        stockFinKg: 5, dechetsFonderieKg: 1, receptionsUsinage2Kg: 2, outputKg: 8,
        ecartKg: 4, tauxEcartPourcent: 33.33, anomalies: [], statut: 'Complet'
      }
    ]
  },
  usinage2: bilan('USINAGE2', 8, 'pieces', 2),
  uap2: bilan('UAP2', 0, 'pieces', 2),
  operations: {
    inventairesBrouillon: 1,
    inventairesValides: 2,
    receptionsNonReceptionnees: 3,
    receptionsValidees: 4,
    controlesPoidsAmbigus: null,
    ofReferencesManquantes: 1
  },
  imports: {
    reussis: 5,
    partiels: 1,
    echoues: 0,
    fichiersIdentiques: 2,
    dernierImportUtc: '2026-01-30T08:00:00Z'
  },
  qualiteDonnees: {
    nombreAnomalies: 1,
    nombreDonneesManquantes: 0,
    nombreConversionsImpossibles: 0,
    nombreReferencesManquantes: 1,
    statutGlobal: 'AVerifier'
  },
  avertissements: [
    {
      code: 'REFERENCES_OF_MANQUANTES',
      source: 'Usinage2',
      message: 'Une référence OF est manquante.',
      statut: 'Partiel'
    }
  ],
  ...overrides
});
