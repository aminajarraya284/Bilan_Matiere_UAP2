import type { AlertesBilanAlert } from '@/features/alertes/alertes.legacy-mock.types';
import type { BilanMatiereReport, BilanUap2ParOF, BilanUsinage2ParPiece } from '@/features/bilan-matiere/bilan.types';
import type { RapportsListType } from '@/features/rapports/rapports.types';
import type { ReceptionUsinage2Mouvement } from '@/features/receptions-usinage2/receptions.types';
import type { SortieX3Mouvement } from '@/features/sorties-x3/sortiesX3.types';
import { sopalTestDataset, type TypeMatiereCode } from '@/mocks/sopalTestDataset';
import {
  calculateBilanFonderie,
  calculateBilanUap2,
  calculateBilanUsinage2,
  getActiveAlerts
} from '@/services/bilanCalculation.service';

export type TypeMatiere = TypeMatiereCode;
export type CategorieArticle = 'ProduitFini' | 'SemiFini' | 'Composant';

export interface TestArticle {
  id: number;
  codeArticle: string;
  designation: string;
  typeMatiere: TypeMatiere;
  categorieArticle: CategorieArticle;
  poidsUnitaireKg: number;
  unite: 'KG' | 'PC';
  actif: boolean;
}

export interface TestOrdreFabrication {
  id: number;
  numeroOF: string;
  codeArticle: string;
  typeMatiere: TypeMatiere;
  quantiteLancee: number;
  quantiteRealisee: number;
  dateDebut: string;
  dateFinPrevue: string;
  atelier: 'Fonderie' | 'Usinage 2' | 'UAP2';
  statut: 'Lance' | 'EnCours' | 'Cloture';
}

export const testArticles: TestArticle[] = sopalTestDataset.refNomenclature.map((nomenclature, index) => ({
  id: index + 1,
  codeArticle: nomenclature.codeArticleParent,
  designation: nomenclature.designationArticleParent,
  typeMatiere: nomenclature.codeComposant,
  categorieArticle: 'Composant',
  poidsUnitaireKg: nomenclature.quantiteLienCumulee,
  unite: 'PC',
  actif: nomenclature.isActive
}));

export const testOrdresFabrication: TestOrdreFabrication[] = sopalTestDataset.x3OrdresFabrication.map((of, index) => {
  const operation = sopalTestDataset.x3OperationsOF.find((item) => item.numeroOF === of.numeroOF);
  const nomenclature = sopalTestDataset.refNomenclature.find((item) => item.codeArticleParent === of.codeArticleParent);
  return {
    id: index + 1,
    numeroOF: of.numeroOF,
    codeArticle: of.codeArticleParent,
    typeMatiere: nomenclature?.codeComposant ?? 'M3010L0',
    quantiteLancee: operation?.quantitePrevue ?? 0,
    quantiteRealisee: operation?.quantiteRealisee ?? 0,
    dateDebut: '2026-06-01',
    dateFinPrevue: of.dateCloture,
    atelier: 'UAP2',
    statut: 'Cloture'
  };
});

export const testSortiesMagasin: SortieX3Mouvement[] = sopalTestDataset.x3OrdresFabrication.map((of, index) => ({
  id: index + 1,
  dateMouvement:
    sopalTestDataset.x3MouvementsStock.find((mouvement) => mouvement.numeroOF === of.numeroOF)?.dateImputation ?? '',
  numeroOF: of.numeroOF,
  codeArticle: of.codeArticleParent,
  designationArticle: of.designationArticle,
  codeTypeMatiere:
    sopalTestDataset.x3MouvementsStock.find((mouvement) => mouvement.numeroOF === of.numeroOF)?.codeArticle ?? '',
  sortieMP: sopalTestDataset.x3MouvementsStock
    .filter((mouvement) => mouvement.numeroOF === of.numeroOF)
    .reduce((total, mouvement) => total + mouvement.quantite, 0)
}));

export const testReceptionsUsinage2: ReceptionUsinage2Mouvement[] = sopalTestDataset.receptionsUsinage2.map((rec) => ({
  id: rec.id,
  numeroOF: rec.numeroOF,
  dateReception: rec.dateReception,
  codeArticle: rec.codeArticle,
  designationArticle: rec.designationArticle,
  origine: rec.origine,
  poidsTotalReceptionneKg: rec.poidsTotalReceptionneKg,
  nombrePiecesReceptionnees: rec.nombrePiecesReceptionnees,
  atelierOrigine: rec.atelierOrigine,
  codeTypeMatiere: rec.codeTypeMatiere,
  designationTypeMatiere: `Matiere ${rec.codeTypeMatiere}`,
  mois: rec.mois,
  poidsPhysiqueUnitaireKg: rec.poidsPhysiqueUnitaireKg,
  quantiteLienUtilisee: rec.quantiteLienUtilisee,
  uniteQuantiteLien: rec.uniteQuantiteLien,
  poidsTheoriqueUnitaireKg: rec.poidsTheoriqueUnitaireKg,
  poidsTheoriqueTotalKg: rec.poidsTheoriqueTotalKg,
  ecartPoidsUnitaireKg: rec.ecartPoidsUnitaireKg,
  tauxEcartPoids: rec.tauxEcartPoids,
  statutAlertePoids: rec.statutAlertePoids,
  seuilAlertePoidsPourcentage: rec.seuilAlertePoidsPourcentage,
  gammeFabricationId: rec.gammeFabricationId,
  nomenclatureId: rec.nomenclatureId,
  statut: rec.statut,
  source: rec.source,
  utilisateur: rec.creePar,
  creeLe: rec.creeLe
}));

const activeAlerts = getActiveAlerts(sopalTestDataset);

export const testBilans: {
  fonderie: BilanMatiereReport[];
  usinage2: BilanUsinage2ParPiece[];
  uap2: BilanUap2ParOF[];
} = {
  fonderie: calculateBilanFonderie(sopalTestDataset),
  usinage2: calculateBilanUsinage2(sopalTestDataset),
  uap2: calculateBilanUap2(sopalTestDataset)
};

export const testAlertes: AlertesBilanAlert[] = activeAlerts.bilanAlerts;

export const testRapports: RapportsListType[] = [
  {
    id: 1,
    titre: 'Rapport Bilan Fonderie',
    description: 'Entrées, sorties et écarts par type matière.',
    module: 'Bilan',
    dateGeneration: '2026-06-30',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 2,
    titre: 'Rapport Bilan Usinage 2',
    description: 'Contrôle EMR et réceptions U2 par OF.',
    module: 'Bilan',
    dateGeneration: '2026-06-30',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 3,
    titre: 'Rapport Bilan UAP2',
    description: 'Bilan par OF avec entrées semi-finies.',
    module: 'Bilan',
    dateGeneration: '2026-06-30',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 4,
    titre: 'Rapport Inventaire ESR',
    description: 'Conteneurs, catégories et fours hors bilan.',
    module: 'Inventaire',
    dateGeneration: '2026-06-30',
    formatDisponible: ['PDF', 'Excel']
  },
  {
    id: 5,
    titre: 'Rapport Alertes',
    description: 'Alertes poids et alertes bilan.',
    module: 'Alertes',
    dateGeneration: '2026-06-30',
    formatDisponible: ['PDF']
  }
];
