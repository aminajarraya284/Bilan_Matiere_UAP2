import type {
  RapportCatalogueItem,
  RapportMetadata,
  RapportPreview,
  RapportsFilters
} from './rapports.types';

export const reportMetadata: RapportMetadata = {
  code: 'RPT-001',
  titre: 'Bilan Fonderie mensuel',
  genereLeUtc: '2026-01-31T12:00:00Z',
  periode: '2026-01',
  unitePrincipale: 'kg',
  estPartiel: true,
  avertissements: ['Une ligne avec données manquantes.'],
  generePar: 'Admin Test',
  unites: ['kg'],
  sources: ['Bilan Fonderie'],
  filtresAppliques: { periode: '2026-01' }
};

export const reportCatalogue: RapportCatalogueItem[] = [
  {
    code: 'RPT-001',
    titre: 'Bilan Fonderie mensuel',
    description: 'Projection du bilan Fonderie existant.',
    module: 'Fonderie',
    unitePrincipale: 'kg',
    filtres: ['periode'],
    formats: ['CSV'],
    disponible: true,
    motifIndisponibilite: null
  },
  {
    code: 'RPT-004',
    titre: 'Détail par OF',
    description: "Contexte réel d'un OF.",
    module: 'Transverse',
    unitePrincipale: null,
    filtres: ['numeroOF', 'periode'],
    formats: ['CSV'],
    disponible: true,
    motifIndisponibilite: null
  }
];

export const defaultReportFilters: RapportsFilters = {
  periode: '2026-01',
  numeroOF: '',
  date: '2026-01-31',
  statut: '',
  severite: '',
  codeTypeMatiere: '',
  dateDepuis: '2026-01-01',
  dateJusqua: '2026-01-31'
};

export const reportPreview: RapportPreview = {
  metadata: reportMetadata,
  columns: [
    { label: 'Type matière' },
    { label: 'Stock début', unite: 'kg' },
    { label: 'Écart', unite: 'kg' },
    { label: 'Statut' }
  ],
  rows: [
    {
      id: 'matiere-1',
      cells: ['M3000', null, 3.5, 'Incomplet']
    }
  ],
  total: 1,
  summary: [{ label: 'Écart total', value: null, unite: 'kg' }]
};

export const fonderieApiPayload = {
  metadata: reportMetadata,
  donnees: {
    periode: '2026-01',
    lignes: [
      {
        typeMatiereId: 'matiere-1',
        codeTypeMatiere: 'M3000',
        periode: '2026-01',
        stockDebutKg: null,
        sortiesMagasinX3Kg: 10,
        inputKg: null,
        stockFinKg: 5,
        dechetsFonderieKg: 1,
        receptionsUsinage2Kg: 2,
        outputKg: 8,
        ecartKg: null,
        tauxEcartPourcent: null,
        anomalies: ['INVENTAIRE_DEBUT_MANQUANT'],
        statut: 'Incomplet'
      }
    ],
    totalInputKg: null,
    totalOutputKg: 8,
    totalEcartKg: null,
    nombreLignes: 1,
    nombreLignesDonneesManquantes: 1,
    nombreAnomalies: 1
  }
};
