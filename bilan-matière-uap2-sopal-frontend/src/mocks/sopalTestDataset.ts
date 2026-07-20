import type { SourceDonnee } from '@/types/domain.types';

export const TEST_DATA_VERSION = 'SOPAL_TEST_V4_X3_MAPPINGS';
export const TEST_DATA_STORAGE_KEY = 'sopal_test_data_v4_x3_mappings';

export type TypeMatiereCode = 'M3000L0' | 'M3010L0' | 'M3060L0';
export type StatutControle = 'OK' | 'AVerifier' | 'Anomalie' | 'Justifie' | 'Regularise';
export type StatutReception = 'Brouillon' | 'Valide' | 'Cloture';

export interface RefTypeMatiere {
  id: number;
  codeTypeMatiere: TypeMatiereCode;
  libelle: string;
  unite: 'KG';
  actif: boolean;
}

export interface X3OrdreFabrication {
  id: number;
  numeroOF: string;
  codeArticleParent: string;
  designationArticle: string;
  siteProduction: string;
  dateCloture: string;
  statutSuivi: 'Ouvert' | 'Cloture';
  sourceSystem: SourceDonnee;
  importBatchId: string;
  importedAt: string;
  isActive: boolean;
}

export interface X3OperationOF {
  id: number;
  numeroOF: string;
  numeroOperation: string;
  centreCharge: string;
  quantitePrevue: number;
  quantiteRealisee: number;
  quantiteRestante: number;
  sourceSystem: SourceDonnee;
  importBatchId: string;
  isActive: boolean;
}

export interface X3ComposantOF {
  id: number;
  numeroOF: string;
  numeroOperation: string;
  codeComposant: TypeMatiereCode;
  flagSuivi: 'Oui' | 'Non';
  quantiteBesoin: number;
  quantiteConsommee: number;
  sourceSystem: SourceDonnee;
  importBatchId: string;
  isActive: boolean;
}

export interface RefGammeFabrication {
  id: number;
  codeArticle: string;
  designationArticle: string;
  dateFinValidite: string;
  etapeSuivi: string;
  numeroOperation: string;
  codeAtelier: string;
  codePoste: string;
  nombrePostes: number;
  groupePosteCharge: string;
  designationOperation: string;
  alternativeGamme: string;
  uniteOperation: string;
  coefficientOperationStock: number;
  tempsCycle: number;
  trs: number;
  tempsAttente: number;
  jalon: string;
  quantite: number;
  cadence: number;
  dateModificationX3: string;
  tempsReglage: number;
  coefficientCCPT: number;
  sourceSystem: SourceDonnee;
  importBatchId: string;
  isActive: boolean;
}

export interface RefNomenclature {
  id: number;
  codeParentNiveau: string;
  codeComposant: TypeMatiereCode;
  codeArticleParent: string;
  designationArticleParent: string;
  niveau: number;
  designationComposant: string;
  sequence: string;
  quantiteLien: number;
  quantiteLienCumulee: number;
  uniteQuantite: 'KG';
  categorieComposant: 'MATIERE';
  typeLien: 'NORMAL';
  pourcentageRebut: number;
  sourceSystem: SourceDonnee;
  importBatchId: string;
  isActive: boolean;
}

export interface X3RapportMachine {
  id: number;
  dateProduction: string;
  codePoste: string;
  numeroOperation: string;
  codeArticle: string;
  designationArticle: string;
  numeroOF: string;
  equipe: string;
  quantiteEMR: number;
  quantiteRebutEMR: number;
  alternativeGamme: string;
  sourceSystem: SourceDonnee;
  importBatchId: string;
  isActive: boolean;
}

export interface X3MouvementStock {
  id: number;
  dateImputation: string;
  codeArticle: TypeMatiereCode;
  designationArticle: string;
  dateCreationX3: string;
  heureCreationX3: string;
  utilisateurX3: string;
  quantite: number;
  unite: 'KG';
  typeTransaction: string;
  typePiece: string;
  numeroLot: string;
  numeroOF: string;
  referenceOrigineX3: string;
  emplacement: string;
  statut: string;
  sourceSystem: SourceDonnee;
  importBatchId: string;
  isActive: boolean;
}

export interface InventaireFonderieConteneur {
  id: number;
  dateInventaire: string;
  numeroConteneur: string;
  numeroOF: string;
  codeTypeMatiere: TypeMatiereCode;
  poidsReelKg: number;
  observation: string;
  statut: 'Brouillon' | 'AValider' | 'Valide' | 'Cloture';
  creePar: string;
  creeLe: string;
}

export interface InventaireFonderieCategorie {
  id: number;
  dateInventaire: string;
  categorie: string;
  codeTypeMatiere: TypeMatiereCode;
  poidsReelKg: number;
  commentaire: string;
  inclusBilanFonderie: boolean;
  statut: 'Brouillon' | 'AValider' | 'Valide' | 'Cloture';
  creePar: string;
  creeLe: string;
}

export interface InventaireFonderieFour {
  id: number;
  dateInventaire: string;
  mois: number;
  annee: number;
  codeFour: 'Four CBP 01' | 'Four CBP 02' | 'Four CDQ' | 'Autre';
  codeTypeMatiere: TypeMatiereCode;
  quantite: number;
  unite: 'KG';
  observation: string;
  statut: 'Brouillon' | 'Valide' | 'Annule';
  inclusBilanFonderie: false;
  visibleRapportInventaire: true;
}

export interface StockDebutMatiere {
  periode: string;
  codeTypeMatiere: TypeMatiereCode;
  stockDebutKg: number;
  source: string;
}

export interface DechetFonderieTest {
  id: number;
  dateDeclaration: string;
  semaine: number;
  codeTypeMatiere: TypeMatiereCode;
  typeDechet: 'Bavure' | 'Poudre';
  poidsDechetKg: number;
  observation: string;
  declarantId: string;
  declarantNom: string;
  statut: 'Brouillon' | 'AValider' | 'Valide' | 'Cloture';
  creeLe: string;
}

export interface ReceptionUsinage2Test {
  id: number;
  numeroOF: string;
  dateReception: string;
  codeArticle: string;
  designationArticle: string;
  origine: 'FONDERIE' | 'AUTRE';
  atelierOrigine: string;
  codeTypeMatiere: TypeMatiereCode;
  poidsTotalReceptionneKg: number;
  nombrePiecesReceptionnees: number;
  poidsPhysiqueUnitaireKg: number;
  quantiteLienUtilisee: number;
  uniteQuantiteLien: 'KG';
  poidsTheoriqueUnitaireKg: number;
  poidsTheoriqueTotalKg: number;
  ecartPoidsUnitaireKg: number;
  tauxEcartPoids: number;
  statutAlertePoids: 'Conforme' | 'Alerte';
  seuilAlertePoidsPourcentage: number;
  gammeFabricationId: number;
  nomenclatureId: number;
  mois: string;
  statut: StatutReception;
  source: SourceDonnee;
  creePar: string;
  creeLe: string;
}

export interface RebutUsinage2 {
  id: number;
  numeroOF: string;
  codeArticle: string;
  codeTypeMatiere: TypeMatiereCode;
  dateDeclaration: string;
  nombrePiecesRebut: number;
  poidsRebutKg: number;
  observation: string;
  statut: 'Brouillon' | 'Valide';
  creePar: string;
}

export interface RebutUap2Saisie {
  id: number;
  numeroOF: string;
  dateDeclaration: string;
  rebutEmrKg: number;
  rebutSciKg: number;
  rebutTrnKg: number;
  statut: 'Valide';
  creePar: string;
}

export interface ImportBatchTest {
  id: string;
  typeImport: string;
  profilImport: string;
  sourceSystem: SourceDonnee;
  nomFichier: string;
  fileHash: string;
  dateDebut: string;
  dateFin: string;
  nombreLignesLues: number;
  nombreLignesImportees: number;
  nombreLignesIgnorees: number;
  nombreErreurs: number;
  statut: 'Succes' | 'Erreur' | 'Partiel';
  importePar: string;
}

export interface SopalTestDataset {
  version: typeof TEST_DATA_VERSION;
  periode: '2026-06';
  dateDebut: '2026-06-01';
  dateFin: '2026-06-30';
  parametresApplication: {
    seuilAlertePoidsPourcentage: number;
    seuilAlerteBilanPourcentage: number;
    seuilQuantiteNonReceptionneeAVerifier: number;
    seuilQuantiteNonReceptionneeAnomalie: number;
    poidsNonReceptionneInclusInventaireFin: boolean;
  };
  refTypesMatiere: RefTypeMatiere[];
  x3OrdresFabrication: X3OrdreFabrication[];
  x3OperationsOF: X3OperationOF[];
  x3ComposantsOF: X3ComposantOF[];
  refGammeFabrication: RefGammeFabrication[];
  refNomenclature: RefNomenclature[];
  x3RapportsMachine: X3RapportMachine[];
  x3MouvementsStock: X3MouvementStock[];
  inventaireConteneurs: InventaireFonderieConteneur[];
  inventaireCategories: InventaireFonderieCategorie[];
  inventaireFours: InventaireFonderieFour[];
  stockDebut: StockDebutMatiere[];
  dechetsFonderie: DechetFonderieTest[];
  receptionsUsinage2: ReceptionUsinage2Test[];
  rebutsUsinage2: RebutUsinage2[];
  rebutsUap2?: RebutUap2Saisie[];
  importBatches: ImportBatchTest[];
}

const IMPORTED_AT = '2026-07-01T08:00:00';
const SOURCE: SourceDonnee = 'Excel';
const SEUIL_POIDS = 3;

export const sopalTestDataset: SopalTestDataset = {
  version: TEST_DATA_VERSION,
  periode: '2026-06',
  dateDebut: '2026-06-01',
  dateFin: '2026-06-30',
  parametresApplication: {
    seuilAlertePoidsPourcentage: SEUIL_POIDS,
    seuilAlerteBilanPourcentage: 3,
    seuilQuantiteNonReceptionneeAVerifier: 10,
    seuilQuantiteNonReceptionneeAnomalie: 10,
    poidsNonReceptionneInclusInventaireFin: true
  },
  refTypesMatiere: [
    { id: 1, codeTypeMatiere: 'M3000L0', libelle: 'Matiere M3000L0', unite: 'KG', actif: true },
    { id: 2, codeTypeMatiere: 'M3010L0', libelle: 'Matiere M3010L0', unite: 'KG', actif: true },
    { id: 3, codeTypeMatiere: 'M3060L0', libelle: 'Matiere M3060L0', unite: 'KG', actif: true }
  ],
  x3OrdresFabrication: [
    {
      id: 1,
      numeroOF: 'OF18100563',
      codeArticleParent: '0101A03-1',
      designationArticle: 'Article test 0101A03-1',
      siteProduction: 'UAP2',
      dateCloture: '2026-06-30',
      statutSuivi: 'Ouvert',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      importedAt: IMPORTED_AT,
      isActive: true
    },
    {
      id: 2,
      numeroOF: 'OF18100564',
      codeArticleParent: '0102A03-1',
      designationArticle: 'Article test 0102A03-1',
      siteProduction: 'UAP2',
      dateCloture: '2026-06-30',
      statutSuivi: 'Ouvert',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      importedAt: IMPORTED_AT,
      isActive: true
    },
    {
      id: 3,
      numeroOF: 'OF18100565',
      codeArticleParent: '0203203-1',
      designationArticle: 'Article test 0203203-1',
      siteProduction: 'UAP2',
      dateCloture: '2026-06-30',
      statutSuivi: 'Ouvert',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      importedAt: IMPORTED_AT,
      isActive: true
    },
    {
      id: 4,
      numeroOF: 'OF18100566',
      codeArticleParent: '0101A03-1',
      designationArticle: 'Article test 0101A03-1',
      siteProduction: 'UAP2',
      dateCloture: '2026-06-30',
      statutSuivi: 'Ouvert',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      importedAt: IMPORTED_AT,
      isActive: true
    }
  ],
  x3OperationsOF: [
    {
      id: 1,
      numeroOF: 'OF18100563',
      numeroOperation: '10',
      centreCharge: 'UAP2',
      quantitePrevue: 1000,
      quantiteRealisee: 950,
      quantiteRestante: 50,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      isActive: true
    },
    {
      id: 2,
      numeroOF: 'OF18100563',
      numeroOperation: '20',
      centreCharge: 'UAP2',
      quantitePrevue: 1000,
      quantiteRealisee: 950,
      quantiteRestante: 50,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      isActive: true
    },
    {
      id: 3,
      numeroOF: 'OF18100564',
      numeroOperation: '10',
      centreCharge: 'UAP2',
      quantitePrevue: 800,
      quantiteRealisee: 800,
      quantiteRestante: 0,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      isActive: true
    },
    {
      id: 4,
      numeroOF: 'OF18100565',
      numeroOperation: '10',
      centreCharge: 'UAP2',
      quantitePrevue: 500,
      quantiteRealisee: 450,
      quantiteRestante: 50,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      isActive: true
    },
    {
      id: 5,
      numeroOF: 'OF18100566',
      numeroOperation: '10',
      centreCharge: 'UAP2',
      quantitePrevue: 300,
      quantiteRealisee: 250,
      quantiteRestante: 50,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      isActive: true
    }
  ],
  x3ComposantsOF: [
    {
      id: 1,
      numeroOF: 'OF18100563',
      numeroOperation: '10',
      codeComposant: 'M3010L0',
      flagSuivi: 'Oui',
      quantiteBesoin: 1500,
      quantiteConsommee: 1425,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      isActive: true
    },
    {
      id: 2,
      numeroOF: 'OF18100564',
      numeroOperation: '10',
      codeComposant: 'M3000L0',
      flagSuivi: 'Oui',
      quantiteBesoin: 1600,
      quantiteConsommee: 1600,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      isActive: true
    },
    {
      id: 3,
      numeroOF: 'OF18100565',
      numeroOperation: '10',
      codeComposant: 'M3060L0',
      flagSuivi: 'Oui',
      quantiteBesoin: 600,
      quantiteConsommee: 540,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      isActive: true
    },
    {
      id: 4,
      numeroOF: 'OF18100566',
      numeroOperation: '10',
      codeComposant: 'M3010L0',
      flagSuivi: 'Oui',
      quantiteBesoin: 450,
      quantiteConsommee: 375,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YLISTOFFERM-202606',
      isActive: true
    }
  ],
  refGammeFabrication: [
    {
      id: 1,
      codeArticle: '0101A03-1',
      designationArticle: 'Article test 0101A03-1',
      dateFinValidite: '2099-12-31',
      etapeSuivi: '1',
      numeroOperation: '10',
      codeAtelier: '19',
      codePoste: 'POSTE-19-01',
      nombrePostes: 1,
      groupePosteCharge: 'UAP2-USINAGE',
      designationOperation: 'Usinage principal',
      alternativeGamme: '1',
      uniteOperation: 'PC',
      coefficientOperationStock: 1,
      tempsCycle: 0.42,
      trs: 88,
      tempsAttente: 0,
      jalon: 'OUI',
      quantite: 1,
      cadence: 140,
      dateModificationX3: '2026-06-01',
      tempsReglage: 20,
      coefficientCCPT: 1,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-GAMMEFAB-202606',
      isActive: true
    },
    {
      id: 2,
      codeArticle: '0101A03-1',
      designationArticle: 'Article test 0101A03-1',
      dateFinValidite: '2099-12-31',
      etapeSuivi: '1',
      numeroOperation: '20',
      codeAtelier: '19',
      codePoste: 'POSTE-19-02',
      nombrePostes: 1,
      groupePosteCharge: 'UAP2-CONTROLE',
      designationOperation: 'Controle final',
      alternativeGamme: '1',
      uniteOperation: 'PC',
      coefficientOperationStock: 1,
      tempsCycle: 0.18,
      trs: 92,
      tempsAttente: 0,
      jalon: 'NON',
      quantite: 1,
      cadence: 220,
      dateModificationX3: '2026-06-01',
      tempsReglage: 10,
      coefficientCCPT: 1,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-GAMMEFAB-202606',
      isActive: true
    },
    {
      id: 3,
      codeArticle: '0102A03-1',
      designationArticle: 'Article test 0102A03-1',
      dateFinValidite: '2099-12-31',
      etapeSuivi: '1',
      numeroOperation: '10',
      codeAtelier: '20',
      codePoste: 'POSTE-20-01',
      nombrePostes: 2,
      groupePosteCharge: 'UAP2-TRONCONNAGE',
      designationOperation: 'Tronconnage',
      alternativeGamme: '1',
      uniteOperation: 'PC',
      coefficientOperationStock: 1,
      tempsCycle: 0.5,
      trs: 84,
      tempsAttente: 0,
      jalon: 'OUI',
      quantite: 1,
      cadence: 120,
      dateModificationX3: '2026-06-01',
      tempsReglage: 25,
      coefficientCCPT: 1,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-GAMMEFAB-202606',
      isActive: true
    },
    {
      id: 4,
      codeArticle: '0102A03-1',
      designationArticle: 'Article test 0102A03-1',
      dateFinValidite: '2099-12-31',
      etapeSuivi: '1',
      numeroOperation: '20',
      codeAtelier: '21',
      codePoste: 'POSTE-21-01',
      nombrePostes: 1,
      groupePosteCharge: 'UAP2-EBAVURAGE',
      designationOperation: 'Ebavurage',
      alternativeGamme: '1',
      uniteOperation: 'PC',
      coefficientOperationStock: 1,
      tempsCycle: 0.22,
      trs: 90,
      tempsAttente: 0,
      jalon: 'NON',
      quantite: 1,
      cadence: 190,
      dateModificationX3: '2026-06-01',
      tempsReglage: 12,
      coefficientCCPT: 1,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-GAMMEFAB-202606',
      isActive: true
    },
    {
      id: 5,
      codeArticle: '0203203-1',
      designationArticle: 'Article test 0203203-1',
      dateFinValidite: '2099-12-31',
      etapeSuivi: '1',
      numeroOperation: '10',
      codeAtelier: '09',
      codePoste: 'POSTE-09-01',
      nombrePostes: 1,
      groupePosteCharge: 'UAP2-LAVAGE',
      designationOperation: 'Lavage',
      alternativeGamme: '1',
      uniteOperation: 'PC',
      coefficientOperationStock: 1,
      tempsCycle: 0.3,
      trs: 86,
      tempsAttente: 0,
      jalon: 'OUI',
      quantite: 1,
      cadence: 160,
      dateModificationX3: '2026-06-01',
      tempsReglage: 15,
      coefficientCCPT: 1,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-GAMMEFAB-202606',
      isActive: true
    }
  ],
  refNomenclature: [
    {
      id: 1,
      codeParentNiveau: '0101A03-1',
      codeComposant: 'M3010L0',
      codeArticleParent: '0101A03-1',
      designationArticleParent: 'Article test 0101A03-1',
      niveau: 1,
      designationComposant: 'Matiere M3010L0',
      sequence: '10',
      quantiteLien: 1.5,
      quantiteLienCumulee: 1.5,
      uniteQuantite: 'KG',
      categorieComposant: 'MATIERE',
      typeLien: 'NORMAL',
      pourcentageRebut: 2,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-BOMARTPARENT-202606',
      isActive: true
    },
    {
      id: 2,
      codeParentNiveau: '0102A03-1',
      codeComposant: 'M3000L0',
      codeArticleParent: '0102A03-1',
      designationArticleParent: 'Article test 0102A03-1',
      niveau: 1,
      designationComposant: 'Matiere M3000L0',
      sequence: '10',
      quantiteLien: 2,
      quantiteLienCumulee: 2,
      uniteQuantite: 'KG',
      categorieComposant: 'MATIERE',
      typeLien: 'NORMAL',
      pourcentageRebut: 1.5,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-BOMARTPARENT-202606',
      isActive: true
    },
    {
      id: 3,
      codeParentNiveau: '0203203-1',
      codeComposant: 'M3060L0',
      codeArticleParent: '0203203-1',
      designationArticleParent: 'Article test 0203203-1',
      niveau: 1,
      designationComposant: 'Matiere M3060L0',
      sequence: '10',
      quantiteLien: 1.2,
      quantiteLienCumulee: 1.2,
      uniteQuantite: 'KG',
      categorieComposant: 'MATIERE',
      typeLien: 'NORMAL',
      pourcentageRebut: 2.5,
      sourceSystem: SOURCE,
      importBatchId: 'IMP-BOMARTPARENT-202606',
      isActive: true
    }
  ],
  x3RapportsMachine: [
    {
      id: 1,
      dateProduction: '2026-06-10',
      codePoste: 'POSTE-19-01',
      numeroOperation: '10',
      codeArticle: '0101A03-1',
      designationArticle: 'Article test 0101A03-1',
      numeroOF: 'OF18100563',
      equipe: 'A',
      quantiteEMR: 600,
      quantiteRebutEMR: 12,
      alternativeGamme: '1',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YRJMCH-202606',
      isActive: true
    },
    {
      id: 2,
      dateProduction: '2026-06-11',
      codePoste: 'POSTE-19-01',
      numeroOperation: '10',
      codeArticle: '0101A03-1',
      designationArticle: 'Article test 0101A03-1',
      numeroOF: 'OF18100563',
      equipe: 'B',
      quantiteEMR: 400,
      quantiteRebutEMR: 8,
      alternativeGamme: '1',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YRJMCH-202606',
      isActive: true
    },
    {
      id: 3,
      dateProduction: '2026-06-12',
      codePoste: 'POSTE-20-01',
      numeroOperation: '10',
      codeArticle: '0102A03-1',
      designationArticle: 'Article test 0102A03-1',
      numeroOF: 'OF18100564',
      equipe: 'A',
      quantiteEMR: 800,
      quantiteRebutEMR: 5,
      alternativeGamme: '1',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YRJMCH-202606',
      isActive: true
    },
    {
      id: 4,
      dateProduction: '2026-06-13',
      codePoste: 'POSTE-09-01',
      numeroOperation: '10',
      codeArticle: '0203203-1',
      designationArticle: 'Article test 0203203-1',
      numeroOF: 'OF18100565',
      equipe: 'B',
      quantiteEMR: 450,
      quantiteRebutEMR: 10,
      alternativeGamme: '1',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YRJMCH-202606',
      isActive: true
    },
    {
      id: 5,
      dateProduction: '2026-06-14',
      codePoste: 'POSTE-19-01',
      numeroOperation: '10',
      codeArticle: '0101A03-1',
      designationArticle: 'Article test 0101A03-1',
      numeroOF: 'OF18100566',
      equipe: 'C',
      quantiteEMR: 250,
      quantiteRebutEMR: 4,
      alternativeGamme: '1',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-YRJMCH-202606',
      isActive: true
    }
  ],
  x3MouvementsStock: [
    {
      id: 1,
      dateImputation: '2026-06-10',
      codeArticle: 'M3010L0',
      designationArticle: 'Matiere M3010L0',
      dateCreationX3: '2026-06-10',
      heureCreationX3: '08:30',
      utilisateurX3: 'X3USER',
      quantite: 1600,
      unite: 'KG',
      typeTransaction: 'SORTIE',
      typePiece: 'OF',
      numeroLot: 'LOT-001',
      numeroOF: 'OF18100563',
      referenceOrigineX3: '23531',
      emplacement: 'MAG-01',
      statut: 'VALIDE',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-MVTSTOCKSVCR-202606',
      isActive: true
    },
    {
      id: 2,
      dateImputation: '2026-06-11',
      codeArticle: 'M3010L0',
      designationArticle: 'Matiere M3010L0',
      dateCreationX3: '2026-06-11',
      heureCreationX3: '09:15',
      utilisateurX3: 'X3USER',
      quantite: -40,
      unite: 'KG',
      typeTransaction: 'RETOUR',
      typePiece: 'OF',
      numeroLot: 'LOT-001',
      numeroOF: 'OF18100563',
      referenceOrigineX3: '23532',
      emplacement: 'MAG-01',
      statut: 'VALIDE',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-MVTSTOCKSVCR-202606',
      isActive: true
    },
    {
      id: 3,
      dateImputation: '2026-06-12',
      codeArticle: 'M3000L0',
      designationArticle: 'Matiere M3000L0',
      dateCreationX3: '2026-06-12',
      heureCreationX3: '10:00',
      utilisateurX3: 'X3USER',
      quantite: 1600,
      unite: 'KG',
      typeTransaction: 'SORTIE',
      typePiece: 'OF',
      numeroLot: 'LOT-002',
      numeroOF: 'OF18100564',
      referenceOrigineX3: '23533',
      emplacement: 'MAG-02',
      statut: 'VALIDE',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-MVTSTOCKSVCR-202606',
      isActive: true
    },
    {
      id: 4,
      dateImputation: '2026-06-13',
      codeArticle: 'M3060L0',
      designationArticle: 'Matiere M3060L0',
      dateCreationX3: '2026-06-13',
      heureCreationX3: '10:30',
      utilisateurX3: 'X3USER',
      quantite: 540,
      unite: 'KG',
      typeTransaction: 'SORTIE',
      typePiece: 'OF',
      numeroLot: 'LOT-003',
      numeroOF: 'OF18100565',
      referenceOrigineX3: '23534',
      emplacement: 'MAG-03',
      statut: 'VALIDE',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-MVTSTOCKSVCR-202606',
      isActive: true
    },
    {
      id: 5,
      dateImputation: '2026-06-14',
      codeArticle: 'M3010L0',
      designationArticle: 'Matiere M3010L0',
      dateCreationX3: '2026-06-14',
      heureCreationX3: '11:00',
      utilisateurX3: 'X3USER',
      quantite: 375,
      unite: 'KG',
      typeTransaction: 'SORTIE',
      typePiece: 'OF',
      numeroLot: 'LOT-004',
      numeroOF: 'OF18100566',
      referenceOrigineX3: '23535',
      emplacement: 'MAG-01',
      statut: 'VALIDE',
      sourceSystem: SOURCE,
      importBatchId: 'IMP-MVTSTOCKSVCR-202606',
      isActive: true
    }
  ],
  inventaireConteneurs: [
    {
      id: 1,
      dateInventaire: '2026-06-30',
      numeroConteneur: 'CONT-701',
      numeroOF: 'OF18100563',
      codeTypeMatiere: 'M3010L0',
      poidsReelKg: 3500,
      observation: 'Conteneur reception fonderie',
      statut: 'Valide',
      creePar: 'Responsable Fonderie',
      creeLe: '2026-06-30T18:00:00'
    },
    {
      id: 2,
      dateInventaire: '2026-06-30',
      numeroConteneur: 'CONT-702',
      numeroOF: 'OF18100566',
      codeTypeMatiere: 'M3010L0',
      poidsReelKg: 200,
      observation: 'Encours OF sans reception U2',
      statut: 'Valide',
      creePar: 'Responsable Fonderie',
      creeLe: '2026-06-30T18:05:00'
    },
    {
      id: 3,
      dateInventaire: '2026-06-30',
      numeroConteneur: 'CONT-703',
      numeroOF: 'OF18100564',
      codeTypeMatiere: 'M3000L0',
      poidsReelKg: 600,
      observation: 'Conteneur standard',
      statut: 'Valide',
      creePar: 'Responsable Fonderie',
      creeLe: '2026-06-30T18:10:00'
    },
    {
      id: 4,
      dateInventaire: '2026-06-30',
      numeroConteneur: 'CONT-704',
      numeroOF: 'OF18100565',
      codeTypeMatiere: 'M3060L0',
      poidsReelKg: 700,
      observation: 'Stock fin periode',
      statut: 'Valide',
      creePar: 'Responsable Fonderie',
      creeLe: '2026-06-30T18:15:00'
    }
  ],
  inventaireCategories: [
    {
      id: 101,
      dateInventaire: '2026-06-30',
      categorie: 'REBUT',
      codeTypeMatiere: 'M3010L0',
      poidsReelKg: 1225,
      commentaire: 'Rebuts inclus bilan',
      inclusBilanFonderie: true,
      statut: 'Valide',
      creePar: 'Responsable Fonderie',
      creeLe: '2026-06-30T18:20:00'
    },
    {
      id: 102,
      dateInventaire: '2026-06-30',
      categorie: 'MP LINGOT',
      codeTypeMatiere: 'M3000L0',
      poidsReelKg: 270,
      commentaire: 'Stock categorie inclus bilan',
      inclusBilanFonderie: true,
      statut: 'Valide',
      creePar: 'Responsable Fonderie',
      creeLe: '2026-06-30T18:25:00'
    },
    {
      id: 103,
      dateInventaire: '2026-06-30',
      categorie: 'REBUT',
      codeTypeMatiere: 'M3060L0',
      poidsReelKg: 0,
      commentaire: 'Aucun stock categorie restant',
      inclusBilanFonderie: true,
      statut: 'Valide',
      creePar: 'Responsable Fonderie',
      creeLe: '2026-06-30T18:30:00'
    }
  ],
  inventaireFours: [
    {
      id: 201,
      dateInventaire: '2026-06-30',
      mois: 6,
      annee: 2026,
      codeFour: 'Four CBP 01',
      codeTypeMatiere: 'M3010L0',
      quantite: 800,
      unite: 'KG',
      observation: 'Visible rapport inventaire, exclu bilan fonderie',
      statut: 'Valide',
      inclusBilanFonderie: false,
      visibleRapportInventaire: true
    },
    {
      id: 202,
      dateInventaire: '2026-06-30',
      mois: 6,
      annee: 2026,
      codeFour: 'Four CDQ',
      codeTypeMatiere: 'M3000L0',
      quantite: 120,
      unite: 'KG',
      observation: 'Visible rapport inventaire, exclu bilan fonderie',
      statut: 'Valide',
      inclusBilanFonderie: false,
      visibleRapportInventaire: true
    },
    {
      id: 203,
      dateInventaire: '2026-06-30',
      mois: 6,
      annee: 2026,
      codeFour: 'Four CBP 02',
      codeTypeMatiere: 'M3060L0',
      quantite: 90,
      unite: 'KG',
      observation: 'Visible rapport inventaire, exclu bilan fonderie',
      statut: 'Valide',
      inclusBilanFonderie: false,
      visibleRapportInventaire: true
    }
  ],
  stockDebut: [
    { periode: '2026-06', codeTypeMatiere: 'M3000L0', stockDebutKg: 1200, source: 'Inventaire valide 2026-05' },
    { periode: '2026-06', codeTypeMatiere: 'M3010L0', stockDebutKg: 4800, source: 'Inventaire valide 2026-05' },
    { periode: '2026-06', codeTypeMatiere: 'M3060L0', stockDebutKg: 800, source: 'Inventaire valide 2026-05' }
  ],
  dechetsFonderie: [
    {
      id: 1,
      dateDeclaration: '2026-06-15',
      semaine: 25,
      codeTypeMatiere: 'M3010L0',
      typeDechet: 'Bavure',
      poidsDechetKg: 156,
      observation: 'Bavures triees',
      declarantId: 'U-FONDERIE',
      declarantNom: 'Responsable Fonderie',
      statut: 'Valide',
      creeLe: '2026-06-15T17:00:00'
    },
    {
      id: 2,
      dateDeclaration: '2026-06-16',
      semaine: 25,
      codeTypeMatiere: 'M3010L0',
      typeDechet: 'Poudre',
      poidsDechetKg: 64,
      observation: 'Poudre recuperee',
      declarantId: 'U-FONDERIE',
      declarantNom: 'Responsable Fonderie',
      statut: 'Valide',
      creeLe: '2026-06-16T17:05:00'
    },
    {
      id: 3,
      dateDeclaration: '2026-06-17',
      semaine: 25,
      codeTypeMatiere: 'M3000L0',
      typeDechet: 'Bavure',
      poidsDechetKg: 200,
      observation: 'Bavures atelier 20',
      declarantId: 'U-FONDERIE',
      declarantNom: 'Responsable Fonderie',
      statut: 'Valide',
      creeLe: '2026-06-17T17:10:00'
    },
    {
      id: 4,
      dateDeclaration: '2026-06-18',
      semaine: 25,
      codeTypeMatiere: 'M3000L0',
      typeDechet: 'Poudre',
      poidsDechetKg: 50,
      observation: 'Poudre atelier 20',
      declarantId: 'U-FONDERIE',
      declarantNom: 'Responsable Fonderie',
      statut: 'Valide',
      creeLe: '2026-06-18T17:15:00'
    },
    {
      id: 5,
      dateDeclaration: '2026-06-19',
      semaine: 25,
      codeTypeMatiere: 'M3060L0',
      typeDechet: 'Bavure',
      poidsDechetKg: 50,
      observation: 'Bavures atelier 09',
      declarantId: 'U-FONDERIE',
      declarantNom: 'Responsable Fonderie',
      statut: 'Valide',
      creeLe: '2026-06-19T17:20:00'
    },
    {
      id: 6,
      dateDeclaration: '2026-06-20',
      semaine: 25,
      codeTypeMatiere: 'M3060L0',
      typeDechet: 'Poudre',
      poidsDechetKg: 60,
      observation: 'Poudre atelier 09',
      declarantId: 'U-FONDERIE',
      declarantNom: 'Responsable Fonderie',
      statut: 'Valide',
      creeLe: '2026-06-20T17:25:00'
    }
  ],
  receptionsUsinage2: [
    {
      id: 1,
      numeroOF: 'OF18100563',
      dateReception: '2026-06-10',
      codeArticle: '0101A03-1',
      designationArticle: 'Article test 0101A03-1',
      origine: 'FONDERIE',
      atelierOrigine: '19',
      codeTypeMatiere: 'M3010L0',
      poidsTotalReceptionneKg: 1455,
      nombrePiecesReceptionnees: 970,
      poidsPhysiqueUnitaireKg: 1.5,
      quantiteLienUtilisee: 1.5,
      uniteQuantiteLien: 'KG',
      poidsTheoriqueUnitaireKg: 1.5,
      poidsTheoriqueTotalKg: 1455,
      ecartPoidsUnitaireKg: 0,
      tauxEcartPoids: 0,
      statutAlertePoids: 'Conforme',
      seuilAlertePoidsPourcentage: SEUIL_POIDS,
      gammeFabricationId: 1,
      nomenclatureId: 1,
      mois: '2026-06',
      statut: 'Valide',
      source: 'SaisieManuelle',
      creePar: 'Responsable Usinage 2',
      creeLe: '2026-06-10T16:00:00'
    },
    {
      id: 2,
      numeroOF: 'OF18100564',
      dateReception: '2026-06-12',
      codeArticle: '0102A03-1',
      designationArticle: 'Article test 0102A03-1',
      origine: 'FONDERIE',
      atelierOrigine: '20',
      codeTypeMatiere: 'M3000L0',
      poidsTotalReceptionneKg: 1680,
      nombrePiecesReceptionnees: 800,
      poidsPhysiqueUnitaireKg: 2.1,
      quantiteLienUtilisee: 2,
      uniteQuantiteLien: 'KG',
      poidsTheoriqueUnitaireKg: 2,
      poidsTheoriqueTotalKg: 1600,
      ecartPoidsUnitaireKg: 0.1,
      tauxEcartPoids: 5,
      statutAlertePoids: 'Alerte',
      seuilAlertePoidsPourcentage: SEUIL_POIDS,
      gammeFabricationId: 3,
      nomenclatureId: 2,
      mois: '2026-06',
      statut: 'Valide',
      source: 'SaisieManuelle',
      creePar: 'Responsable Usinage 2',
      creeLe: '2026-06-12T16:05:00'
    },
    {
      id: 3,
      numeroOF: 'OF18100565',
      dateReception: '2026-06-13',
      codeArticle: '0203203-1',
      designationArticle: 'Article test 0203203-1',
      origine: 'FONDERIE',
      atelierOrigine: '09',
      codeTypeMatiere: 'M3060L0',
      poidsTotalReceptionneKg: 480,
      nombrePiecesReceptionnees: 400,
      poidsPhysiqueUnitaireKg: 1.2,
      quantiteLienUtilisee: 1.2,
      uniteQuantiteLien: 'KG',
      poidsTheoriqueUnitaireKg: 1.2,
      poidsTheoriqueTotalKg: 480,
      ecartPoidsUnitaireKg: 0,
      tauxEcartPoids: 0,
      statutAlertePoids: 'Conforme',
      seuilAlertePoidsPourcentage: SEUIL_POIDS,
      gammeFabricationId: 5,
      nomenclatureId: 3,
      mois: '2026-06',
      statut: 'Valide',
      source: 'SaisieManuelle',
      creePar: 'Responsable Usinage 2',
      creeLe: '2026-06-13T16:10:00'
    }
  ],
  rebutsUsinage2: [
    {
      id: 1,
      numeroOF: 'OF18100563',
      codeArticle: '0101A03-1',
      codeTypeMatiere: 'M3010L0',
      dateDeclaration: '2026-06-10',
      nombrePiecesRebut: 10,
      poidsRebutKg: 15,
      observation: 'Rebut U2 lie a OF18100563',
      statut: 'Valide',
      creePar: 'Responsable Usinage 2'
    },
    {
      id: 2,
      numeroOF: 'OF18100564',
      codeArticle: '0102A03-1',
      codeTypeMatiere: 'M3000L0',
      dateDeclaration: '2026-06-12',
      nombrePiecesRebut: 10,
      poidsRebutKg: 20,
      observation: 'Rebut U2 lie a OF18100564',
      statut: 'Valide',
      creePar: 'Responsable Usinage 2'
    },
    {
      id: 3,
      numeroOF: 'OF18100565',
      codeArticle: '0203203-1',
      codeTypeMatiere: 'M3060L0',
      dateDeclaration: '2026-06-13',
      nombrePiecesRebut: 5,
      poidsRebutKg: 6,
      observation: 'Rebut U2 lie a OF18100565',
      statut: 'Valide',
      creePar: 'Responsable Usinage 2'
    }
  ],
  rebutsUap2: [],
  importBatches: [
    {
      id: 'IMP-YLISTOFFERM-202606',
      typeImport: 'YLISTOFFERM',
      profilImport: 'YLISTOFFERM_REEL',
      sourceSystem: SOURCE,
      nomFichier: 'YListOFFerm_202606.xlsx',
      fileHash: 'mock-ylistofferm-mapping-reel',
      dateDebut: '2026-06-30T08:00:00',
      dateFin: '2026-06-30T08:00:05',
      nombreLignesLues: 5,
      nombreLignesImportees: 5,
      nombreLignesIgnorees: 0,
      nombreErreurs: 0,
      statut: 'Succes',
      importePar: 'Administrateur SOPAL'
    },
    {
      id: 'IMP-GAMMEFAB-202606',
      typeImport: 'GAMMEFAB',
      profilImport: 'GAMMEFAB_REEL',
      sourceSystem: SOURCE,
      nomFichier: 'GAMMEFAB_202606.xlsx',
      fileHash: 'mock-gammefab-mapping-reel',
      dateDebut: '2026-06-30T08:05:00',
      dateFin: '2026-06-30T08:05:05',
      nombreLignesLues: 5,
      nombreLignesImportees: 5,
      nombreLignesIgnorees: 0,
      nombreErreurs: 0,
      statut: 'Succes',
      importePar: 'Administrateur SOPAL'
    },
    {
      id: 'IMP-BOMARTPARENT-202606',
      typeImport: 'BOMARTPARENT',
      profilImport: 'BOMARTPARENT_REEL',
      sourceSystem: SOURCE,
      nomFichier: 'BOMARTPARENT_202606.xlsx',
      fileHash: 'mock-bomartparent-mapping-reel',
      dateDebut: '2026-06-30T08:10:00',
      dateFin: '2026-06-30T08:10:05',
      nombreLignesLues: 3,
      nombreLignesImportees: 3,
      nombreLignesIgnorees: 0,
      nombreErreurs: 0,
      statut: 'Succes',
      importePar: 'Administrateur SOPAL'
    },
    {
      id: 'IMP-YRJMCH-202606',
      typeImport: 'YRJMCH',
      profilImport: 'YRJMCH_REEL',
      sourceSystem: SOURCE,
      nomFichier: 'YRJMCH_202606.xlsx',
      fileHash: 'mock-yrjmch-mapping-reel',
      dateDebut: '2026-06-30T08:15:00',
      dateFin: '2026-06-30T08:15:05',
      nombreLignesLues: 5,
      nombreLignesImportees: 5,
      nombreLignesIgnorees: 0,
      nombreErreurs: 0,
      statut: 'Succes',
      importePar: 'Administrateur SOPAL'
    },
    {
      id: 'IMP-MVTSTOCKSVCR-202606',
      typeImport: 'MVTSTOCKSVCR',
      profilImport: 'MVTSTOCKSVCR_REEL',
      sourceSystem: SOURCE,
      nomFichier: 'MVTSTOCKSVCR_202606.xlsx',
      fileHash: 'mock-mvtstocksvcr-mapping-reel',
      dateDebut: '2026-06-30T08:20:00',
      dateFin: '2026-06-30T08:20:05',
      nombreLignesLues: 5,
      nombreLignesImportees: 5,
      nombreLignesIgnorees: 0,
      nombreErreurs: 0,
      statut: 'Succes',
      importePar: 'Administrateur SOPAL'
    }
  ]
};
