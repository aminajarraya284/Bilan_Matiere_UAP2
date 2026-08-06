import type { DashboardBilanSummary, DashboardResponse } from './dashboard.types';

const emptyBilan = (
  code: DashboardBilanSummary['code'],
  unite: DashboardBilanSummary['unite']
): DashboardBilanSummary => ({
  code,
  ecart: null,
  unite,
  nombreLignes: 0,
  nombreOf: null,
  nombreOk: 0,
  nombreAVerifier: 0,
  nombreAnomalies: 0,
  nombreDonneesManquantes: 0,
  statut: 'NonDisponible'
});

/** Démonstration explicite sans chiffre métier fabriqué. Jamais utilisée après une erreur API. */
export const createDashboardMock = (periode: string): DashboardResponse => ({
  periode,
  calculeLeUtc: new Date().toISOString(),
  statutGlobal: 'NonDisponible',
  fonderie: emptyBilan('FONDERIE', 'kg'),
  usinage2: emptyBilan('USINAGE2', 'pieces'),
  uap2: emptyBilan('UAP2', 'pieces'),
  operations: {
    inventairesBrouillon: 0,
    inventairesValides: 0,
    receptionsNonReceptionnees: 0,
    receptionsValidees: 0,
    controlesPoidsAmbigus: null,
    ofReferencesManquantes: 0
  },
  imports: {
    reussis: 0,
    partiels: 0,
    echoues: 0,
    fichiersIdentiques: 0,
    dernierImportUtc: null
  },
  qualiteDonnees: {
    nombreAnomalies: 0,
    nombreDonneesManquantes: 0,
    nombreConversionsImpossibles: 0,
    nombreReferencesManquantes: 0,
    statutGlobal: 'NonDisponible'
  },
  avertissements: [
    {
      code: 'MODE_DEMONSTRATION_SANS_DONNEES',
      source: 'Dashboard',
      message: 'Le mode démonstration ne présente aucun chiffre métier fictif.',
      statut: 'NonDisponible'
    }
  ]
});
