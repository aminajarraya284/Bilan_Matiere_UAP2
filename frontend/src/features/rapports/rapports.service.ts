import { assertApiSource } from '@/config/dataSources';
import api from '@/services/api';
import type { BilanFonderieApiResponse, BilanUap2ApiResponse, BilanUsinage2ApiResponse } from '@/types/api.types';
import type { AlerteListItem, PagedAlertesResponse } from '@/features/alertes/alertes.types';
import type {
  InventaireSynthese,
  NonReceptionResponse,
  PagedRapportImports,
  RapportCatalogueItem,
  RapportCode,
  RapportDownload,
  RapportFilterKey,
  RapportMetadata,
  RapportOfDetail,
  RapportPreview,
  RapportResponse,
  RapportsFilters,
  RapportTableColumn,
  RapportTableRow
} from './rapports.types';

interface PreviewOptions {
  code: RapportCode;
  filters: RapportsFilters;
  page: number;
  pageSize: number;
}

const compactParams = (
  filters: RapportsFilters,
  keys: readonly RapportFilterKey[],
  page?: number,
  pageSize?: number
): Record<string, string | number> => {
  const params: Record<string, string | number> = {};
  for (const key of keys) {
    const value = filters[key].trim();
    if (value) params[key] = value;
  }
  if (page !== undefined) params.page = page;
  if (pageSize !== undefined) params.pageSize = pageSize;
  return params;
};

const metadataOf = (payload: { metadata: RapportMetadata }): RapportMetadata => payload.metadata;

const table = (
  metadata: RapportMetadata,
  columns: RapportTableColumn[],
  rows: RapportTableRow[],
  total: number,
  summary: RapportPreview['summary'] = []
): RapportPreview => ({ metadata, columns, rows, total, summary });

const normalizeFonderie = (payload: RapportResponse<BilanFonderieApiResponse>): RapportPreview =>
  table(
    payload.metadata,
    [
      { label: 'Type matière' },
      { label: 'Stock début', unite: 'kg' },
      { label: 'Sorties X3', unite: 'kg' },
      { label: 'Input', unite: 'kg' },
      { label: 'Stock fin', unite: 'kg' },
      { label: 'Déchets', unite: 'kg' },
      { label: 'Réceptions U2', unite: 'kg' },
      { label: 'Output', unite: 'kg' },
      { label: 'Écart', unite: 'kg' },
      { label: 'Statut' }
    ],
    payload.donnees.lignes.map((item) => ({
      id: item.typeMatiereId,
      cells: [
        item.codeTypeMatiere,
        item.stockDebutKg,
        item.sortiesMagasinX3Kg,
        item.inputKg,
        item.stockFinKg,
        item.dechetsFonderieKg,
        item.receptionsUsinage2Kg,
        item.outputKg,
        item.ecartKg,
        item.statut
      ]
    })),
    payload.donnees.nombreLignes,
    [
      { label: 'Input total', value: payload.donnees.totalInputKg, unite: 'kg' },
      { label: 'Output total', value: payload.donnees.totalOutputKg, unite: 'kg' },
      { label: 'Écart total', value: payload.donnees.totalEcartKg, unite: 'kg' },
      { label: 'Données manquantes', value: payload.donnees.nombreLignesDonneesManquantes }
    ]
  );

const normalizeUsinage2 = (payload: RapportResponse<BilanUsinage2ApiResponse>): RapportPreview =>
  table(
    payload.metadata,
    [
      { label: 'OF' },
      { label: 'Article' },
      { label: 'Atelier' },
      { label: 'Type matière' },
      { label: 'CTRL EMR', unite: 'pièces' },
      { label: 'Rebut EMR', unite: 'pièces' },
      { label: 'Nombre de pièces', unite: 'pièces' },
      { label: 'Réceptionnées', unite: 'pièces' },
      { label: 'Non réceptionnées', unite: 'pièces' },
      { label: 'Bilan U2', unite: 'pièces' },
      { label: 'Statut' }
    ],
    payload.donnees.lignes.map((item) => ({
      id: `${item.numeroOF}|${item.codeArticle}`,
      cells: [
        item.numeroOF,
        item.codeArticle,
        item.atelierOrigine,
        item.typeMatiere,
        item.quantiteCtrlEmrPieces,
        item.rebutEmrPieces,
        item.nombreDePieces,
        item.quantiteReceptionneePieces,
        item.quantiteNonReceptionneePieces,
        item.bilanUsinage2Pieces,
        item.statutReception
      ]
    })),
    payload.donnees.total,
    [
      { label: 'Réceptionnées', value: payload.donnees.quantiteTotaleReceptionneePieces, unite: 'pièces' },
      { label: 'Rebut U2', value: payload.donnees.totalRebutUsinage2Pieces, unite: 'pièces' },
      { label: 'Bilan U2', value: payload.donnees.totalBilanUsinage2Pieces, unite: 'pièces' },
      { label: 'Données manquantes', value: payload.donnees.nombreLignesDonneesManquantes }
    ]
  );

const normalizeUap2 = (payload: RapportResponse<BilanUap2ApiResponse>): RapportPreview =>
  table(
    payload.metadata,
    [
      { label: 'OF' },
      { label: 'Article' },
      { label: 'Type matière' },
      { label: 'CTRL référence', unite: 'pièces' },
      { label: 'Rebut SCI', unite: 'pièces' },
      { label: 'Rebut EMR', unite: 'pièces' },
      { label: 'Rebut TRN', unite: 'pièces' },
      { label: 'Sortie MP', unite: 'pièces' },
      { label: 'Sortie MP', unite: 'kg' },
      { label: 'Écart UAP2', unite: 'pièces' },
      { label: 'Statut' }
    ],
    payload.donnees.lignes.map((item) => ({
      id: `${item.numeroOF}|${item.codeArticle}`,
      cells: [
        item.numeroOF,
        item.codeArticle,
        item.codeTypeMatiere,
        item.quantiteCtrlPieces,
        item.rebutSciPieces,
        item.rebutEmrPieces,
        item.rebutTrnPieces,
        item.quantiteSortieMpPieces,
        item.sortieMpKg,
        item.ecartUap2Pieces,
        item.statut
      ]
    })),
    payload.donnees.total,
    [
      { label: 'CTRL total', value: payload.donnees.totalQuantiteCtrlPieces, unite: 'pièces' },
      { label: 'Écart total', value: payload.donnees.totalEcartUap2Pieces, unite: 'pièces' },
      { label: 'Sortie MP totale', value: payload.donnees.totalSortieMpKg, unite: 'kg' },
      { label: 'Données manquantes', value: payload.donnees.nombreLignesDonneesManquantes }
    ]
  );

const normalizeOf = (payload: RapportOfDetail): RapportPreview => {
  const rows: RapportTableRow[] = [
    ...payload.receptions.map((item) => ({
      id: `reception-${item.id}`,
      cells: ['Réception', item.dateReception, item.codeArticle, item.nombrePiecesReceptionnees, 'pièces', item.statut]
    })),
    ...payload.processus.map((item) => ({
      id: `processus-${item.id}`,
      cells: ['Processus', item.dateProduction, item.codePoste, item.quantiteCtrlPieces, 'pièces', item.processus]
    })),
    ...payload.mouvements.map((item) => ({
      id: `mouvement-${item.id}`,
      cells: [
        'Mouvement',
        item.dateImputation,
        item.codeArticle,
        item.quantiteSignee,
        item.unite,
        item.statutRapprochement
      ]
    })),
    ...payload.controlesPoids.map((item) => ({
      id: `controle-${item.id}`,
      cells: [
        'Contrôle poids',
        item.dateControle,
        item.codeArticle,
        item.poidsPhysiqueUnitaireKg,
        'kg/pièce',
        item.statut
      ]
    })),
    ...payload.references.map((item, index) => ({
      id: `reference-${index}`,
      cells: [
        item.typeReference,
        null,
        item.codeComposant ?? item.codeArticle,
        item.quantiteLien,
        item.uniteQuantite,
        item.codePoste ?? item.numeroOperation
      ]
    })),
    ...(payload.bilanUsinage2
      ? [
          {
            id: 'bilan-usinage2',
            cells: [
              'Bilan Usinage 2',
              null,
              payload.bilanUsinage2.codeArticle,
              payload.bilanUsinage2.bilanUsinage2Pieces,
              'pièces',
              payload.bilanUsinage2.statutReception
            ]
          }
        ]
      : []),
    ...(payload.bilanUap2
      ? [
          {
            id: 'bilan-uap2',
            cells: [
              'Bilan UAP2',
              null,
              payload.bilanUap2.codeArticle,
              payload.bilanUap2.ecartUap2Pieces,
              'pièces',
              payload.bilanUap2.statut
            ]
          }
        ]
      : []),
    ...payload.alertes.map((item) => ({
      id: `alerte-${item.id}`,
      cells: ['Alerte', item.detecteeLeUtc, item.code, null, null, `${item.statut} — ${item.titre}`]
    }))
  ];
  return table(
    metadataOf(payload),
    [
      { label: 'Section' },
      { label: 'Date' },
      { label: 'Code' },
      { label: 'Valeur' },
      { label: 'Unité' },
      { label: 'Statut / contexte' }
    ],
    rows,
    rows.length,
    [
      { label: 'OF', value: payload.contexte.numeroOF },
      { label: 'Article', value: payload.contexte.codeArticle },
      { label: 'Type matière', value: payload.contexte.typeMatiere },
      { label: 'Rapprochement', value: payload.contexte.statutRapprochement }
    ]
  );
};

const traitementAlerte = (
  item: AlerteListItem
): { utilisateur: string | null; dateUtc: string | null } => {
  if (item.resolueLeUtc) return { utilisateur: item.resoluePar ?? null, dateUtc: item.resolueLeUtc };
  if (item.ignoreeLeUtc) return { utilisateur: item.ignoreePar ?? null, dateUtc: item.ignoreeLeUtc };
  if (item.acquitteeLeUtc) return { utilisateur: item.acquitteePar ?? null, dateUtc: item.acquitteeLeUtc };
  return { utilisateur: null, dateUtc: null };
};

const normalizeAlertes = (payload: RapportResponse<PagedAlertesResponse>): RapportPreview =>
  table(
    payload.metadata,
    [
      { label: 'Sévérité' },
      { label: 'Code' },
      { label: 'Titre' },
      { label: 'Description' },
      { label: 'Source' },
      { label: 'OF' },
      { label: 'Article' },
      { label: 'Statut' },
      { label: 'Dernière détection' },
      { label: 'Traitée par' },
      { label: 'Traitée le' },
      { label: 'Commentaire' }
    ],
    payload.donnees.items.map((item) => {
      const traitement = traitementAlerte(item);
      return {
        id: item.id,
        cells: [
          item.severite,
          item.code,
          item.titre,
          item.description ?? null,
          item.source,
          item.numeroOF,
          item.codeArticle,
          item.statut,
          item.derniereDetectionLeUtc,
          traitement.utilisateur,
          traitement.dateUtc,
          item.commentaireTraitement ?? null
        ]
      };
    }),
    payload.donnees.total,
    [
      { label: 'Ouvertes', value: payload.donnees.summary.ouvertes },
      { label: 'Acquittées', value: payload.donnees.summary.acquittees },
      { label: 'Résolues', value: payload.donnees.summary.resolues },
      { label: 'Ignorées', value: payload.donnees.summary.ignorees }
    ]
  );

const normalizeInventaire = (payload: RapportResponse<InventaireSynthese>): RapportPreview =>
  table(
    payload.metadata,
    [
      { label: 'Type matière' },
      { label: 'Conteneurs', unite: 'kg' },
      { label: 'Catégories', unite: 'kg' },
      { label: 'Fours hors bilan', unite: 'kg' },
      { label: 'Total rapport', unite: 'kg' },
      { label: 'Total bilan matière', unite: 'kg' },
      { label: 'Statut' }
    ],
    payload.donnees.lignes.map((item) => ({
      id: item.codeTypeMatiere,
      cells: [
        item.codeTypeMatiere,
        item.partie1Kg,
        item.partie2Kg,
        item.partie3FourKg,
        item.totalRapportInventaireKg,
        item.totalBilanMatiereKg,
        item.statut
      ]
    })),
    payload.donnees.lignes.length,
    [
      { label: 'Total rapport', value: payload.donnees.totalGeneral.totalRapportInventaireKg, unite: 'kg' },
      { label: 'Total bilan matière', value: payload.donnees.totalGeneral.totalBilanMatiereKg, unite: 'kg' }
    ]
  );

const normalizeImports = (payload: RapportResponse<PagedRapportImports>): RapportPreview =>
  table(
    payload.metadata,
    [
      { label: 'Type' },
      { label: 'Profil' },
      { label: 'Fichier' },
      { label: 'Début UTC' },
      { label: 'Lues' },
      { label: 'Valides' },
      { label: 'Importées' },
      { label: 'Insérées' },
      { label: 'Mises à jour' },
      { label: 'Rejetées' },
      { label: 'Anomalies' },
      { label: 'Statut' }
    ],
    payload.donnees.items.map((item) => ({
      id: item.id,
      cells: [
        item.typeImport,
        item.profilImport,
        item.nomFichier,
        item.dateCreationUtc,
        item.lignesLues,
        item.lignesValides,
        item.lignesImportees,
        item.lignesInserees,
        item.lignesMisesAJour,
        item.lignesRejetees,
        item.nombreAnomalies,
        item.statut
      ]
    })),
    payload.donnees.total
  );

const normalizeNonReception = (payload: RapportResponse<NonReceptionResponse>): RapportPreview =>
  table(
    payload.metadata,
    [
      { label: 'OF' },
      { label: 'Article' },
      { label: 'Type matière' },
      { label: 'Total EMR', unite: 'pièces' },
      { label: 'Rebut EMR', unite: 'pièces' },
      { label: 'Net EMR', unite: 'pièces' },
      { label: 'Réceptionnées', unite: 'pièces' },
      { label: 'Non réceptionnées', unite: 'pièces' },
      { label: 'Statut' }
    ],
    payload.donnees.lignes.map((item) => ({
      id: `${item.date}|${item.numeroOF}`,
      cells: [
        item.numeroOF,
        item.codeArticle,
        item.codeTypeMatiere,
        item.totalEmrPieces,
        item.rebutEmrPieces,
        item.netEmrPieces,
        item.quantiteReceptionneeU2Pieces,
        item.piecesNonReceptionnees,
        item.statut
      ]
    })),
    payload.donnees.total,
    [
      { label: 'À vérifier', value: payload.donnees.nombreAVerifier },
      { label: 'Sur-réceptions', value: payload.donnees.nombreSurReceptions },
      { label: 'Données manquantes', value: payload.donnees.nombreDonneesManquantes },
      { label: 'Total non réceptionné', value: payload.donnees.totalPiecesNonReceptionnees, unite: 'pièces' }
    ]
  );

const fileNameFromDisposition = (disposition: string | undefined, fallback: string): string => {
  if (!disposition) return fallback;
  const encoded = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
  if (encoded) return decodeURIComponent(encoded);
  return disposition.match(/filename="?([^";]+)"?/i)?.[1] ?? fallback;
};

export const rapportsService = {
  async getCatalogue(): Promise<RapportCatalogueItem[]> {
    assertApiSource('rapports');
    const { data } = await api.get<RapportCatalogueItem[]>('/v1/rapports/catalogue');
    return data;
  },

  async preview(options: PreviewOptions): Promise<RapportPreview> {
    assertApiSource('rapports');
    const { code, filters, page, pageSize } = options;
    if (code === 'RPT-001') {
      const { data } = await api.get<RapportResponse<BilanFonderieApiResponse>>(
        `/v1/rapports/fonderie/${encodeURIComponent(filters.periode)}`
      );
      return normalizeFonderie(data);
    }
    if (code === 'RPT-002') {
      const { data } = await api.get<RapportResponse<BilanUsinage2ApiResponse>>(
        `/v1/rapports/usinage2/${encodeURIComponent(filters.periode)}`,
        { params: { page, pageSize } }
      );
      return normalizeUsinage2(data);
    }
    if (code === 'RPT-003') {
      const { data } = await api.get<RapportResponse<BilanUap2ApiResponse>>(
        `/v1/rapports/uap2/${encodeURIComponent(filters.periode)}`,
        { params: { page, pageSize } }
      );
      return normalizeUap2(data);
    }
    if (code === 'RPT-004') {
      const { data } = await api.get<RapportOfDetail>(
        `/v1/rapports/of/${encodeURIComponent(filters.numeroOF)}`,
        { params: compactParams(filters, ['periode']) }
      );
      return normalizeOf(data);
    }
    if (code === 'RPT-005') {
      const { data } = await api.get<RapportResponse<PagedAlertesResponse>>('/v1/rapports/alertes', {
        params: compactParams(filters, ['periode', 'statut', 'severite'], page, pageSize)
      });
      return normalizeAlertes(data);
    }
    if (code === 'RPT-006') {
      const { data } = await api.get<RapportResponse<InventaireSynthese>>(
        `/v1/rapports/inventaire/${encodeURIComponent(filters.periode)}`,
        { params: compactParams(filters, ['codeTypeMatiere']) }
      );
      return normalizeInventaire(data);
    }
    if (code === 'RPT-007') {
      const { data } = await api.get<RapportResponse<PagedRapportImports>>('/v1/rapports/imports', {
        params: compactParams(filters, ['dateDepuis', 'dateJusqua'], page, pageSize)
      });
      return normalizeImports(data);
    }
    const { data } = await api.get<RapportResponse<NonReceptionResponse>>(
      '/v1/rapports/receptions-non-receptionnees',
      { params: compactParams(filters, ['date'], page, pageSize) }
    );
    return normalizeNonReception(data);
  },

  async exportCsv(code: RapportCode, filters: RapportsFilters): Promise<RapportDownload> {
    assertApiSource('rapports');
    const response = await api.get<Blob>(`/v1/rapports/${encodeURIComponent(code)}/export`, {
      params: compactParams(filters, [
        'periode',
        'numeroOF',
        'date',
        'statut',
        'severite',
        'codeTypeMatiere',
        'dateDepuis',
        'dateJusqua'
      ]),
      responseType: 'blob'
    });
    return {
      blob: response.data,
      fileName: fileNameFromDisposition(response.headers['content-disposition'], `${code.toLowerCase()}.csv`)
    };
  }
};
