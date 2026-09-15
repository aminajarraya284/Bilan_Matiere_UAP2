export type InventaireStatut = 'Brouillon' | 'Valide' | 'Annule';

export interface InventaireTypeMatiere {
  id: string;
  code: string;
}

export interface InventaireConteneur {
  id: string;
  periode: string;
  dateInventaire: string;
  numeroConteneur: string;
  numeroOF?: string;
  codeArticle?: string;
  designationArticle?: string;
  typeMatiereId: string;
  codeTypeMatiere: string;
  quantiteKg: number;
  observation?: string;
  statut: InventaireStatut;
}

export interface InventaireCategorie {
  id: string;
  periode: string;
  categorie: string;
  typeMatiereId: string;
  codeTypeMatiere: string;
  quantiteKg: number;
  observation?: string;
  statut: InventaireStatut;
  inclusDansBilanFonderie: true;
  visibleDansRapportInventaire: true;
}

export interface InventaireFour {
  id: string;
  periode: string;
  dateInventaire: string;
  four: string;
  typeMatiereId: string;
  codeTypeMatiere: string;
  quantiteKg: number;
  observation?: string;
  statut: InventaireStatut;
  inclusDansBilanFonderie: false;
  visibleDansRapportInventaire: true;
}

export interface InventaireTotalParMatiere {
  typeMatiereId: string;
  codeTypeMatiere: string;
  stockDebutKg: number | null;
  stockFinKg: number;
  totalPartie1: number;
  totalPartie2: number;
  totalPartie3: number;
  totalInventaireRapport: number;
  totalUtiliseBilan: number;
  anomalies: string[];
}

export interface InventaireComptagePeriode {
  periode: string;
  conteneurs: InventaireConteneur[];
  categories: InventaireCategorie[];
  fours: InventaireFour[];
  totaux: InventaireTotalParMatiere[];
  typesMatiere: InventaireTypeMatiere[];
}

export interface CreateInventaireConteneurInput {
  dateInventaire: string;
  numeroConteneur: string;
  numeroOF?: string;
  codeArticle?: string;
  designationArticle?: string;
  typeMatiereId: string;
  quantiteKg: number;
  observation?: string;
}

export interface CreateInventaireCategorieInput {
  periode: string;
  categorie: string;
  typeMatiereId: string;
  quantiteKg: number;
  observation?: string;
}

export interface CreateInventaireFourInput {
  dateInventaire: string;
  four: string;
  typeMatiereId: string;
  quantiteKg: number;
  observation?: string;
}
