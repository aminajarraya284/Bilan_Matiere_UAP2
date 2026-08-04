import api from '@/services/api';
import { assertApiSource } from '@/config/dataSources';
import type {
  CreateInventaireCategorieInput,
  CreateInventaireConteneurInput,
  CreateInventaireFourInput,
  InventaireCategorie,
  InventaireComptagePeriode,
  InventaireConteneur,
  InventaireFour,
  InventaireStatut,
  InventaireTotalParMatiere,
  InventaireTypeMatiere
} from './inventaire.types';

const basePath = '/v1/inventaires-fonderie/comptage';

interface ApiConteneur {
  id: string;
  periode: string;
  dateInventaire: string;
  numeroConteneur: string;
  numeroOF: string | null;
  codeArticle: string | null;
  designationArticle: string | null;
  typeMatiereId: string;
  codeTypeMatiere: string;
  quantiteKg: number;
  observation: string | null;
  statut: string;
}

interface ApiCategorie {
  id: string;
  periode: string;
  categorie: string;
  typeMatiereId: string;
  codeTypeMatiere: string;
  quantiteKg: number;
  observation: string | null;
  statut: string;
  inclusBilanFonderie: boolean;
  visibleRapportInventaire: boolean;
}

interface ApiFour {
  id: string;
  periode: string;
  dateInventaire: string;
  codeFour: string;
  typeMatiereId: string;
  codeTypeMatiere: string;
  quantiteKg: number;
  observation: string | null;
  statut: string;
  inclusBilanFonderie: boolean;
  visibleRapportInventaire: boolean;
}

interface ApiLegacyTotal {
  typeMatiereId: string;
  codeTypeMatiere: string;
  totalPartie1: number;
  totalPartie2: number;
  totalPartie3: number;
  totalInventaireRapport: number;
  totalUtiliseBilan: number;
}

interface ApiProjectionTotal {
  typeMatiereId: string;
  codeTypeMatiere: string;
  stockDebutKg: number | null;
  stockFinKg: number;
  totalFoursKg: number;
  totalUtiliseBilanKg: number;
  totalInventaireRapportKg: number;
  anomalies: string[];
}

interface ApiComptagePeriode {
  periode: string;
  conteneurs: ApiConteneur[];
  categories: ApiCategorie[];
  fours: ApiFour[];
  totaux: ApiLegacyTotal[];
  totauxParTypeMatiere: ApiProjectionTotal[] | null;
}

const toStatut = (value: string): InventaireStatut =>
  value === 'Valide' || value === 'Annule' ? value : 'Brouillon';

const toConteneur = (item: ApiConteneur): InventaireConteneur => ({
  id: item.id,
  periode: item.periode,
  dateInventaire: item.dateInventaire,
  numeroConteneur: item.numeroConteneur,
  numeroOF: item.numeroOF ?? undefined,
  codeArticle: item.codeArticle ?? undefined,
  designationArticle: item.designationArticle ?? undefined,
  typeMatiereId: item.typeMatiereId,
  codeTypeMatiere: item.codeTypeMatiere,
  quantiteKg: item.quantiteKg,
  observation: item.observation ?? undefined,
  statut: toStatut(item.statut)
});

const toCategorie = (item: ApiCategorie): InventaireCategorie => ({
  id: item.id,
  periode: item.periode,
  categorie: item.categorie,
  typeMatiereId: item.typeMatiereId,
  codeTypeMatiere: item.codeTypeMatiere,
  quantiteKg: item.quantiteKg,
  observation: item.observation ?? undefined,
  statut: toStatut(item.statut),
  inclusDansBilanFonderie: true,
  visibleDansRapportInventaire: true
});

const toFour = (item: ApiFour): InventaireFour => ({
  id: item.id,
  periode: item.periode,
  dateInventaire: item.dateInventaire,
  four: item.codeFour,
  typeMatiereId: item.typeMatiereId,
  codeTypeMatiere: item.codeTypeMatiere,
  quantiteKg: item.quantiteKg,
  observation: item.observation ?? undefined,
  statut: toStatut(item.statut),
  inclusDansBilanFonderie: false,
  visibleDansRapportInventaire: true
});

const toPeriode = (value: string): string => {
  if (!/^\d{4}-(0[1-9]|1[0-2])$/.test(value)) {
    throw new Error('La période doit être au format AAAA-MM.');
  }

  return value;
};

const toTotals = (response: ApiComptagePeriode): InventaireTotalParMatiere[] => {
  const legacyByType = new Map(response.totaux.map((item) => [item.typeMatiereId, item]));
  const projections = response.totauxParTypeMatiere ?? [];

  return projections.map((projection) => {
    const legacy = legacyByType.get(projection.typeMatiereId);
    return {
      typeMatiereId: projection.typeMatiereId,
      codeTypeMatiere: projection.codeTypeMatiere,
      stockDebutKg: projection.stockDebutKg,
      stockFinKg: projection.stockFinKg,
      totalPartie1: legacy?.totalPartie1 ?? 0,
      totalPartie2: legacy?.totalPartie2 ?? 0,
      totalPartie3: projection.totalFoursKg,
      totalInventaireRapport: projection.totalInventaireRapportKg,
      totalUtiliseBilan: projection.totalUtiliseBilanKg,
      anomalies: projection.anomalies ?? []
    };
  });
};

const toTypesMatiere = (response: ApiComptagePeriode): InventaireTypeMatiere[] =>
  (response.totauxParTypeMatiere ?? response.totaux).map((item) => ({
    id: item.typeMatiereId,
    code: item.codeTypeMatiere
  }));

export const inventaireService = {
  async getPeriode(periode: string): Promise<InventaireComptagePeriode> {
    assertApiSource('inventaire');
    const normalizedPeriode = toPeriode(periode);
    const response = (await api.get<ApiComptagePeriode>(`${basePath}/${normalizedPeriode}`)).data;

    return {
      periode: response.periode,
      conteneurs: response.conteneurs.map(toConteneur),
      categories: response.categories.map(toCategorie),
      fours: response.fours.map(toFour),
      totaux: toTotals(response),
      typesMatiere: toTypesMatiere(response)
    };
  },

  async createConteneur(input: CreateInventaireConteneurInput): Promise<InventaireConteneur> {
    assertApiSource('inventaire');
    const response = await api.post<ApiConteneur>(`${basePath}/conteneurs`, {
      dateInventaire: input.dateInventaire,
      numeroConteneur: input.numeroConteneur,
      numeroOF: input.numeroOF,
      codeArticle: input.codeArticle,
      designationArticle: input.designationArticle,
      typeMatiereId: input.typeMatiereId,
      quantiteKg: input.quantiteKg,
      observation: input.observation
    });
    return toConteneur(response.data);
  },

  async createCategorie(input: CreateInventaireCategorieInput): Promise<InventaireCategorie> {
    assertApiSource('inventaire');
    const response = await api.post<ApiCategorie>(`${basePath}/categories`, {
      periode: `${toPeriode(input.periode)}-01`,
      categorie: input.categorie,
      typeMatiereId: input.typeMatiereId,
      quantiteKg: input.quantiteKg,
      observation: input.observation
    });
    return toCategorie(response.data);
  },

  async createFour(input: CreateInventaireFourInput): Promise<InventaireFour> {
    assertApiSource('inventaire');
    const response = await api.post<ApiFour>(`${basePath}/fours`, {
      dateInventaire: input.dateInventaire,
      codeFour: input.four,
      typeMatiereId: input.typeMatiereId,
      quantiteKg: input.quantiteKg,
      observation: input.observation
    });
    return toFour(response.data);
  },

  async validerConteneur(id: string): Promise<InventaireConteneur> {
    assertApiSource('inventaire');
    return toConteneur((await api.post<ApiConteneur>(`${basePath}/conteneurs/${id}/valider`)).data);
  },

  async annulerConteneur(id: string): Promise<InventaireConteneur> {
    assertApiSource('inventaire');
    return toConteneur((await api.post<ApiConteneur>(`${basePath}/conteneurs/${id}/annuler`)).data);
  },

  async validerCategorie(id: string): Promise<InventaireCategorie> {
    assertApiSource('inventaire');
    return toCategorie((await api.post<ApiCategorie>(`${basePath}/categories/${id}/valider`)).data);
  },

  async annulerCategorie(id: string): Promise<InventaireCategorie> {
    assertApiSource('inventaire');
    return toCategorie((await api.post<ApiCategorie>(`${basePath}/categories/${id}/annuler`)).data);
  },

  async validerFour(id: string): Promise<InventaireFour> {
    assertApiSource('inventaire');
    return toFour((await api.post<ApiFour>(`${basePath}/fours/${id}/valider`)).data);
  },

  async annulerFour(id: string): Promise<InventaireFour> {
    assertApiSource('inventaire');
    return toFour((await api.post<ApiFour>(`${basePath}/fours/${id}/annuler`)).data);
  }
};
