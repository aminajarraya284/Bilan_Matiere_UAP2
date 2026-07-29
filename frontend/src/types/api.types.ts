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
  quantiteLienUtilisee: number;
  uniteQuantiteLien: 'KG';
  poidsTheoriqueUnitaireKg: number;
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
  poidsTotalReceptionneKg: number;
  nombrePiecesReceptionnees: number;
  poidsPhysiqueUnitaireKg: number;
  poidsTheoriqueTotalKg: number;
  ecartPoidsUnitaireKg: number;
  tauxEcartPoids: number;
  statutAlertePoids: 'Conforme' | 'Alerte';
  seuilAlertePoidsPourcentage: number;
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

export interface BilanFonderieResponse {
  bilanKey: string;
  periode: string;
  typeMatiere: string;
  encoursDebut: number;
  sortiesX3: number;
  inputTotal: number;
  encoursFinReel: number;
  dechetsFonderie: number;
  receptionsUsinage2: number;
  poidsNonReceptionne: number;
  nombrePiecesNonReceptionnees: number;
  outputTotal: number;
  ecartKg: number;
  tauxEcart: number;
  statut: BilanStatus;
  justification?: string;
  dateCalcul: IsoDateString;
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
