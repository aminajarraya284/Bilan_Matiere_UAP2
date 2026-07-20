import { BUSINESS_RULES } from '@/config/businessRules';
import type { AlertesBilanAlert, AlertesWeightAlert } from '@/features/alertes/alertes.types';
import type { BilanMatiereReport, BilanUap2ParOF, BilanUsinage2ParPiece } from '@/features/bilan-matiere/bilan.types';
import type {
  DashboardAlert,
  DashboardBilanRow,
  DashboardDataSourceStatus,
  DashboardDataSourceStatusValue,
  DashboardFilterOptions,
  DashboardStats
} from '@/features/dashboard/dashboard.types';
import {
  type ReceptionUsinage2Test,
  type RefGammeFabrication,
  type RefNomenclature,
  type SopalTestDataset,
  type TypeMatiereCode,
  type X3OrdreFabrication
} from '@/mocks/sopalTestDataset';
import { testDataRepository } from '@/services/testDataRepository.service';
import { buildBilanFonderieKey, buildBilanUap2Key, buildBilanUsinage2Key } from '@/types/domain.types';

const round = (value: number, digits = 2): number => Number(value.toFixed(digits));
const isInPeriod = (date: string, periode: string): boolean => date.slice(0, 7) === periode;
const normalizeX3Text = (value: string | undefined | null): string =>
  String(value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toUpperCase();

const resolveDataset = (value?: SopalTestDataset): SopalTestDataset => value ?? testDataRepository.getDataset();

export const findOrdreFabrication = (
  datasetOrNumeroOF: SopalTestDataset | string,
  numeroOF?: string
): X3OrdreFabrication | undefined => {
  const dataset = typeof datasetOrNumeroOF === 'string' ? resolveDataset() : datasetOrNumeroOF;
  const of = typeof datasetOrNumeroOF === 'string' ? datasetOrNumeroOF : numeroOF;
  return dataset.x3OrdresFabrication.find((item) => item.numeroOF === of && item.isActive);
};

export const findArticleByOF = (
  datasetOrNumeroOF: SopalTestDataset | string,
  numeroOF?: string
): X3OrdreFabrication | undefined =>
  typeof datasetOrNumeroOF === 'string'
    ? findOrdreFabrication(datasetOrNumeroOF)
    : findOrdreFabrication(datasetOrNumeroOF, numeroOF);

export const findGammeByArticle = (
  datasetOrArticle: SopalTestDataset | string,
  codeArticle?: string
): RefGammeFabrication | undefined => {
  const dataset = typeof datasetOrArticle === 'string' ? resolveDataset() : datasetOrArticle;
  const article = typeof datasetOrArticle === 'string' ? datasetOrArticle : codeArticle;
  return dataset.refGammeFabrication.find((gamme) => gamme.codeArticle === article && gamme.isActive);
};

export const findNomenclatureMatiereByArticle = (
  datasetOrArticle: SopalTestDataset | string,
  codeArticle?: string
): RefNomenclature | undefined => {
  const dataset = typeof datasetOrArticle === 'string' ? resolveDataset() : datasetOrArticle;
  const article = typeof datasetOrArticle === 'string' ? datasetOrArticle : codeArticle;
  return dataset.refNomenclature.find(
    (nomenclature) =>
      nomenclature.codeArticleParent === article &&
      dataset.refTypesMatiere.some((type) => type.codeTypeMatiere === nomenclature.codeComposant) &&
      nomenclature.isActive
  );
};

export const buildReceptionDefaults = (datasetOrNumeroOF: SopalTestDataset | string, maybeNumeroOF?: string) => {
  const dataset = typeof datasetOrNumeroOF === 'string' ? resolveDataset() : datasetOrNumeroOF;
  const numeroOF = typeof datasetOrNumeroOF === 'string' ? datasetOrNumeroOF : maybeNumeroOF || '';
  if (!/^OF\d{8}$/.test(numeroOF)) {
    throw new Error('Format OF invalide. Utilisez OF suivi de 8 chiffres.');
  }

  const ordre = findOrdreFabrication(dataset, numeroOF);
  if (!ordre) throw new Error('OF introuvable dans les données X3 synchronisées.');

  const gamme = findGammeByArticle(dataset, ordre.codeArticleParent);
  if (!gamme) throw new Error("Gamme de fabrication introuvable pour l'article.");

  const nomenclature = findNomenclatureMatiereByArticle(dataset, ordre.codeArticleParent);
  if (!nomenclature) throw new Error("Nomenclature matière introuvable pour l'article.");

  return {
    numeroOF: ordre.numeroOF,
    codeArticle: ordre.codeArticleParent,
    designationArticle: ordre.designationArticle,
    atelierOrigine: gamme.codeAtelier,
    codeTypeMatiere: nomenclature.codeComposant,
    quantiteLienUtilisee: nomenclature.quantiteLienCumulee,
    uniteQuantiteLien: nomenclature.uniteQuantite,
    poidsTheoriqueUnitaireKg: nomenclature.quantiteLienCumulee,
    gammeFabricationId: gamme.id,
    nomenclatureId: nomenclature.id
  };
};

export const calculateReceptionWeight = (
  datasetOrReception:
    | SopalTestDataset
    | Pick<
        ReceptionUsinage2Test,
        'numeroOF' | 'dateReception' | 'origine' | 'poidsTotalReceptionneKg' | 'nombrePiecesReceptionnees'
      >,
  receptionArg?: Pick<
    ReceptionUsinage2Test,
    'numeroOF' | 'dateReception' | 'origine' | 'poidsTotalReceptionneKg' | 'nombrePiecesReceptionnees'
  >
): Omit<ReceptionUsinage2Test, 'id' | 'source' | 'creePar' | 'creeLe' | 'statut'> => {
  const dataset = 'version' in datasetOrReception ? datasetOrReception : resolveDataset();
  const reception = 'version' in datasetOrReception ? receptionArg : datasetOrReception;
  if (!reception) throw new Error('Données de réception manquantes.');
  if (reception.nombrePiecesReceptionnees <= 0)
    throw new Error('Le nombre de pièces réceptionnées doit être supérieur à 0.');
  if (reception.poidsTotalReceptionneKg <= 0) throw new Error('Le poids total réceptionné doit être supérieur à 0.');

  const defaults = buildReceptionDefaults(dataset, reception.numeroOF);
  const poidsPhysiqueUnitaireKg = round(reception.poidsTotalReceptionneKg / reception.nombrePiecesReceptionnees, 3);
  const poidsTheoriqueTotalKg = round(reception.nombrePiecesReceptionnees * defaults.poidsTheoriqueUnitaireKg, 3);
  const ecartPoidsUnitaireKg = round(poidsPhysiqueUnitaireKg - defaults.poidsTheoriqueUnitaireKg, 3);
  const tauxEcartPoids =
    defaults.poidsTheoriqueUnitaireKg > 0
      ? round((Math.abs(ecartPoidsUnitaireKg) / defaults.poidsTheoriqueUnitaireKg) * 100, 3)
      : 0;
  const seuil = dataset.parametresApplication.seuilAlertePoidsPourcentage;

  return {
    ...defaults,
    dateReception: reception.dateReception,
    origine: reception.origine,
    poidsTotalReceptionneKg: reception.poidsTotalReceptionneKg,
    nombrePiecesReceptionnees: reception.nombrePiecesReceptionnees,
    poidsPhysiqueUnitaireKg,
    poidsTheoriqueTotalKg,
    ecartPoidsUnitaireKg,
    tauxEcartPoids,
    statutAlertePoids: tauxEcartPoids > seuil ? 'Alerte' : 'Conforme',
    seuilAlertePoidsPourcentage: seuil,
    mois: reception.dateReception.slice(0, 7)
  };
};

export const calculateNonReceivedQuantities = (
  dataset: SopalTestDataset,
  periode: string = dataset.periode
): BilanUsinage2ParPiece[] =>
  Array.from(
    dataset.x3RapportsMachine
      .filter((rapport) => isInPeriod(rapport.dateProduction, periode) && rapport.isActive)
      .reduce((groups, rapport) => {
        const key = `${rapport.numeroOF}::${rapport.codeArticle}`;
        const current = groups.get(key);
        if (current) {
          current.quantiteEMR += rapport.quantiteEMR;
          current.quantiteRebutEMR += rapport.quantiteRebutEMR;
        } else {
          groups.set(key, { ...rapport });
        }
        return groups;
      }, new Map<string, SopalTestDataset['x3RapportsMachine'][number]>())
      .values()
  ).map((rapport) => {
    const totalReceptionne = dataset.receptionsUsinage2
      .filter(
        (rec) =>
          rec.numeroOF === rapport.numeroOF &&
          rec.codeArticle === rapport.codeArticle &&
          isInPeriod(rec.dateReception, periode)
      )
      .reduce((sum, rec) => sum + rec.nombrePiecesReceptionnees, 0);
    const quantiteNonReceptionnee = rapport.quantiteEMR - totalReceptionne;
    const quantiteRebutEmrX3Pieces = rapport.quantiteRebutEMR;
    const quantiteEmrNetteX3Pieces = rapport.quantiteEMR - quantiteRebutEmrX3Pieces;
    const nomenclature = findNomenclatureMatiereByArticle(dataset, rapport.codeArticle);
    const poidsNonReceptionneEstimeKg = round(quantiteNonReceptionnee * (nomenclature?.quantiteLienCumulee ?? 0), 3);
    const rebutsUsinage2 = dataset.rebutsUsinage2.filter(
      (rebut) =>
        rebut.numeroOF === rapport.numeroOF &&
        rebut.codeArticle === rapport.codeArticle &&
        rebut.statut === 'Valide' &&
        isInPeriod(rebut.dateDeclaration, periode)
    );
    const rebutUsinage2Pieces = rebutsUsinage2.reduce((sum, rebut) => sum + rebut.nombrePiecesRebut, 0);
    const rebutUsinage2Kg = rebutsUsinage2.reduce((sum, rebut) => sum + rebut.poidsRebutKg, 0);
    const entreeOfPieces = dataset.x3MouvementsStock
      .filter((mouvement) => {
        const transaction = normalizeX3Text(mouvement.typeTransaction);
        const typePiece = normalizeX3Text(mouvement.typePiece);
        return (
          mouvement.numeroOF === rapport.numeroOF &&
          mouvement.isActive &&
          isInPeriod(mouvement.dateImputation, periode) &&
          transaction.includes('ENTREE') &&
          (transaction.includes('OF') || typePiece.includes('OF') || typePiece.includes('DECLARATION'))
        );
      })
      .reduce((sum, mouvement) => sum + mouvement.quantite, 0);
    const statutControle =
      quantiteNonReceptionnee === 0
        ? 'OK'
        : quantiteNonReceptionnee <= dataset.parametresApplication.seuilQuantiteNonReceptionneeAVerifier
          ? 'AVerifier'
          : 'Anomalie';

    return {
      bilanKey: buildBilanUsinage2Key(periode, rapport.numeroOF, rapport.codeArticle),
      numeroOF: rapport.numeroOF,
      codeArticle: rapport.codeArticle,
      designationArticle: rapport.designationArticle,
      codeTypeMatiere: nomenclature?.codeComposant ?? 'M3010L0',
      quantiteEmrPieces: rapport.quantiteEMR,
      quantiteRebutEmrX3Pieces,
      quantiteEmrNetteX3Pieces,
      quantiteReceptionneeU2Pieces: totalReceptionne,
      quantiteNonReceptionneePieces: quantiteNonReceptionnee,
      poidsNonReceptionneEstimeKg,
      rebutUsinage2Pieces,
      rebutUsinage2Kg,
      entreeOfPieces,
      statutControle,
      observation:
        quantiteNonReceptionnee === 0
          ? 'Quantités conformes'
          : 'Quantité issue de l’état EMR X3 non réceptionnée dans l’application',
      periode,
      dateCalcul: new Date().toISOString().split('T')[0]
    };
  });

export const calculateInventorySummary = (dataset: SopalTestDataset, periode: string = dataset.periode) => {
  const types = dataset.refTypesMatiere.map((type) => type.codeTypeMatiere);

  return types.map((codeTypeMatiere) => {
    const partie1 = dataset.inventaireConteneurs
      .filter(
        (item) =>
          item.codeTypeMatiere === codeTypeMatiere &&
          item.statut === 'Valide' &&
          isInPeriod(item.dateInventaire, periode)
      )
      .reduce((sum, item) => sum + item.poidsReelKg, 0);
    const partie2 = dataset.inventaireCategories
      .filter(
        (item) =>
          item.codeTypeMatiere === codeTypeMatiere &&
          item.statut === 'Valide' &&
          item.inclusBilanFonderie &&
          isInPeriod(item.dateInventaire, periode)
      )
      .reduce((sum, item) => sum + item.poidsReelKg, 0);
    const partie3 = dataset.inventaireFours
      .filter(
        (item) =>
          item.codeTypeMatiere === codeTypeMatiere &&
          item.statut === 'Valide' &&
          item.visibleRapportInventaire &&
          isInPeriod(item.dateInventaire, periode)
      )
      .reduce((sum, item) => sum + item.quantite, 0);

    return {
      codeTypeMatiere,
      totalPartie1: partie1,
      totalPartie2: partie2,
      totalPartie3: partie3,
      totalInventaireRapport: partie1 + partie2 + partie3,
      totalUtiliseBilan: partie1 + partie2,
      statut: 'Valide' as const
    };
  });
};

export const calculateInventoryReportSummary = (dataset: SopalTestDataset, periode: string = dataset.periode) =>
  calculateInventorySummary(dataset, periode);

export const calculateInventoryBilanSummary = (dataset: SopalTestDataset, periode: string = dataset.periode) =>
  calculateInventorySummary(dataset, periode).map((row) => ({
    ...row,
    totalPartie3: 0,
    totalInventaireRapport: row.totalUtiliseBilan,
    totalUtiliseBilan: row.totalUtiliseBilan
  }));

export const calculateBilanFonderie = (
  dataset: SopalTestDataset,
  periode: string = dataset.periode
): BilanMatiereReport[] => {
  const nonReceived = calculateNonReceivedQuantities(dataset, periode);

  return dataset.refTypesMatiere.map((type) => {
    const code = type.codeTypeMatiere;
    const encoursDebut =
      dataset.stockDebut.find((stock) => stock.periode === periode && stock.codeTypeMatiere === code)?.stockDebutKg ??
      0;
    const sortiesX3 = dataset.x3ComposantsOF
      .filter((component) => component.codeComposant === code && component.isActive)
      .reduce((sum, component) => sum + component.quantiteConsommee, 0);
    const inputTotal = encoursDebut + sortiesX3;
    const inventaire = calculateInventorySummary(dataset, periode).find((row) => row.codeTypeMatiere === code);
    const encoursFinReel = inventaire?.totalUtiliseBilan ?? 0;
    const dechetsFonderie = dataset.dechetsFonderie
      .filter(
        (dechet) =>
          dechet.codeTypeMatiere === code && dechet.statut === 'Valide' && isInPeriod(dechet.dateDeclaration, periode)
      )
      .reduce((sum, dechet) => sum + dechet.poidsDechetKg, 0);
    const receptionsUsinage2 = dataset.receptionsUsinage2
      .filter((rec) => rec.codeTypeMatiere === code && isInPeriod(rec.dateReception, periode))
      .reduce((sum, rec) => sum + rec.poidsTotalReceptionneKg, 0);
    const poidsNonReceptionne = dataset.parametresApplication.poidsNonReceptionneInclusInventaireFin
      ? 0
      : nonReceived
          .filter((item) => item.codeTypeMatiere === code)
          .reduce((sum, item) => sum + item.poidsNonReceptionneEstimeKg, 0);
    const nombrePiecesNonReceptionnees = nonReceived
      .filter((item) => item.codeTypeMatiere === code)
      .reduce((sum, item) => sum + item.quantiteNonReceptionneePieces, 0);
    const outputTotal = encoursFinReel + dechetsFonderie + receptionsUsinage2 + poidsNonReceptionne;
    const ecartKg = round(inputTotal - outputTotal, 3);
    const tauxEcart = inputTotal > 0 ? round((Math.abs(ecartKg) / inputTotal) * 100, 3) : 0;

    return {
      bilanKey: buildBilanFonderieKey(periode, code),
      periode,
      typeMatiere: code,
      encoursDebut,
      sortiesX3,
      inputTotal,
      encoursFinReel,
      dechetsFonderie,
      receptionsUsinage2,
      poidsNonReceptionne,
      nombrePiecesNonReceptionnees,
      outputTotal,
      ecartKg,
      tauxEcart,
      statut: tauxEcart > dataset.parametresApplication.seuilAlerteBilanPourcentage ? 'AvecEcart' : 'Valide',
      justification:
        tauxEcart > dataset.parametresApplication.seuilAlerteBilanPourcentage
          ? 'Écart matière à justifier.'
          : 'Bilan équilibré.',
      dateCalcul: new Date().toISOString().split('T')[0]
    };
  });
};

export const calculateBilanUsinage2 = calculateNonReceivedQuantities;

export const calculateBilanUap2 = (dataset: SopalTestDataset, periode: string = dataset.periode): BilanUap2ParOF[] =>
  dataset.x3OrdresFabrication
    .filter((ordre) => ordre.isActive)
    .map((ordre) => {
      const rapport = dataset.x3RapportsMachine.find(
        (item) => item.numeroOF === ordre.numeroOF && isInPeriod(item.dateProduction, periode)
      );
      const composant = dataset.x3ComposantsOF.find((item) => item.numeroOF === ordre.numeroOF);
      const receptionPoids = dataset.receptionsUsinage2
        .filter((item) => item.numeroOF === ordre.numeroOF && isInPeriod(item.dateReception, periode))
        .reduce((sum, item) => sum + item.poidsTotalReceptionneKg, 0);
      const receptionPieces = dataset.receptionsUsinage2
        .filter((item) => item.numeroOF === ordre.numeroOF && isInPeriod(item.dateReception, periode))
        .reduce((sum, item) => sum + item.nombrePiecesReceptionnees, 0);
      const reception = dataset.receptionsUsinage2.find((item) => item.numeroOF === ordre.numeroOF);
      const poidsPhy = reception?.poidsPhysiqueUnitaireKg ?? 0;
      const qteCtrl = rapport?.quantiteEMR ?? 0;
      const poidsControle = round(qteCtrl * poidsPhy, 3);
      const quantiteSortieMP = composant?.quantiteConsommee ?? 0;
      const saisiesRebutsUap2 = (dataset.rebutsUap2 ?? []).filter(
        (item) =>
          item.numeroOF === ordre.numeroOF && item.statut === 'Valide' && isInPeriod(item.dateDeclaration, periode)
      );
      const rebutEMR = saisiesRebutsUap2.length
        ? saisiesRebutsUap2.reduce((sum, item) => sum + item.rebutEmrKg, 0)
        : round((rapport?.quantiteRebutEMR ?? 0) * poidsPhy, 3);
      const rebutSCI = saisiesRebutsUap2.reduce((sum, item) => sum + item.rebutSciKg, 0);
      const rebutTRN = saisiesRebutsUap2.reduce((sum, item) => sum + item.rebutTrnKg, 0);
      const totalRebutEmrSciTrn = rebutEMR + rebutSCI + rebutTRN;
      const rebutsUsinage2 = dataset.rebutsUsinage2.filter(
        (item) => item.numeroOF === ordre.numeroOF && item.statut === 'Valide' && isInPeriod(item.dateDeclaration, periode)
      );
      const rebutUsinage2Pieces = rebutsUsinage2.reduce((sum, item) => sum + item.nombrePiecesRebut, 0);
      const rebutUsinage2 = rebutsUsinage2.reduce((sum, item) => sum + item.poidsRebutKg, 0);
      const entreeSF = dataset.x3MouvementsStock
        .filter(
          (item) =>
            item.numeroOF === ordre.numeroOF &&
            item.typeTransaction === 'Entrée OF' &&
            item.typePiece === 'Déclaration production' &&
            item.isActive &&
            isInPeriod(item.dateImputation, periode)
        )
        .reduce((sum, item) => sum + item.quantite, 0);
      const entreeSFNet = dataset.x3MouvementsStock
        .filter((item) => item.numeroOF === ordre.numeroOF && item.isActive && isInPeriod(item.dateImputation, periode))
        .reduce((sum, item) => sum + item.quantite, 0);
      const quantiteNonReceptionnee = Math.max(0, qteCtrl - receptionPieces);
      const ecartBilanOF = round(
        poidsControle - quantiteSortieMP - totalRebutEmrSciTrn + receptionPoids - (rebutUsinage2 + entreeSFNet),
        3
      );
      const tauxEcartOF = quantiteSortieMP > 0 ? round((Math.abs(ecartBilanOF) / quantiteSortieMP) * 100, 3) : 0;

      return {
        bilanKey: buildBilanUap2Key(periode, ordre.numeroOF),
        numeroOF: ordre.numeroOF,
        codeArticle: ordre.codeArticleParent,
        designationArticle: ordre.designationArticle,
        codeTypeMatiere: composant?.codeComposant ?? reception?.codeTypeMatiere ?? 'M3010L0',
        quantiteSortieMPKg: quantiteSortieMP,
        quantiteControlePieces: qteCtrl,
        poidsPhysiqueUnitaireKg: poidsPhy,
        poidsControleKg: poidsControle,
        rebutEmrKg: rebutEMR,
        rebutSciKg: rebutSCI,
        rebutTrnKg: rebutTRN,
        totalRebutEmrSciTrnKg: totalRebutEmrSciTrn,
        quantiteReceptionneeU2Kg: receptionPoids,
        quantiteNonReceptionneePieces: quantiteNonReceptionnee,
        rebutUsinage2Pieces,
        rebutUsinage2Kg: rebutUsinage2,
        entreeSfNetKg: entreeSFNet,
        outputOFKg: round(totalRebutEmrSciTrn + receptionPoids + rebutUsinage2 + entreeSF, 3),
        ecartBilanOFKg: ecartBilanOF,
        tauxEcartOF,
        statut: tauxEcartOF > BUSINESS_RULES.seuilEcartBilanAutoPourcentage ? 'AVerifier' : 'OK',
        observation:
          tauxEcartOF > BUSINESS_RULES.seuilEcartBilanAutoPourcentage ? 'Écart OF à justifier' : 'Bilan OF équilibré',
        periode,
        dateCalcul: new Date().toISOString().split('T')[0]
      };
    });

export const calculateDashboard = (dataset: SopalTestDataset, periode: string = dataset.periode) => {
  const bilans = calculateBilanFonderie(dataset, periode);
  const inputTotal = bilans.reduce((sum, item) => sum + item.inputTotal, 0);
  const outputTotal = bilans.reduce((sum, item) => sum + item.outputTotal, 0);
  const stockDebut = bilans.reduce((sum, item) => sum + item.encoursDebut, 0);
  const sortiesMagasinX3 = bilans.reduce((sum, item) => sum + item.sortiesX3, 0);
  const ecart = round(inputTotal - outputTotal, 3);
  const taux = inputTotal > 0 ? round((Math.abs(ecart) / inputTotal) * 100, 3) : 0;
  const weightAlerts = getActiveAlerts(dataset, periode).weightAlerts;
  const bilanAlerts = bilans.filter(
    (bilan) => bilan.tauxEcart > dataset.parametresApplication.seuilAlerteBilanPourcentage
  );
  const stats: DashboardStats = {
    inputMatiere: inputTotal,
    outputMatiere: outputTotal,
    stockDebut,
    sortiesMagasinX3,
    ecartMatiere: ecart,
    tauxEcart: taux,
    periodeLabel: periode,
    dechetsFonderie: bilans.reduce((sum, item) => sum + item.dechetsFonderie, 0),
    encoursFinUtiliseBilan: bilans.reduce((sum, item) => sum + item.encoursFinReel, 0),
    receptionsU2: bilans.reduce((sum, item) => sum + item.receptionsUsinage2, 0),
    alertesPoids: weightAlerts.length,
    alertesBilan: bilanAlerts.length
  };
  const rows: DashboardBilanRow[] = bilans.map((bilan) => ({
    typeMatiere: bilan.typeMatiere,
    inputX3: bilan.inputTotal,
    stockDebut: bilan.encoursDebut,
    sortiesX3: bilan.sortiesX3,
    encoursFin: bilan.encoursFinReel,
    dechets: bilan.dechetsFonderie,
    usinage2: bilan.receptionsUsinage2,
    poidsNonReceptionne: bilan.poidsNonReceptionne,
    ecartKg: bilan.ecartKg,
    statut: bilan.statut
  }));
  const alerts: DashboardAlert[] = [
    ...bilanAlerts
      .map((bilan, index): DashboardAlert => ({
        id: index + 1,
        type: 'danger' as const,
        category: 'bilan',
        actionLabel: 'Consulter le bilan',
        actionTo: '/bilan-matiere',
        typeMatiere: bilan.typeMatiere,
        ecartKg: bilan.ecartKg,
        tauxEcart: bilan.tauxEcart,
        seuil: dataset.parametresApplication.seuilAlerteBilanPourcentage,
        statut: bilan.statut,
        title: `Alerte Écart Critique : ${bilan.typeMatiere}`,
        description: `${bilan.typeMatiere} présente un écart de ${bilan.ecartKg.toLocaleString('fr-FR')} kg (${bilan.tauxEcart.toLocaleString('fr-FR')}%).`
      }))
      .concat(
        weightAlerts.map((alert) => {
          const reception = dataset.receptionsUsinage2.find((item) => item.id === alert.id);
          return {
          id: 1000 + alert.id,
          type: 'warning' as const,
          category: 'weight',
          actionLabel: 'Consulter',
          actionTo: '/receptions-usinage-2',
          numeroOF: alert.numeroOF,
          codeArticle: alert.codeArticle,
          typeMatiere: alert.codeTypeMatiere,
          poidsTheoriqueUnitaireKg: reception?.poidsTheoriqueUnitaireKg,
          poidsPhysiqueUnitaireKg: reception?.poidsPhysiqueUnitaireKg,
          tauxEcart: reception?.tauxEcartPoids,
          seuil: dataset.parametresApplication.seuilAlertePoidsPourcentage,
          statut: 'Alerte',
          title: `Alerte poids : ${alert.numeroOF}`,
          description: `${alert.codeArticle} dépasse le seuil de ${dataset.parametresApplication.seuilAlertePoidsPourcentage} %.`
        };
        })
      )
  ];

  return {
    stats,
    rows,
    alerts,
    sources: buildDashboardSources(dataset),
    filterOptions: buildDashboardFilterOptions(dataset)
  };
};

export const getActiveAlerts = (dataset: SopalTestDataset, periode: string = dataset.periode) => {
  const weightAlerts: AlertesWeightAlert[] = dataset.receptionsUsinage2
    .filter((rec) => isInPeriod(rec.dateReception, periode) && rec.tauxEcartPoids > rec.seuilAlertePoidsPourcentage)
    .map((rec) => ({
      id: rec.id,
      numeroOF: rec.numeroOF,
      dateReception: rec.dateReception,
      codeArticle: rec.codeArticle,
      designationArticle: rec.designationArticle,
      codeTypeMatiere: rec.codeTypeMatiere,
      poidsTheoriqueTotalKg: rec.poidsTheoriqueTotalKg,
      poidsTotalReceptionneKg: rec.poidsTotalReceptionneKg,
      poidsKg: rec.poidsTheoriqueTotalKg,
      poidsPhysique: rec.poidsTotalReceptionneKg,
      poidsTheoriqueUnitaireKg: rec.poidsTheoriqueUnitaireKg,
      poidsPhysiqueUnitaireKg: rec.poidsPhysiqueUnitaireKg,
      ecartPoidsUnitaireKg: round(rec.poidsPhysiqueUnitaireKg - rec.poidsTheoriqueUnitaireKg, 3),
      tauxEcartPoids: rec.tauxEcartPoids,
      difference: round(rec.poidsTotalReceptionneKg - rec.poidsTheoriqueTotalKg, 3),
      atelierOrigine: rec.atelierOrigine,
      mois: rec.mois,
      source: rec.source
    }));
  const bilanAlerts: AlertesBilanAlert[] = calculateBilanFonderie(dataset, periode)
    .filter((bilan) => bilan.tauxEcart > dataset.parametresApplication.seuilAlerteBilanPourcentage)
    .map((bilan, index) => ({
      id: index + 1,
      typeMatiere: bilan.typeMatiere,
      inputTotal: bilan.inputTotal,
      outputTotal: bilan.outputTotal,
      ecartKg: bilan.ecartKg,
      tauxEcart: bilan.tauxEcart,
      statut: bilan.statut,
      justification: bilan.justification
    }));

  return { weightAlerts, bilanAlerts };
};

const latestDate = (values: Array<string | undefined | null>): string | undefined =>
  values.filter((value): value is string => Boolean(value)).sort().at(-1);

const resolveSourceStatus = (updatedAt: string | undefined, rowCount: number): DashboardDataSourceStatusValue => {
  if (!updatedAt || rowCount <= 0) return 'Non disponible';
  const date = new Date(updatedAt);
  if (Number.isNaN(date.getTime())) return 'Non disponible';
  const ageInDays = Math.abs(Date.now() - date.getTime()) / 86_400_000;
  return ageInDays > 45 ? 'Ancien' : 'À jour';
};

const buildDashboardSources = (dataset: SopalTestDataset): DashboardDataSourceStatus[] => {
  const batchByType = (typeImport: string) =>
    dataset.importBatches.find((batch) => batch.typeImport === typeImport && batch.statut !== 'Erreur');

  const makeSource = (
    id: string,
    group: DashboardDataSourceStatus['group'],
    name: string,
    updatedAt: string | undefined,
    rowCount: number
  ): DashboardDataSourceStatus => ({
    id,
    group,
    name,
    updatedAt,
    rowCount,
    status: resolveSourceStatus(updatedAt, rowCount)
  });

  const inventaireRows =
    dataset.inventaireConteneurs.length + dataset.inventaireCategories.length + dataset.inventaireFours.length;

  return [
    makeSource(
      'x3-consommations-of',
      'X3',
      'Mouvements VCR + encours production',
      batchByType('MVTSTOCKSVCR')?.dateFin,
      dataset.x3ComposantsOF.length
    ),
    makeSource(
      'x3-gamme',
      'X3',
      'Gamme de fabrication',
      batchByType('GAMMEFAB')?.dateFin,
      dataset.refGammeFabrication.length
    ),
    makeSource(
      'x3-nomenclature',
      'X3',
      'Nomenclature',
      batchByType('BOMARTPARENT')?.dateFin,
      dataset.refNomenclature.length
    ),
    makeSource(
      'x3-emr',
      'X3',
      'Rapport EMR',
      batchByType('YRJMCH')?.dateFin,
      dataset.x3RapportsMachine.length
    ),
    makeSource(
      'app-inventaire',
      'Application',
      'Inventaire Fonderie',
      latestDate([
        ...dataset.inventaireConteneurs.map((item) => item.dateInventaire),
        ...dataset.inventaireCategories.map((item) => item.dateInventaire),
        ...dataset.inventaireFours.map((item) => item.dateInventaire)
      ]),
      inventaireRows
    ),
    makeSource(
      'app-dechets',
      'Application',
      'Déchets Fonderie',
      latestDate(dataset.dechetsFonderie.map((item) => item.dateDeclaration)),
      dataset.dechetsFonderie.length
    ),
    makeSource(
      'app-receptions-u2',
      'Application',
      'Réceptions Usinage 2',
      latestDate(dataset.receptionsUsinage2.map((item) => item.dateReception)),
      dataset.receptionsUsinage2.length
    )
  ];
};

const buildDashboardFilterOptions = (dataset: SopalTestDataset): DashboardFilterOptions => {
  const ofTypeMatieres = dataset.x3ComposantsOF.reduce<Record<string, string[]>>((acc, component) => {
    if (!component.isActive) return acc;
    const current = acc[component.numeroOF] ?? [];
    if (!current.includes(component.codeComposant)) current.push(component.codeComposant);
    acc[component.numeroOF] = current.sort();
    return acc;
  }, {});

  return {
    typeMatieres: dataset.refTypesMatiere.map((item) => item.codeTypeMatiere),
    numerosOF: Array.from(new Set(dataset.x3OrdresFabrication.map((item) => item.numeroOF))).sort(),
    ofTypeMatieres
  };
};

export const toTypeMatiereId = (code: TypeMatiereCode | string): number => {
  if (code === 'M3000L0') return 1;
  if (code === 'M3010L0') return 2;
  if (code === 'M3060L0') return 3;
  return 0;
};
