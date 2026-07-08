import { SortieX3Mouvement } from '@/features/sorties-x3/sortiesX3.types';
import { DechetMouvement, DechetType } from '@/features/dechets-fonderie/dechets.types';
import { ReceptionUsinage2Mouvement } from '@/features/receptions-usinage2/receptions.types';
import { InventaireConteneur, InventaireCategorie, InventaireFour } from '@/features/inventaire/inventaire.types';
import { BilanMatiereReport, BilanUsinage2ParPiece, BilanUap2ParOF } from '@/features/bilan-matiere/bilan.types';
import { AuditLogEntry } from '@/features/audit/audit.types';

// Keys
const KEYS = {
  SORTIES_X3: 'sopal_sorties_x3',
  DECHETS: 'sopal_dechets',
  RECEPTIONS: 'sopal_receptions',
  INVENTAIRE_CONTENEURS: 'sopal_inventaire_conteneurs',
  INVENTAIRE_CATEGORIES: 'sopal_inventaire_categories',
  INVENTAIRE_FOURS: 'sopal_inventaire_fours',
  BILANS: 'sopal_bilans',
  BILANS_USINAGE: 'sopal_bilans_usinage',
  BILANS_UAP2: 'sopal_bilans_uap2',
  AUDIT_LOGS: 'sopal_audit_logs'
};

// --- INITIAL SEED DATA ---

const SEED_SORTIES_X3: SortieX3Mouvement[] = [
  {
    id: 1,
    dateImputation: '2026-06-15',
    codeMatiere: 'M3010L0',
    designation: 'Laiton Sanitaire (Sans Plomb)',
    dateCreation: '2026-06-15',
    heureCreation: '08:42:00',
    utilisateur: 'M. TRABELSI',
    quantiteImportee: -2203.0,
    quantiteUtilisee: 2203.0,
    unite: 'KG',
    transaction: 'TRS-4421',
    typePiece: 'SMR',
    lot: 'LOT-2606-A',
    pieceOrigine: 'X3-99120',
    designationOrigine: 'Billet de Laiton',
    emplacement: 'DEP-FOND-1',
    statut: 'Synchronisé',
    source: 'API Sage X3'
  },
  {
    id: 2,
    dateImputation: '2026-06-16',
    codeMatiere: 'M3000L0',
    designation: 'Laiton Standard Industriel',
    dateCreation: '2026-06-16',
    heureCreation: '10:15:30',
    utilisateur: 'M. TRABELSI',
    quantiteImportee: -1500.0,
    quantiteUtilisee: 1500.0,
    unite: 'KG',
    transaction: 'TRS-4422',
    typePiece: 'SMR',
    lot: 'LOT-2606-B',
    pieceOrigine: 'X3-99121',
    designationOrigine: 'Lopin de Laiton Standard',
    emplacement: 'DEP-FOND-1',
    statut: 'Synchronisé',
    source: 'API Sage X3'
  },
  {
    id: 3,
    dateImputation: '2026-06-20',
    codeMatiere: 'M3010L0',
    designation: 'Laiton Sanitaire (Sans Plomb)',
    dateCreation: '2026-06-20',
    heureCreation: '14:22:10',
    utilisateur: 'K. BEN ALI',
    quantiteImportee: -3100.0,
    quantiteUtilisee: 3100.0,
    unite: 'KG',
    transaction: 'TRS-4439',
    typePiece: 'SMR',
    lot: 'LOT-2606-C',
    pieceOrigine: 'X3-99155',
    designationOrigine: 'Billet de Laiton',
    emplacement: 'DEP-FOND-1',
    statut: 'Synchronisé',
    source: 'Fichier Excel'
  },
  // Synthetic movements to match the total required for SOPAL official June 2026 Bilan
  {
    id: 4,
    dateImputation: '2026-06-28',
    codeMatiere: 'M3010L0',
    designation: 'Laiton Sanitaire (Sans Plomb)',
    dateCreation: '2026-06-28',
    heureCreation: '17:00:00',
    utilisateur: 'SYSTEM',
    quantiteImportee: -27111.0,
    quantiteUtilisee: 27111.0,
    unite: 'KG',
    transaction: 'TRS-4450',
    typePiece: 'SMR',
    lot: 'LOT-2606-D',
    pieceOrigine: 'X3-99180',
    designationOrigine: 'Importation Consolidée Sage X3',
    emplacement: 'DEP-FOND-1',
    statut: 'Synchronisé',
    source: 'API Sage X3'
  },
  {
    id: 5,
    dateImputation: '2026-06-29',
    codeMatiere: 'M3000L0',
    designation: 'Laiton Standard Industriel',
    dateCreation: '2026-06-29',
    heureCreation: '11:30:00',
    utilisateur: 'SYSTEM',
    quantiteImportee: -10435.0,
    quantiteUtilisee: 10435.0,
    unite: 'KG',
    transaction: 'TRS-4451',
    typePiece: 'SMR',
    lot: 'LOT-2606-E',
    pieceOrigine: 'X3-99181',
    designationOrigine: 'Importation Consolidée Sage X3',
    emplacement: 'DEP-FOND-1',
    statut: 'Synchronisé',
    source: 'API Sage X3'
  }
];

const SEED_DECHETS: DechetMouvement[] = [
  {
    id: 1,
    dateDeclaration: '2026-06-25',
    semaine: 26,
    codeArticle: 'ART-00912',
    designationArticle: 'Corps Robinet 1/2',
    codeTypeMatiere: 'M3010L0',
    quantiteKg: 420.0,
    unite: 'KG',
    typeDechet: 'Bavure',
    commentaire: 'Lots défectueux après coulée du matin',
    statut: 'Valide',
    utilisateur: 'A. TRABELSI'
  },
  {
    id: 2,
    dateDeclaration: '2026-06-26',
    semaine: 26,
    codeArticle: 'ART-00984',
    designationArticle: 'Raccord Coudé M2',
    codeTypeMatiere: 'M3000L0',
    quantiteKg: 187.0,
    unite: 'KG',
    typeDechet: 'Poudre',
    commentaire: "Porosités d'usinage signalées par atelier",
    statut: 'Valide',
    utilisateur: 'A. TRABELSI'
  },
  {
    id: 3,
    dateDeclaration: '2026-06-27',
    semaine: 26,
    codeArticle: 'ART-01005',
    designationArticle: "Robinet d'arrêt DN20",
    codeTypeMatiere: 'M3010L0',
    quantiteKg: 332.0,
    unite: 'KG',
    typeDechet: 'Bavure',
    commentaire: 'Criques thermiques constatées',
    statut: 'Valide', // Seed as Valide to complete the math for M3010L0 (420 + 332 = 752)
    utilisateur: 'A. TRABELSI'
  }
];

const normalizeDechetType = (value?: string): DechetType => {
  const normalized = (value || '').trim().toLowerCase();
  if (
    normalized === 'poudre' ||
    normalized.includes('poudre') ||
    normalized.includes('scorie') ||
    normalized.includes('limaille') ||
    normalized.includes('copeaux')
  ) {
    return 'Poudre';
  }
  return 'Bavure';
};

const SEED_RECEPTIONS: ReceptionUsinage2Mouvement[] = [
  {
    id: 1,
    numOf: 'OF-9831',
    dateReception: '2026-06-25',
    codeArticle: 'ART-00912',
    designationArticle: 'Corps Robinet 1/2',
    poidsKg: 1420.0,
    nombrePieces: 3380,
    atelierOrigine: 'FONDERIE',
    codeTypeMatiere: 'M3010L0',
    designationTypeMatiere: 'Laiton Sanitaire (Sans Plomb)',
    mois: '2026-06',
    poidsPhysique: 1420.0,
    source: 'Synchronisé ERP',
    utilisateur: 'K. BEN ALI'
  },
  {
    id: 2,
    numOf: 'OF-9832',
    dateReception: '2026-06-26',
    codeArticle: 'ART-00984',
    designationArticle: 'Raccord Coudé M2',
    poidsKg: 580.0,
    nombrePieces: 3222,
    atelierOrigine: 'FONDERIE',
    codeTypeMatiere: 'M3000L0',
    designationTypeMatiere: 'Laiton Standard Industriel',
    mois: '2026-06',
    poidsPhysique: 580.0,
    source: 'Synchronisé ERP',
    utilisateur: 'K. BEN ALI'
  },
  {
    id: 3,
    numOf: 'OF-9835',
    dateReception: '2026-06-27',
    codeArticle: 'ART-01005',
    designationArticle: "Robinet d'arrêt DN20",
    poidsKg: 900.0,
    nombrePieces: 1380,
    atelierOrigine: 'FONDERIE',
    codeTypeMatiere: 'M3010L0',
    designationTypeMatiere: 'Laiton Sanitaire (Sans Plomb)',
    mois: '2026-06',
    poidsPhysique: 900.0,
    source: 'Saisie Manuelle',
    utilisateur: 'K. BEN ALI'
  },
  // Synthetic receptions to match the total required for SOPAL official June 2026 Bilan
  {
    id: 4,
    numOf: 'OF-9839',
    dateReception: '2026-06-29',
    codeArticle: 'ART-00912',
    designationArticle: 'Corps Robinet 1/2',
    poidsKg: 27075.0,
    nombrePieces: 64500,
    atelierOrigine: 'FONDERIE',
    codeTypeMatiere: 'M3010L0',
    designationTypeMatiere: 'Laiton Sanitaire (Sans Plomb)',
    mois: '2026-06',
    poidsPhysique: 27075.0,
    source: 'Synchronisé ERP',
    utilisateur: 'SYSTEM'
  },
  {
    id: 5,
    numOf: 'OF-9840',
    dateReception: '2026-06-29',
    codeArticle: 'ART-00984',
    designationArticle: 'Raccord Coudé M2',
    poidsKg: 11357.0,
    nombrePieces: 63100,
    atelierOrigine: 'FONDERIE',
    codeTypeMatiere: 'M3000L0',
    designationTypeMatiere: 'Laiton Standard Industriel',
    mois: '2026-06',
    poidsPhysique: 11357.0,
    source: 'Synchronisé ERP',
    utilisateur: 'SYSTEM'
  }
];

const SEED_INVENTAIRE_CONTENEURS: InventaireConteneur[] = [
  {
    id: 1,
    dateInventaire: '2026-06-30',
    mois: 6,
    annee: 2026,
    numeroConteneur: 'CONT-702',
    ofDestination: 'OF-9831',
    codeArticle: 'ART-00912',
    designationArticle: 'Corps Robinet 1/2',
    typeMatiereId: 1,
    codeTypeMatiere: 'M3010L0',
    quantiteReelle: 3410,
    unite: 'KG',
    observation: "Pesée de fin d'équipe validée",
    statut: 'Valide',
  },
  {
    id: 2,
    dateInventaire: '2026-06-30',
    mois: 6,
    annee: 2026,
    numeroConteneur: 'CONT-705',
    ofDestination: 'OF-9832',
    codeArticle: 'ART-00984',
    designationArticle: 'Raccord Coudé M2',
    typeMatiereId: 2,
    codeTypeMatiere: 'M3000L0',
    quantiteReelle: 503,
    unite: 'KG',
    observation: 'Conteneur standard',
    statut: 'Valide'
  }
];

const SEED_INVENTAIRE_CATEGORIES: InventaireCategorie[] = [
  {
    id: 101,
    mois: 6,
    annee: 2026,
    categorie: 'REBUT',
    categorieInventaire: 'CategorieBilan',
    typeMatiereId: 1,
    codeTypeMatiere: 'M3010L0',
    quantiteReelle: 1200,
    unite: 'KG',
    commentaire: 'Bavures de fonderie non triées',
    statut: 'Valide'
  },
  {
    id: 102,
    mois: 6,
    annee: 2026,
    categorie: 'Four CBP 01',
    typeMatiereId: 1,
    codeTypeMatiere: 'M3010L0',
    quantiteReelle: 800,
    unite: 'KG',
    commentaire: 'Fond de four estimé',
    statut: 'Valide'
  },
  {
    id: 103,
    mois: 6,
    annee: 2026,
    categorie: 'MP LINGOT',
    typeMatiereId: 2,
    codeTypeMatiere: 'M3000L0',
    quantiteReelle: 400,
    unite: 'KG',
    commentaire: 'Lingots restants au sol',
    statut: 'Valide'
  }
];

const SEED_INVENTAIRE_FOURS: InventaireFour[] = [
  {
    id: 1,
    dateInventaire: '2026-06-30',
    mois: 6,
    annee: 2026,
    four: 'Four CBP 01',
    codeTypeMatiere: 'M3010L0',
    quantite: 250,
    unite: 'KG',
    observation: 'Quantité présente dans le four, suivie pour rapport uniquement',
    statut: 'Brouillon',
    inclusDansBilanFonderie: false,
    visibleDansRapportInventaire: true
  },
  {
    id: 2,
    dateInventaire: '2026-06-30',
    mois: 6,
    annee: 2026,
    four: 'Four CDQ',
    codeTypeMatiere: 'M3000L0',
    quantite: 120,
    unite: 'KG',
    observation: 'Stock en four pour suivi inventaire uniquement',
    statut: 'Valide',
    inclusDansBilanFonderie: false,
    visibleDansRapportInventaire: true
  }
];

// Initial precalculated bilans
const SEED_BILANS: BilanMatiereReport[] = [
  {
    id: 1,
    periode: '2026-06',
    typeMatiere: 'M3010L0',
    encoursDebut: 4800.0,
    sortiesX3: 32414.0,
    inputTotal: 37214.0,
    encoursFinReel: 5410.0,
    dechetsFonderie: 752.0,
    receptionsUsinage2: 29395.0,
    poidsNonReceptionne: 0,
    nombrePiecesNonReceptionnees: 0,
    outputTotal: 35557.0,
    ecartKg: 1657.0,
    tauxEcart: 4.45,
    statut: 'AvecEcart',
    justification: "Écart de pesée sur le conteneur 702 en cours d'analyse.",
    dateCalcul: '2026-06-30'
  },
  {
    id: 2,
    periode: '2026-06',
    typeMatiere: 'M3000L0',
    encoursDebut: 1200.0,
    sortiesX3: 11935.0,
    inputTotal: 13135.0,
    encoursFinReel: 903.0,
    dechetsFonderie: 187.0,
    receptionsUsinage2: 11937.0,
    poidsNonReceptionne: 0,
    nombrePiecesNonReceptionnees: 0,
    outputTotal: 13027.0,
    ecartKg: 108.0,
    tauxEcart: 0.82,
    statut: 'Valide',
    justification: 'Écart mineur conforme aux tolérances usine (0.8%).',
    dateCalcul: '2026-06-30'
  }
];

const SEED_BILANS_USINAGE: BilanUsinage2ParPiece[] = [
  {
    id: 1,
    codeArticle: 'ART-00912',
    designationArticle: 'Corps Robinet 1/2',
    codeTypeMatiere: 'M3010L0',
    nombrePieces: 67880,
    quantiteEmrX3: 28550.0,
    quantiteReceptionneeU2: 28495.0,
    quantiteNonReceptionnee: 55.0,
    ecartParPiece: 55.0 / 67880,
    statutControle: 'OK',
    observation: 'Écart minime sous tolérance',
    periode: '2026-06',
    dateCalcul: '2026-06-30'
  },
  {
    id: 2,
    codeArticle: 'ART-00984',
    designationArticle: 'Raccord Coudé M2',
    codeTypeMatiere: 'M3000L0',
    nombrePieces: 66322,
    quantiteEmrX3: 12050.0,
    quantiteReceptionneeU2: 11937.0,
    quantiteNonReceptionnee: 113.0,
    ecartParPiece: 113.0 / 66322,
    statutControle: 'AVerifier',
    observation: 'Écart de réception à vérifier par le responsable',
    periode: '2026-06',
    dateCalcul: '2026-06-30'
  },
  {
    id: 3,
    codeArticle: 'ART-01005',
    designationArticle: "Robinet d'arrêt DN20",
    codeTypeMatiere: 'M3010L0',
    nombrePieces: 1380,
    quantiteEmrX3: 900.0,
    quantiteReceptionneeU2: 900.0,
    quantiteNonReceptionnee: 0.0,
    ecartParPiece: 0.0,
    statutControle: 'OK',
    observation: 'Quantités conformes',
    periode: '2026-06',
    dateCalcul: '2026-06-30'
  }
];

const SEED_BILANS_UAP2: BilanUap2ParOF[] = [
  {
    id: 1,
    of: 'OF-9831',
    codeArticle: 'ART-00912',
    designationArticle: 'Corps Robinet 1/2',
    codeTypeMatiere: 'M3010L0',
    quantiteSortieMP: 3050.0,
    qteCtrl: 3380,
    poidsPhy: 0.42,
    poidsControle: 1419.6,
    rebutEMR: 25.0,
    rebutSCI: 10.0,
    rebutTRN: 5.0,
    totalRebutEmrSciTrn: 40.0,
    quantiteReceptionneeU2: 1420.0,
    quantiteNonReceptionnee: 15.0,
    ecartParPiece: 0.004,
    rebutUsinage2: 15.0,
    entreeSF: 100.0,
    outputOF: 1419.6 + 40.0 + 1420.0 + 15.0 + 100.0, // 2994.6
    ecartBilanOF: 3050.0 - (1419.6 + 40.0 + 1420.0 + 15.0 + 100.0), // 55.4
    tauxEcartOF: ((3050.0 - (1419.6 + 40.0 + 1420.0 + 15.0 + 100.0)) / 3050.0) * 100, // 1.82
    statut: 'OK',
    observation: 'Conforme',
    periode: '2026-06',
    dateCalcul: '2026-06-30'
  },
  {
    id: 2,
    of: 'OF-9832',
    codeArticle: 'ART-00984',
    designationArticle: 'Raccord Coudé M2',
    codeTypeMatiere: 'M3000L0',
    quantiteSortieMP: 1850.0,
    qteCtrl: 3222,
    poidsPhy: 0.18,
    poidsControle: 579.96,
    rebutEMR: 10.0,
    rebutSCI: 5.0,
    rebutTRN: 2.0,
    totalRebutEmrSciTrn: 17.0,
    quantiteReceptionneeU2: 580.0,
    quantiteNonReceptionnee: 8.0,
    ecartParPiece: 0.002,
    rebutUsinage2: 8.0,
    entreeSF: 650.0,
    outputOF: 579.96 + 17.0 + 580.0 + 8.0 + 650.0, // 1834.96
    ecartBilanOF: 1850.0 - (579.96 + 17.0 + 580.0 + 8.0 + 650.0), // 15.04
    tauxEcartOF: ((1850.0 - (579.96 + 17.0 + 580.0 + 8.0 + 650.0)) / 1850.0) * 100, // 0.81
    statut: 'OK',
    observation: 'Conforme aux tolérances',
    periode: '2026-06',
    dateCalcul: '2026-06-30'
  },
  {
    id: 3,
    of: 'OF-9835',
    codeArticle: 'ART-01005',
    designationArticle: "Robinet d'arrêt DN20",
    codeTypeMatiere: 'M3010L0',
    quantiteSortieMP: 2000.0,
    qteCtrl: 1380,
    poidsPhy: 0.65,
    poidsControle: 897.0,
    rebutEMR: 30.0,
    rebutSCI: 15.0,
    rebutTRN: 5.0,
    totalRebutEmrSciTrn: 50.0,
    quantiteReceptionneeU2: 900.0,
    quantiteNonReceptionnee: -3.0,
    ecartParPiece: -0.002,
    rebutUsinage2: 10.0,
    entreeSF: 100.0,
    outputOF: 897.0 + 50.0 + 900.0 + 10.0 + 100.0, // 1957.0
    ecartBilanOF: 2000.0 - (897.0 + 50.0 + 900.0 + 10.0 + 100.0), // 43.0
    tauxEcartOF: ((2000.0 - (897.0 + 50.0 + 900.0 + 10.0 + 100.0)) / 2000.0) * 100, // 2.15
    statut: 'AVerifier',
    observation: "Ajustement d'inventaire requis",
    periode: '2026-06',
    dateCalcul: '2026-06-30'
  },
  {
    id: 4,
    of: 'OF-9839',
    codeArticle: 'ART-00912',
    designationArticle: 'Corps Robinet 1/2',
    codeTypeMatiere: 'M3010L0',
    quantiteSortieMP: 55200.0,
    qteCtrl: 64500,
    poidsPhy: 0.42,
    poidsControle: 27090.0,
    rebutEMR: 100.0,
    rebutSCI: 50.0,
    rebutTRN: 20.0,
    totalRebutEmrSciTrn: 170.0,
    quantiteReceptionneeU2: 27075.0,
    quantiteNonReceptionnee: 15.0,
    ecartParPiece: 0.0002,
    rebutUsinage2: 120.0,
    entreeSF: 500.0,
    outputOF: 27090.0 + 170.0 + 27075.0 + 120.0 + 500.0, // 54955.0
    ecartBilanOF: 55200.0 - (27090.0 + 170.0 + 27075.0 + 120.0 + 500.0), // 245.0
    tauxEcartOF: ((55200.0 - (27090.0 + 170.0 + 27075.0 + 120.0 + 500.0)) / 55200.0) * 100, // 0.44
    statut: 'OK',
    observation: 'Conforme',
    periode: '2026-06',
    dateCalcul: '2026-06-30'
  },
  {
    id: 5,
    of: 'OF-9840',
    codeArticle: 'ART-00984',
    designationArticle: 'Raccord Coudé M2',
    codeTypeMatiere: 'M3000L0',
    quantiteSortieMP: 35200.0,
    qteCtrl: 63100,
    poidsPhy: 0.18,
    poidsControle: 11358.0,
    rebutEMR: 80.0,
    rebutSCI: 40.0,
    rebutTRN: 10.0,
    totalRebutEmrSciTrn: 130.0,
    quantiteReceptionneeU2: 11357.0,
    quantiteNonReceptionnee: 1.0,
    ecartParPiece: 0.00001,
    rebutUsinage2: 90.0,
    entreeSF: 12000.0,
    outputOF: 11358.0 + 130.0 + 11357.0 + 90.0 + 12000.0, // 34935.0
    ecartBilanOF: 35200.0 - (11358.0 + 130.0 + 11357.0 + 90.0 + 12000.0), // 265.0
    tauxEcartOF: ((35200.0 - (11358.0 + 130.0 + 11357.0 + 90.0 + 12000.0)) / 35200.0) * 100, // 0.75
    statut: 'OK',
    observation: 'Conforme',
    periode: '2026-06',
    dateCalcul: '2026-06-30'
  }
];

// --- STORAGE MANAGER ---

export const getStorageItem = <T>(key: string, defaultValue: T): T => {
  const stored = localStorage.getItem(key);
  if (!stored) {
    localStorage.setItem(key, JSON.stringify(defaultValue));
    return defaultValue;
  }
  try {
    return JSON.parse(stored) as T;
  } catch (e) {
    console.error('Error parsing localStorage key ' + key, e);
    return defaultValue;
  }
};

export const setStorageItem = <T>(key: string, value: T): void => {
  localStorage.setItem(key, JSON.stringify(value));
};

const FOUR_STORAGE_NAMES = ['Four CBP 01', 'Four CBP 02', 'Four CDQ', 'Autre'];

const normalizeInventaireCategorie = (item: InventaireCategorie): InventaireCategorie => {
  const isFour = FOUR_STORAGE_NAMES.includes(item.categorie);
  return {
    ...item,
    categorieInventaire: isFour ? 'FourHorsBilan' : item.categorieInventaire ?? 'CategorieBilan',
    inclusDansBilanFonderie: isFour ? false : item.inclusDansBilanFonderie ?? true,
    visibleDansRapportInventaire: item.visibleDansRapportInventaire ?? true
  };
};

const normalizeInventaireFour = (item: InventaireFour): InventaireFour => ({
  ...item,
  inclusDansBilanFonderie: false,
  visibleDansRapportInventaire: true
});

export const storage = {
  // Sorties X3
  getSortiesX3(): SortieX3Mouvement[] {
    return getStorageItem(KEYS.SORTIES_X3, SEED_SORTIES_X3);
  },
  saveSortiesX3(data: SortieX3Mouvement[]): void {
    setStorageItem(KEYS.SORTIES_X3, data);
  },

  // Déchets
  getDechets(): DechetMouvement[] {
    const dechets = getStorageItem(KEYS.DECHETS, SEED_DECHETS);
    const normalized = dechets.map((dechet) => ({
      ...dechet,
      typeDechet: normalizeDechetType(dechet.typeDechet)
    }));

    if (normalized.some((dechet, index) => dechet.typeDechet !== dechets[index].typeDechet)) {
      setStorageItem(KEYS.DECHETS, normalized);
    }

    return normalized;
  },
  saveDechets(data: DechetMouvement[]): void {
    setStorageItem(
      KEYS.DECHETS,
      data.map((dechet) => ({
        ...dechet,
        typeDechet: normalizeDechetType(dechet.typeDechet)
      }))
    );
  },

  // Réceptions
  getReceptions(): ReceptionUsinage2Mouvement[] {
    return getStorageItem(KEYS.RECEPTIONS, SEED_RECEPTIONS);
  },
  saveReceptions(data: ReceptionUsinage2Mouvement[]): void {
    setStorageItem(KEYS.RECEPTIONS, data);
  },

  // Inventaires
  getConteneurs(): InventaireConteneur[] {
    return getStorageItem(KEYS.INVENTAIRE_CONTENEURS, SEED_INVENTAIRE_CONTENEURS);
  },
  saveConteneurs(data: InventaireConteneur[]): void {
    setStorageItem(KEYS.INVENTAIRE_CONTENEURS, data);
  },
  getCategories(): InventaireCategorie[] {
    const categories = getStorageItem(KEYS.INVENTAIRE_CATEGORIES, SEED_INVENTAIRE_CATEGORIES);
    const normalized = categories.map(normalizeInventaireCategorie);
    if (JSON.stringify(normalized) !== JSON.stringify(categories)) {
      setStorageItem(KEYS.INVENTAIRE_CATEGORIES, normalized);
    }
    return normalized.filter((item) => item.categorieInventaire !== 'FourHorsBilan');
  },
  saveCategories(data: InventaireCategorie[]): void {
    setStorageItem(KEYS.INVENTAIRE_CATEGORIES, data);
  },
  getFours(): InventaireFour[] {
    const current = getStorageItem(KEYS.INVENTAIRE_FOURS, SEED_INVENTAIRE_FOURS).map(normalizeInventaireFour);
    const legacy = getStorageItem(KEYS.INVENTAIRE_CATEGORIES, SEED_INVENTAIRE_CATEGORIES)
      .map(normalizeInventaireCategorie)
      .filter((item) => item.categorieInventaire === 'FourHorsBilan')
      .map((item) =>
        normalizeInventaireFour({
          id: item.id,
          dateInventaire: `${item.annee}-${String(item.mois).padStart(2, '0')}-01`,
          mois: item.mois,
          annee: item.annee,
          four: (FOUR_STORAGE_NAMES.includes(item.categorie) ? item.categorie : 'Autre') as InventaireFour['four'],
          codeTypeMatiere: item.codeTypeMatiere,
          quantite: item.quantiteReelle,
          unite: item.unite,
          observation: item.commentaire,
          statut: item.statut === 'Cloture' ? 'Valide' : item.statut === 'AValider' ? 'Brouillon' : (item.statut as InventaireFour['statut']),
          inclusDansBilanFonderie: false,
          visibleDansRapportInventaire: true
        })
      );
    return [...legacy, ...current.filter((item) => !legacy.some((legacyItem) => legacyItem.id === item.id))];
  },
  saveFours(data: InventaireFour[]): void {
    setStorageItem(KEYS.INVENTAIRE_FOURS, data);
  },

  // Bilans
  getBilans(): BilanMatiereReport[] {
    return getStorageItem(KEYS.BILANS, SEED_BILANS);
  },
  saveBilans(data: BilanMatiereReport[]): void {
    setStorageItem(KEYS.BILANS, data);
  },

  // Bilans Usinage
  getBilansUsinage(): BilanUsinage2ParPiece[] {
    return getStorageItem(KEYS.BILANS_USINAGE, SEED_BILANS_USINAGE);
  },
  saveBilansUsinage(data: BilanUsinage2ParPiece[]): void {
    setStorageItem(KEYS.BILANS_USINAGE, data);
  },

  // Bilans UAP2
  getBilansUap2(): BilanUap2ParOF[] {
    return getStorageItem(KEYS.BILANS_UAP2, SEED_BILANS_UAP2);
  },
  saveBilansUap2(data: BilanUap2ParOF[]): void {
    setStorageItem(KEYS.BILANS_UAP2, data);
  },

  // Audit
  getAuditLogs(): AuditLogEntry[] {
    return getStorageItem(KEYS.AUDIT_LOGS, []);
  },
  saveAuditLogs(data: AuditLogEntry[]): void {
    setStorageItem(KEYS.AUDIT_LOGS, data);
  },
  addAuditLog(entry: Omit<AuditLogEntry, 'id'>): AuditLogEntry {
    const current = this.getAuditLogs();
    const newEntry: AuditLogEntry = {
      ...entry,
      id: current.length ? Math.max(...current.map((log) => log.id)) + 1 : 1000
    };
    this.saveAuditLogs([newEntry, ...current]);
    return newEntry;
  },

  // Reset helper
  resetAll(): void {
    localStorage.removeItem(KEYS.SORTIES_X3);
    localStorage.removeItem(KEYS.DECHETS);
    localStorage.removeItem(KEYS.RECEPTIONS);
    localStorage.removeItem(KEYS.INVENTAIRE_CONTENEURS);
    localStorage.removeItem(KEYS.INVENTAIRE_CATEGORIES);
    localStorage.removeItem(KEYS.INVENTAIRE_FOURS);
    localStorage.removeItem(KEYS.BILANS);
    localStorage.removeItem(KEYS.BILANS_USINAGE);
    localStorage.removeItem(KEYS.BILANS_UAP2);
    localStorage.removeItem(KEYS.AUDIT_LOGS);
    window.location.reload();
  }
};
