export const BUSINESS_RULE_PARAMETER_KEYS = {
  seuilEcartFonderie: 'SEUIL_ECART_FONDERIE',
  seuilEcartUap2: 'SEUIL_ECART_UAP2',
  codeAtelierUap2: 'CODE_ATELIER_UAP2',
  transactionEntreeOf: 'TRANSACTION_ENTREE_OF',
  typesMatiere: 'TYPES_MATIERE'
} as const;

const formatLocalIsoDate = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const currentMonth = (date = new Date()) => {
  const start = new Date(date.getFullYear(), date.getMonth(), 1);
  const end = new Date(date.getFullYear(), date.getMonth() + 1, 0);
  return {
    periode: formatLocalIsoDate(start).slice(0, 7),
    dateDebut: formatLocalIsoDate(start),
    dateFin: formatLocalIsoDate(end)
  };
};

const isoWeek = (date = new Date()): number => {
  const utcDate = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  const day = utcDate.getUTCDay() || 7;
  utcDate.setUTCDate(utcDate.getUTCDate() + 4 - day);
  const yearStart = new Date(Date.UTC(utcDate.getUTCFullYear(), 0, 1));
  return Math.ceil(((utcDate.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
};

const readOptionalNumber = (value: string | undefined): number => {
  if (!value) return Number.NaN;
  const parsed = Number(value.replace(',', '.'));
  return Number.isFinite(parsed) ? parsed : Number.NaN;
};

const readOptionalCsv = (value: string | undefined): readonly string[] =>
  value
    ?.split(',')
    .map((item) => item.trim().toUpperCase())
    .filter(Boolean) ?? [];

const defaults = currentMonth();
const typeMatieresParDefaut = readOptionalCsv(import.meta.env.VITE_TYPES_MATIERE);

// Le backend expose les valeurs metier via GET /api/v1/parametres.
// Ce fichier ne porte plus de dates ni de seuils fixes : les valeurs numeriques
// optionnelles ne servent qu'a maintenir les ecrans historiques tant que leur lecture
// asynchrone des parametres n'est pas branchee.
export const BUSINESS_RULES = {
  periodeParDefaut: defaults.periode,
  dateDebutParDefaut: defaults.dateDebut,
  dateFinParDefaut: defaults.dateFin,
  semaineDeclarationParDefaut: isoWeek(),
  seuilEcartPourcentage: readOptionalNumber(import.meta.env.VITE_SEUIL_ECART_FONDERIE),
  seuilAlertePoidsPourcentage: readOptionalNumber(import.meta.env.VITE_SEUIL_ECART_FONDERIE),
  seuilAlerteBilanPourcentage: readOptionalNumber(import.meta.env.VITE_SEUIL_ECART_FONDERIE),
  seuilEcartBilanAutoPourcentage: readOptionalNumber(import.meta.env.VITE_SEUIL_ECART_UAP2),
  seuilCritiqueEcartPourcentage: readOptionalNumber(import.meta.env.VITE_SEUIL_ECART_FONDERIE),
  seuilAlertePoidsKg: readOptionalNumber(import.meta.env.VITE_SEUIL_ALERTE_POIDS_KG),
  seuilEcartKgHighlight: readOptionalNumber(import.meta.env.VITE_SEUIL_ECART_KG_HIGHLIGHT),
  seuilDecalageImportantKg: readOptionalNumber(import.meta.env.VITE_SEUIL_DECALAGE_IMPORTANT_KG),
  typeMatieresParDefaut,
  encoursDebutParTypeMatiere: {} as Record<string, number>,
  statutBilanAvecEcart: 'AvecEcart',
  statutsVerificationControleNonReception: ['AVerifier', 'Justifie', 'Regularise', 'Anomalie'] as const
} as const;
