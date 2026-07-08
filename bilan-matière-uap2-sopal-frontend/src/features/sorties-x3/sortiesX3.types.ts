export interface SortieX3Mouvement {
  id: number;
  articleParent?: string;
  centreCharge?: string;
  numeroOf?: string;
  operation?: string | number;
  composant?: string;
  flagSuivi?: string;
  siteProduction?: string;
  qtePrevue?: number;
  qteRealisee?: number;
  qteRestante?: number;
  qteBesoin?: number;
  qteConsommee?: number;
  dateCloture?: string;
  dateImputation: string;
  codeMatiere: string;
  designation: string;
  dateCreation: string;
  heureCreation: string;
  utilisateur: string;
  quantiteImportee: number; // e.g. -2203
  quantiteUtilisee: number; // absolute value: 2203
  unite: 'KG';
  transaction: string;
  typePiece: string;
  lot: string;
  pieceOrigine: string;
  designationOrigine: string;
  emplacement: string;
  statut: 'Surchargé' | 'Synchronisé' | 'À Vérifier';
  source: 'API Sage X3' | 'Fichier Excel' | 'Manuel';
}
