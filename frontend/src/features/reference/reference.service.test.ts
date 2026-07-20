import { beforeEach, describe, expect, it } from 'vitest';
import { testDataRepository } from '@/services/testDataRepository.service';
import { parseGammeRows, parseNomenclatureRows, referenceService } from './reference.service';

describe('reference fabrication Excel imports', () => {
  beforeEach(() => localStorage.clear());

  it('detects a GAMMEFAB header after title rows and keeps column F as the workshop', () => {
    const rows = [
      ['Export X3 GAMMEFAB'],
      [],
      [
        'Colonne A',
        'Colonne B',
        'Date fin validité',
        'Étape suivi',
        'Colonne E',
        'Article',
        'Code poste',
        'Nombre postes',
        'Atelier',
        'Désignation opération',
        'Opération',
        'Unité opération',
        'Coefficient stock',
        'Temps cycle',
        'TRS',
        'Temps attente',
        'Jalon',
        'Quantité',
        'Cadence',
        'Date modification',
        'Temps réglage',
        'CCPT'
      ],
      ['0101A03-1', 'Article test', '31/12/2099', '1', '10', '19', 'P-19', 1, 'UAP2', 'Usinage', '1', 'PC'],
      ['0101A03-1', 'Doublon', '31/12/2099', '1', '10', '99', 'P-99', 1, 'UAP2', 'Doublon', '1', 'PC']
    ];

    const result = parseGammeRows(rows, 'IMP-TEST-GAMME');

    expect(result.items).toHaveLength(1);
    expect(result.ignoredCount).toBe(1);
    expect(result.items[0]).toMatchObject({
      codeArticle: '0101A03-1',
      numeroOperation: '10',
      codeAtelier: '99',
      dateFinValidite: '2099-12-31',
      importBatchId: 'IMP-TEST-GAMME'
    });
  });

  it('keeps BOMARTPARENT columns H, I and J aligned', () => {
    const rows = [
      [
        'Code parent niveau',
        'Code composant',
        'Code article parent',
        'Désignation article parent',
        'Niveau',
        'Désignation composant',
        'Séquence',
        'Quantité lien',
        'Quantité lien cumulée',
        'Unité quantité',
        'Catégorie composant',
        'Type lien',
        'Pourcentage rebut'
      ],
      ['0101A03-1', 'M3010L0', '0101A03-1', 'Article test', 1, 'Matière M3010L0', '10', '1,5', '1,750', 'KG', 'MATIERE', 'NORMAL', '2,5']
    ];

    const result = parseNomenclatureRows(rows, 'IMP-TEST-BOM');

    expect(result.ignoredCount).toBe(0);
    expect(result.items[0]).toMatchObject({
      codeArticleParent: '0101A03-1',
      codeComposant: 'M3010L0',
      quantiteLien: 1.5,
      quantiteLienCumulee: 1.75,
      uniteQuantite: 'KG',
      pourcentageRebut: 2.5,
      importBatchId: 'IMP-TEST-BOM'
    });
  });

  it('ignores nomenclature rows with unsupported material codes', () => {
    const rows = [
      ['Code composant', 'Code article parent', 'Quantité lien'],
      ['INVALID', '0101A03-1', 1.5]
    ];

    const result = parseNomenclatureRows(rows, 'IMP-TEST-BOM');

    expect(result.items).toEqual([]);
    expect(result.ignoredCount).toBe(1);
  });

  it('restores test gammes without deleting application-entered data', () => {
    const dataset = testDataRepository.getDataset();
    dataset.refGammeFabrication = [];
    dataset.dechetsFonderie = [];
    testDataRepository.replaceDataset(dataset);

    const restored = referenceService.resetGammes();
    const afterReset = testDataRepository.getDataset();

    expect(restored.length).toBeGreaterThan(0);
    expect(afterReset.refGammeFabrication.length).toBeGreaterThan(0);
    expect(afterReset.dechetsFonderie).toEqual([]);
  });
});
