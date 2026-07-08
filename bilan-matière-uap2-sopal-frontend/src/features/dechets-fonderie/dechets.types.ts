export type DechetType = 'Bavure' | 'Poudre';

export interface DechetMouvement {
  id: number;
  dateDeclaration: string;
  semaine: number;
  codeArticle?: string;
  designationArticle?: string;
  codeTypeMatiere: string;
  quantiteKg: number;
  unite: 'KG';
  typeDechet: DechetType;
  commentaire?: string;
  statut: 'Brouillon' | 'AValider' | 'Valide' | 'Cloture';
  utilisateur: string;
}
