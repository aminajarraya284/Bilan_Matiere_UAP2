import type { UserRole, BilanStatus } from './common.types';
import type { BilanType, IsoDateString, IsoDateTimeString, SourceDonnee, StatutControle } from './domain.types';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface AuthUser {
  id: number;
  fullName: string;
  username: string;
  email: string;
  role: UserRole;
  status: 'Actif' | 'Inactif';
}

export interface LoginResponse {
  token: string;
  user: AuthUser;
  /** Expiration du token renvoyee par l'API (mode API reelle). L'expiration effective
   *  reste portee par le token lui-meme : un token expire provoque un 401. */
  expiresAtUtc?: IsoDateString;
}

export interface CreateReceptionU2Request {
  numeroOF: string;
  dateReception: IsoDateString;
  origine: 'FONDERIE' | 'AUTRE';
  poidsTotalReceptionneKg: number;
  nombrePiecesReceptionnees: number;
}

export interface ReceptionU2DefaultsResponse {
  numeroOF: string;
  codeArticle: string;
  designationArticle: string;
  atelierOrigine: string;
  codeTypeMatiere: string;
  quantiteLienUtilisee: number | null;
  uniteQuantiteLien: string | null;
  poidsTheoriqueUnitaireKg: number | null;
  gammeFabricationId: number;
  nomenclatureId: number;
  poidsTheoriqueTotalKg?: number | null;
  poidsPhysiqueUnitaireKg?: number | null;
  controlePoidsFonderieId?: string | null;
  sourcePoidsPhysique?: string | null;
  totalCtrlSci?: number | null;
  totalCtrlEmr?: number | null;
  totalCtrlTrn?: number | null;
  totalRebutSci?: number | null;
  totalRebutEmr?: number | null;
  totalRebutTrn?: number | null;
  piecesReceptionneesU2?: number | null;
  quantiteNonReceptionnee?: number | null;
  nombreLignesRapportMachine?: number;
  nombreLignesRapportMachineNonMappees?: number;
  statutArticle?: string;
  statutAtelier?: string;
  statutTypeMatiere?: string;
  statutNomenclature?: string;
  statutPoidsPhysique?: string;
  statutRapportMachine?: string;
  avertissements?: string[];
}

export interface ReceptionU2Response extends ReceptionU2DefaultsResponse {
  id: number;
  // Guid réel renvoyé par l'API : nécessaire pour les
  // actions valider/annuler, l'id numerique ci-dessus restant reserve a la generation mock.
  apiId?: string;
  dateReception: IsoDateString;
  origine: 'FONDERIE' | 'AUTRE';
  designationTypeMatiere: string;
  poidsTotalReceptionneKg: number | null;
  nombrePiecesReceptionnees: number;
  poidsPhysiqueUnitaireKg: number | null;
  poidsTheoriqueTotalKg: number | null;
  ecartPoidsUnitaireKg: number | null;
  tauxEcartPoids: number | null;
  statutAlertePoids: 'Conforme' | 'Alerte' | null;
  seuilAlertePoidsPourcentage: number | null;
  mois: string;
  statut: 'Brouillon' | 'Valide' | 'Cloture' | 'Annule';
  source: SourceDonnee;
  utilisateur: string;
  creeLe?: IsoDateTimeString;
}

export interface VerifyControleNonReceptionRequest {
  statutControle: StatutControle;
  commentaireControle?: string;
  justification?: string;
}

export interface CreateInventaireConteneurRequest {
  dateInventaire: IsoDateString;
  numeroConteneur: string;
  numeroOF?: string;
  codeTypeMatiere: string;
  poidsReelKg: number;
  observation?: string;
}

export interface CreateInventaireCategorieRequest {
  dateInventaire: IsoDateString;
  categorie: string;
  codeTypeMatiere: string;
  poidsReelKg: number;
  commentaire?: string;
}

export interface CreateInventaireFourRequest {
  dateInventaire: IsoDateString;
  codeFour: string;
  codeTypeMatiere: string;
  quantiteKg: number;
  observation?: string;
}

export interface CreateDechetFonderieRequest {
  dateDeclaration: IsoDateString;
  codeTypeMatiere: string;
  poidsDechetKg: number;
  typeDechet: 'Bavure' | 'Poudre';
  commentaire?: string;
}

export interface CreateSortieX3ManuelleRequest {
  numeroOF: string;
  codeArticle: string;
  codeTypeMatiere: string;
  quantitePrevue?: number;
  quantiteRealisee?: number;
  quantiteRestante?: number;
  quantiteBesoin?: number;
  quantiteConsommeeKg: number;
}

export interface CalculateBilanRequest {
  periode: string;
  codeTypeMatiere?: string;
  dateDebut?: IsoDateString;
  dateFin?: IsoDateString;
}

/**
 * Bilan Fonderie tel que consomme par les ecrans.
 *
 * Les composantes pouvant etre INCONNUES sont `number | null` : une donnee absente
 * n'est jamais ramenee a zero (RM-COM-003). Un `null` se propage aux totaux qui en
 * dependent, et la raison est listee dans `anomalies`.
 * Le jeu de demonstration renseigne toutes les valeurs : il reste compatible.
 */
export interface BilanFonderieResponse {
  bilanKey: string;
  periode: string;
  typeMatiere: string;
  encoursDebut: number | null;
  sortiesX3: number | null;
  inputTotal: number | null;
  encoursFinReel: number | null;
  dechetsFonderie: number;
  receptionsUsinage2: number | null;
  poidsNonReceptionne: number;
  nombrePiecesNonReceptionnees: number;
  outputTotal: number | null;
  ecartKg: number | null;
  tauxEcart: number | null;
  statut: BilanStatus | 'Complet' | 'Incomplet';
  justification?: string;
  dateCalcul: IsoDateString;
  /** Codes d'anomalie renvoyes par l'API (vide en mode demonstration). */
  anomalies?: string[];
}

/**
 * Bilan Fonderie dont TOUTES les composantes sont connues.
 *
 * Le jeu de demonstration est complet par construction : les consommateurs qui ne
 * lisent que lui (tableau de bord, export) gardent ainsi des nombres non-nullables,
 * sans garde inutile. Seule la source API peut produire des composantes inconnues.
 */
export type BilanFonderieComplet = Omit<
  BilanFonderieResponse,
  | 'encoursDebut'
  | 'sortiesX3'
  | 'inputTotal'
  | 'encoursFinReel'
  | 'receptionsUsinage2'
  | 'outputTotal'
  | 'ecartKg'
  | 'tauxEcart'
> & {
  encoursDebut: number;
  sortiesX3: number;
  inputTotal: number;
  encoursFinReel: number;
  receptionsUsinage2: number;
  outputTotal: number;
  ecartKg: number;
  tauxEcart: number;
};

/** Contrat brut de `GET /v1/bilans/fonderie/{periode}`, avant adaptation a l'ecran. */
export interface BilanFonderieApiLigne {
  typeMatiereId: string;
  codeTypeMatiere: string;
  periode: string;
  stockDebutKg: number | null;
  sortiesMagasinX3Kg: number | null;
  inputKg: number | null;
  stockFinKg: number | null;
  dechetsFonderieKg: number;
  receptionsUsinage2Kg: number | null;
  outputKg: number | null;
  ecartKg: number | null;
  tauxEcartPourcent: number | null;
  anomalies: string[];
  statut: 'Complet' | 'Incomplet';
}

export interface BilanFonderieApiResponse {
  periode: string;
  lignes: BilanFonderieApiLigne[];
  totalInputKg: number | null;
  totalOutputKg: number | null;
  totalEcartKg: number | null;
  nombreLignes: number;
  nombreLignesDonneesManquantes: number;
  nombreAnomalies: number;
}

export interface BilanUsinage2Response {
  bilanKey: string;
  periode: string;
  numeroOF: string;
  codeArticle: string;
  designationArticle?: string;
  codeTypeMatiere: string;
  quantiteEmrPieces: number;
  quantiteRebutEmrX3Pieces: number;
  quantiteEmrNetteX3Pieces: number;
  quantiteReceptionneeU2Pieces: number;
  quantiteNonReceptionneePieces: number;
  poidsNonReceptionneEstimeKg: number;
  rebutUsinage2Pieces?: number;
  rebutUsinage2Kg: number;
  entreeOfPieces?: number;
  statutControle: StatutControle;
  observation?: string;
  dateCalcul: IsoDateString;
}

/**
 * Ligne de Bilan Usinage 2. Chaque champ porte son unite dans son nom :
 *   `Pieces` -> quantite en pieces | `Kg` -> poids total | `UnitaireKg` -> kg par piece
 *   | `Pourcent` -> pourcentage.
 * Les pieces et les kilogrammes ne sont jamais melanges.
 */
export interface BilanUsinage2LigneApiResponse {
  numeroOF: string;
  codeArticle: string;
  designationArticle: string | null;
  atelierOrigine: string | null;
  typeMatiere: string | null;
  periode: string;

  // Bloc pieces
  quantiteCtrlEmrPieces: number | null;
  rebutEmrPieces: number | null;
  /** Remplace l'ancien « EMR nette ». */
  nombreDePieces: number | null;
  quantiteReceptionneePieces: number;
  quantiteNonReceptionneePieces: number | null;
  rebutUsinage2Pieces: number;
  entreeOfPieces: number | null;
  bilanUsinage2Pieces: number | null;

  // Taux (pourcentages, jamais integres au bilan en pieces)
  tauxReceptionPourcent: number | null;
  tauxNonReceptionPourcent: number | null;

  // Bloc controle des poids
  poidsReceptionneTotalKg: number | null;
  poidsPhysiqueUnitaireKg: number | null;
  poidsTheoriqueUnitaireKg: number | null;
  ecartPoidsUnitaireKg: number | null;
  tauxEcartPoidsPourcent: number | null;

  statutReception: 'OK' | 'AVerifier' | 'Anomalie' | 'DonneesManquantes';
  statutPoids: 'Trouve' | 'DonneesManquantes';
  anomalies: string[];
}

export interface BilanUsinage2ApiResponse {
  periode: string;
  lignes: BilanUsinage2LigneApiResponse[];
  /** Quantite totale receptionnee U2 sur la periode, en pieces. */
  quantiteTotaleReceptionneePieces: number;
  nombreLignes: number;
  nombreLignesDonneesManquantes: number;
  // Agregats calcules par le backend sur la PERIODE ENTIERE. Ils ne doivent pas etre
  // recalcules a partir de `lignes`, qui ne contient que la page courante.
  /** Somme des rebuts U2 VALIDES de la periode, en pieces. */
  totalRebutUsinage2Pieces: number;
  /** Somme des quantites Entree OF signees de la periode, en pieces. */
  totalEntreeOfPieces: number;
  /** Null des qu'une ligne de la periode n'est pas calculable : jamais un faux zero. */
  totalBilanUsinage2Pieces: number | null;
  nombreAnomalies: number;
  total: number;
  page: number;
  pageSize: number;
}

export interface BilanUap2Response {
  bilanKey: string;
  periode: string;
  numeroOF: string;
  codeArticle: string;
  designationArticle?: string;
  codeTypeMatiere: string;
  quantiteSortieMPKg: number;
  quantiteControlePieces: number;
  poidsPhysiqueUnitaireKg: number;
  poidsControleKg: number;
  rebutEmrKg: number;
  rebutSciKg: number;
  rebutTrnKg: number;
  totalRebutEmrSciTrnKg: number;
  quantiteReceptionneeU2Kg: number;
  quantiteNonReceptionneePieces: number;
  rebutUsinage2Pieces?: number;
  rebutUsinage2Kg: number;
  entreeSfNetKg: number;
  outputOFKg: number;
  ecartBilanOFKg: number;
  tauxEcartOF: number;
  statut: StatutControle | 'Cloture';
  observation?: string;
  dateCalcul: IsoDateString;
}

/**
 * Contrat brut de `GET /v1/bilans/uap2/{periode}` (RM-UAP2-001 et RM-UAP2-002).
 *
 * ATTENTION AUX UNITES : l'ecart de la partie 2 est EN PIECES, pas en kilogrammes.
 * Seule `sortieMpKg` est convertie, via le poids physique du Controle poids Fonderie.
 * Le type de demonstration `BilanUap2Response` ci-dessus est l'ancien modele, tout en
 * kg et avec un terme « entrees semi-finies » qui n'existe pas dans la regle reelle.
 */
export interface BilanUap2ApiLigne {
  numeroOF: string;
  codeArticle: string;
  designationArticle: string | null;
  codeTypeMatiere: string | null;
  periode: string;
  quantiteCtrlPieces: number;
  rebutSciPieces: number;
  rebutEmrPieces: number;
  rebutTrnPieces: number;
  quantiteSortieMpPieces: number;
  poidsPhysiqueUnitaireKg: number | null;
  sortieMpKg: number | null;
  entreeOfPieces: number | null;
  rebutUsinage2Pieces: number;
  ecartUap2Pieces: number | null;
  tauxEcartPourcent: number | null;
  anomalies: string[];
  statut: string;
}

export interface BilanUap2ApiResponse {
  periode: string;
  lignes: BilanUap2ApiLigne[];
  totalQuantiteCtrlPieces: number;
  totalEcartUap2Pieces: number | null;
  totalSortieMpKg: number | null;
  seuilEcartPourcent: number;
  nombreLignes: number;
  nombreLignesHorsSeuil: number;
  nombreLignesDonneesManquantes: number;
  nombreAnomalies: number;
  total: number;
  page: number;
  pageSize: number;
}

export interface InventaireTotalsResponse {
  codeTypeMatiere: string;
  totalPartie1: number;
  totalPartie2: number;
  totalPartie3: number;
  totalInventaireRapport: number;
  totalUtiliseBilan: number;
  statut: 'Brouillon' | 'AValider' | 'Valide' | 'Cloture';
}

export interface ImportBatchResponse {
  id: string;
  typeImport: string;
  profilImport: string;
  source: SourceDonnee;
  nomFichier: string;
  fileHash: string;
  dateDebut: IsoDateTimeString;
  dateFin?: IsoDateTimeString;
  nombreLignesLues: number;
  nombreLignesImportees: number;
  nombreLignesIgnorees: number;
  nombreErreurs: number;
  statut: 'Succes' | 'Erreur' | 'Partiel';
  importePar: string;
}

export interface VerifyBilanRequest {
  bilanType: BilanType;
  statutControle: string;
  observation?: string;
  justification?: string;
}

export interface ApiErrorResponse {
  status: number;
  code: string;
  message: string;
  errors?: Record<string, string[]>;
  correlationId?: string;
}

export interface PagedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
