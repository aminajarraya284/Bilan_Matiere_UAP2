export interface TypeMatiereResponse {
  id: string;
  code: string;
  libelle: string;
  estActif: boolean;
  dateCreationUtc: string;
  dateModificationUtc: string | null;
}
