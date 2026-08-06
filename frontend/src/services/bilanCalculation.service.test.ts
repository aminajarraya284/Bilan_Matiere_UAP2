import { describe, expect, it } from 'vitest';
// Imports "?raw" (geres par Vite) plutot que node:fs : en environnement jsdom,
// node:fs est externalise comme un stub navigateur sans implementation, et cet
// environnement doit rester jsdom pour que testDataRepository (localStorage) fonctionne.
import datasetSourceRaw from '@/mocks/sopalTestDataset.ts?raw';
import calculationSourceRaw from './bilanCalculation.service.ts?raw';
import testDataSourceRaw from '@/data/testData.ts?raw';
import receptionsServiceSourceRaw from '@/features/receptions-usinage2/receptions.service.ts?raw';
import receptionsUsinage2ViewSourceRaw from '@/features/receptions-usinage2/ReceptionsUsinage2View.vue?raw';
import alertesViewSourceRaw from '@/features/alertes/AlertesView.vue?raw';
import exporterSourceRaw from '@/features/rapports/exporter.ts?raw';
import x3ImporterSourceRaw from '@/features/imports/X3Importer.vue?raw';
import dashboardViewSourceRaw from '@/features/dashboard/DashboardView.vue?raw';
import bilanMatiereViewSourceRaw from '@/features/bilan-matiere/BilanMatiereView.vue?raw';
import { sopalTestDataset, type SopalTestDataset } from '@/mocks/sopalTestDataset';
import {
  buildReceptionDefaults,
  calculateBilanFonderie,
  calculateBilanUap2,
  calculateInventorySummary,
  calculateNonReceivedQuantities,
  calculateReceptionWeight,
  findArticleByOF,
  findGammeByArticle,
  findNomenclatureMatiereByArticle
} from './bilanCalculation.service';
import { testDataRepository } from './testDataRepository.service';

const datasetSource = datasetSourceRaw;
const calculationSource = calculationSourceRaw;

const existingOF = new Set(sopalTestDataset.x3OrdresFabrication.map((item) => item.numeroOF));
const existingArticles = new Set(sopalTestDataset.x3OrdresFabrication.map((item) => item.codeArticleParent));
const existingMaterials = new Set(sopalTestDataset.refTypesMatiere.map((item) => item.codeTypeMatiere));

const getGamme = (codeArticle: string, numeroOperation?: string) =>
  sopalTestDataset.refGammeFabrication.find(
    (item) => item.codeArticle === codeArticle && (!numeroOperation || item.numeroOperation === numeroOperation)
  );

const expectNoOrphans = (dataset: SopalTestDataset) => {
  dataset.x3OrdresFabrication.forEach((of) => {
    expect(getGamme(of.codeArticleParent)).toBeTruthy();
    expect(findNomenclatureMatiereByArticle(dataset, of.codeArticleParent)).toBeTruthy();
  });

  dataset.refNomenclature.forEach((row) => {
    expect(existingArticles.has(row.codeArticleParent)).toBe(true);
    expect(existingMaterials.has(row.codeComposant)).toBe(true);
  });

  dataset.x3OperationsOF.forEach((operation) => {
    const of = dataset.x3OrdresFabrication.find((item) => item.numeroOF === operation.numeroOF);
    expect(of).toBeTruthy();
    expect(getGamme(of?.codeArticleParent ?? '', operation.numeroOperation)).toBeTruthy();
  });

  dataset.x3ComposantsOF.forEach((component) => {
    expect(existingOF.has(component.numeroOF)).toBe(true);
    expect(existingMaterials.has(component.codeComposant)).toBe(true);
  });

  dataset.x3RapportsMachine.forEach((rapport) => {
    expect(existingOF.has(rapport.numeroOF)).toBe(true);
    expect(existingArticles.has(rapport.codeArticle)).toBe(true);
    expect(getGamme(rapport.codeArticle, rapport.numeroOperation)).toBeTruthy();
  });

  dataset.x3MouvementsStock.forEach((mouvement) => {
    expect(existingOF.has(mouvement.numeroOF)).toBe(true);
    expect(existingMaterials.has(mouvement.codeArticle)).toBe(true);
  });

  dataset.receptionsUsinage2.forEach((reception) => {
    const defaults = buildReceptionDefaults(dataset, reception.numeroOF);
    expect(reception.codeArticle).toBe(defaults.codeArticle);
    expect(reception.atelierOrigine).toBe(defaults.atelierOrigine);
    expect(reception.codeTypeMatiere).toBe(defaults.codeTypeMatiere);
  });
};

describe('SOPAL coherent test dataset calculations', () => {
  it('uses exactly the three requested material types', () => {
    expect(sopalTestDataset.refTypesMatiere).toEqual([
      { id: 1, codeTypeMatiere: 'M3000L0', libelle: 'Matiere M3000L0', unite: 'KG', actif: true },
      { id: 2, codeTypeMatiere: 'M3010L0', libelle: 'Matiere M3010L0', unite: 'KG', actif: true },
      { id: 3, codeTypeMatiere: 'M3060L0', libelle: 'Matiere M3060L0', unite: 'KG', actif: true }
    ]);
  });

  it('returns article 0101A03-1 for OF18100563', () => {
    expect(findArticleByOF(sopalTestDataset, 'OF18100563')?.codeArticleParent).toBe('0101A03-1');
  });

  it('returns atelier 19 for 0101A03-1', () => {
    expect(findGammeByArticle(sopalTestDataset, '0101A03-1')?.codeAtelier).toBe('19');
  });

  it('returns material M3010L0 and link quantity 1.500 for 0101A03-1', () => {
    const nomenclature = findNomenclatureMatiereByArticle(sopalTestDataset, '0101A03-1');
    expect(nomenclature?.codeComposant).toBe('M3010L0');
    expect(nomenclature?.quantiteLienCumulee).toBe(1.5);
  });

  it('keeps YListOFFerm column C as numeroOF and column L as quantiteConsommee', () => {
    const of = sopalTestDataset.x3OrdresFabrication.find((item) => item.numeroOF === 'OF18100563');
    const component = sopalTestDataset.x3ComposantsOF.find((item) => item.numeroOF === 'OF18100563');

    expect(of?.numeroOF).toBe('OF18100563');
    expect(component?.quantiteConsommee).toBe(1425);
    expect(component).not.toHaveProperty('unite');
  });

  it('keeps GAMMEFAB column A as codeArticle and column F as codeAtelier', () => {
    const gamme = sopalTestDataset.refGammeFabrication.find((item) => item.codeArticle === '0101A03-1');
    expect(gamme?.codeArticle).toBe('0101A03-1');
    expect(gamme?.codeAtelier).toBe('19');
    expect(gamme?.codePoste).toBe('POSTE-19-01');
  });

  it('keeps BOMARTPARENT columns H, I and J aligned without artificial shift', () => {
    const nomenclature = findNomenclatureMatiereByArticle(sopalTestDataset, '0101A03-1');
    expect(nomenclature?.quantiteLienCumulee).toBe(1.5);
    expect(nomenclature?.uniteQuantite).toBe('KG');
    expect(nomenclature?.categorieComposant).toBe('MATIERE');
    expect(nomenclature).not.toHaveProperty('alternativeNomenclature');
    expect(nomenclature).not.toHaveProperty('uniteGestion');
  });

  it('treats YRJMCH EMR quantities as pieces, including scrap pieces', () => {
    const rows = calculateNonReceivedQuantities(sopalTestDataset);
    const of18100563 = rows.find((row) => row.numeroOF === 'OF18100563');
    const rawReports = sopalTestDataset.x3RapportsMachine.filter((item) => item.numeroOF === 'OF18100563');

    expect(rawReports.reduce((sum, item) => sum + item.quantiteEMR, 0)).toBe(1000);
    expect(rawReports.reduce((sum, item) => sum + item.quantiteRebutEMR, 0)).toBe(20);
    expect(of18100563?.quantiteEmrPieces).toBe(1000);
    expect(of18100563?.quantiteRebutEmrX3Pieces).toBe(20);
    expect(of18100563?.quantiteEmrNetteX3Pieces).toBe(980);
    expect(of18100563?.quantiteNonReceptionneePieces).toBe(30);
  });

  it('keeps MVTSTOCKSVCR column C as designationArticle and column M as referenceOrigineX3', () => {
    const movement = sopalTestDataset.x3MouvementsStock.find((item) => item.referenceOrigineX3 === '23531');
    expect(movement?.designationArticle).toBe('Matiere M3010L0');
    expect(movement?.referenceOrigineX3).toBe('23531');
    expect(sopalTestDataset.x3MouvementsStock.some((item) => item.designationArticle === '23531')).toBe(false);
  });

  it('preserves negative stock movements and totals 1600 + (-40) = 1560', () => {
    const total = sopalTestDataset.x3MouvementsStock
      .filter((item) => item.numeroOF === 'OF18100563')
      .reduce((sum, item) => sum + item.quantite, 0);

    expect(total).toBe(1560);
    expect(sopalTestDataset.x3MouvementsStock.some((item) => item.quantite < 0)).toBe(true);
    expect(calculationSource).not.toContain('Math.abs(item.quantite');
    expect(calculationSource).not.toContain('Math.abs(mouvement.quantite');
  });

  it('builds reception defaults from OF, gamme and nomenclature', () => {
    expect(buildReceptionDefaults(sopalTestDataset, 'OF18100563')).toMatchObject({
      codeArticle: '0101A03-1',
      atelierOrigine: '19',
      codeTypeMatiere: 'M3010L0',
      poidsTheoriqueUnitaireKg: 1.5
    });
  });

  it('calculates a conforming reception at or below the 3% threshold', () => {
    const reception = calculateReceptionWeight(sopalTestDataset, {
      numeroOF: 'OF18100563',
      dateReception: '2026-06-10',
      origine: 'FONDERIE',
      poidsTotalReceptionneKg: 1455,
      nombrePiecesReceptionnees: 970
    });

    expect(reception.poidsPhysiqueUnitaireKg).toBe(1.5);
    expect(reception.tauxEcartPoids).toBeLessThanOrEqual(3);
    expect(reception.statutAlertePoids).toBe('Conforme');
  });

  it('generates a weight alert when reception unit weight exceeds 3%', () => {
    const reception = sopalTestDataset.receptionsUsinage2.find((item) => item.numeroOF === 'OF18100564');
    expect(reception?.poidsPhysiqueUnitaireKg).toBe(2.1);
    expect(reception?.tauxEcartPoids).toBeGreaterThan(3);
    expect(reception?.statutAlertePoids).toBe('Alerte');
  });

  it('covers conforming, alert, partial and missing U2 reception cases', () => {
    const rows = calculateNonReceivedQuantities(sopalTestDataset);
    expect(sopalTestDataset.receptionsUsinage2.find((item) => item.numeroOF === 'OF18100563')?.statutAlertePoids).toBe(
      'Conforme'
    );
    expect(sopalTestDataset.receptionsUsinage2.find((item) => item.numeroOF === 'OF18100564')?.statutAlertePoids).toBe(
      'Alerte'
    );
    expect(rows.find((row) => row.numeroOF === 'OF18100565')?.quantiteNonReceptionneePieces).toBe(50);
    expect(rows.find((row) => row.numeroOF === 'OF18100566')?.quantiteReceptionneeU2Pieces).toBe(0);
    expect(rows.find((row) => row.numeroOF === 'OF18100563')?.rebutUsinage2Kg).toBe(15);
  });

  it('includes furnaces in inventory report but excludes them from fonderie bilan', () => {
    const summary = calculateInventorySummary(sopalTestDataset);
    const m3010 = summary.find((row) => row.codeTypeMatiere === 'M3010L0');

    expect(m3010?.totalPartie3).toBe(800);
    expect(m3010?.totalInventaireRapport).toBe(5725);
    expect(m3010?.totalUtiliseBilan).toBe(4925);
  });

  it('calculates fonderie bilans from dataset values and keeps the M3060L0 alert visible', () => {
    const bilans = calculateBilanFonderie(sopalTestDataset);
    expect(bilans.find((row) => row.typeMatiere === 'M3000L0')?.ecartKg).toBe(0);
    expect(bilans.find((row) => row.typeMatiere === 'M3010L0')?.ecartKg).toBe(0);
    expect(bilans.find((row) => row.typeMatiere === 'M3060L0')?.ecartKg).toBe(50);
    expect(bilans.find((row) => row.typeMatiere === 'M3060L0')?.statut).toBe('AvecEcart');
  });

  it('calculates UAP2 balances for every OF, including the OF without reception', () => {
    const rows = calculateBilanUap2(sopalTestDataset);
    expect(rows).toHaveLength(sopalTestDataset.x3OrdresFabrication.length);
    expect(rows.find((row) => row.numeroOF === 'OF18100566')?.quantiteReceptionneeU2Kg).toBe(0);
    expect(rows.find((row) => row.numeroOF === 'OF18100566')?.quantiteNonReceptionneePieces).toBe(250);
  });

  it('uses manually entered EMR, SCI and TRN rejects in the UAP2 balance', () => {
    const dataset = structuredClone(sopalTestDataset);
    dataset.rebutsUap2 = [
      {
        id: 1,
        numeroOF: 'OF18100563',
        dateDeclaration: '2026-06-30',
        rebutEmrKg: 12,
        rebutSciKg: 3,
        rebutTrnKg: 2,
        statut: 'Valide',
        creePar: 'Administrateur SOPAL'
      }
    ];

    const row = calculateBilanUap2(dataset).find((item) => item.numeroOF === 'OF18100563');
    expect(row?.rebutEmrKg).toBe(12);
    expect(row?.rebutSciKg).toBe(3);
    expect(row?.rebutTrnKg).toBe(2);
    expect(row?.totalRebutEmrSciTrnKg).toBe(17);
  });

  it('has complete OF/article/gamme/nomenclature/report/movement/reception relations and no orphan data', () => {
    expectNoOrphans(sopalTestDataset);
  });

  it('uses numeroOF everywhere in mock-facing code, not legacy numOf or numeroOf', () => {
    const mockFacingSources = [
      datasetSource,
      testDataSourceRaw,
      receptionsServiceSourceRaw,
      receptionsUsinage2ViewSourceRaw,
      alertesViewSourceRaw,
      exporterSourceRaw,
      x3ImporterSourceRaw
    ].join('\n');

    expect(mockFacingSources).not.toContain('numOf');
    expect(mockFacingSources).not.toContain('numeroOf');
  });

  it('does not hard-code important mock KPI values directly inside Vue views', () => {
    const dashboardView = dashboardViewSourceRaw;
    const bilanView = bilanMatiereViewSourceRaw;
    const receptionsView = receptionsUsinage2ViewSourceRaw;
    const viewSource = `${dashboardView}\n${bilanView}\n${receptionsView}`;

    ['1455', '1560', 'MVTSTOCKSVCR_REEL'].forEach((literal) => {
      expect(viewSource).not.toContain(literal);
    });
  });

  it('X3 synchronization keeps application receptions intact', () => {
    const before = testDataRepository.resetDataset().receptionsUsinage2;
    const after = testDataRepository.synchronizeX3().receptionsUsinage2;
    expect(after).toEqual(before);
  });

  it('X3 synchronization keeps all application-entered inventory and waste data', () => {
    const before = testDataRepository.resetDataset();
    const after = testDataRepository.synchronizeX3();
    expect(after.inventaireConteneurs).toEqual(before.inventaireConteneurs);
    expect(after.inventaireCategories).toEqual(before.inventaireCategories);
    expect(after.inventaireFours).toEqual(before.inventaireFours);
    expect(after.dechetsFonderie).toEqual(before.dechetsFonderie);
    expect(after.rebutsUsinage2).toEqual(before.rebutsUsinage2);
  });

  it('creates a manual reception without requiring a motif', async () => {
    const before = testDataRepository.resetDataset();
    const calculated = calculateReceptionWeight(sopalTestDataset, {
      numeroOF: 'OF18100563',
      dateReception: '2026-06-30',
      origine: 'FONDERIE',
      poidsTotalReceptionneKg: 150,
      nombrePiecesReceptionnees: 100
    });
    const created = testDataRepository.addReception({
      ...calculated,
      statut: 'Valide',
      source: 'SaisieManuelle',
      creePar: 'Operateur',
      creeLe: '2026-06-30T12:00:00'
    });
    expect(created.source).toBe('SaisieManuelle');
    expect(created.codeArticle).toBe('0101A03-1');
    expect('motif' in created).toBe(false);
    expect(testDataRepository.getDataset().receptionsUsinage2).toHaveLength(before.receptionsUsinage2.length + 1);
  });
});
