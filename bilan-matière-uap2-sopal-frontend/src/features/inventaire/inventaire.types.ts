export type InventaireStatut = 'Brouillon' | 'AValider' | 'Valide' | 'Cloture';
export type CategorieInventaire = 'ConteneurOF' | 'CategorieBilan' | 'FourHorsBilan';

export interface InventaireConteneur {
  id: number;
  dateInventaire: string;
  mois: number;
  annee: number;
  numeroConteneur: string;
  ofDestination?: string;
  codeArticle: string;
  designationArticle?: string;
  typeMatiereId: number;
  codeTypeMatiere: string;
  quantiteReelle: number;
  unite: 'KG';
  observation?: string;
  statut: InventaireStatut;
}

export interface InventaireCategorie {
  id: number;
  mois: number;
  annee: number;
  categorie: string;
  categorieInventaire?: CategorieInventaire;
  typeMatiereId: number;
  codeTypeMatiere: string;
  quantiteReelle: number;
  unite: 'KG';
  commentaire?: string;
  statut: InventaireStatut;
  inclusDansBilanFonderie?: boolean;
  visibleDansRapportInventaire?: boolean;
}

export interface InventaireFour {
  id: number;
  dateInventaire: string;
  mois: number;
  annee: number;
  four: 'Four CBP 01' | 'Four CBP 02' | 'Four CDQ' | 'Autre';
  codeTypeMatiere: string;
  quantite: number;
  unite: 'KG' | 'UN';
  observation?: string;
  statut: 'Brouillon' | 'Valide' | 'Annule';
  inclusDansBilanFonderie: false;
  visibleDansRapportInventaire: true;
}

export interface InventaireTotalParMatiere {
  codeTypeMatiere: string;
  totalPartie1: number;
  totalPartie2: number;
  totalPartie3: number;
  totalInventaireRapport: number;
  totalUtiliseBilan: number;
  statut: InventaireStatut;
}
