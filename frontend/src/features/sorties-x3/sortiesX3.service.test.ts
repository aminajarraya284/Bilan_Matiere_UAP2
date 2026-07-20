import { describe, expect, it } from 'vitest';
import {
  mergeConsommationRows,
  normalizeOF,
  parseExcelDate,
  parseEncoursProductionRows,
  parseMouvementStockRows,
  toNumber
} from './sortiesX3.service';

describe('rapprochement Mvt de stock / Encours production', () => {
  it('détecte les en-têtes même après une ligne de titre', () => {
    const mouvements = parseMouvementStockRows([
      ['Mvt de stock par trans-VCR'],
      ['Date imput', 'Code', 'Trans-VCR', 'Quantité'],
      ['15/07/2026', 'M3010L0', '23531', '1 600,5'],
      ['15/07/2026', 'M3010L0', '23532', -40]
    ]);
    const encours = parseEncoursProductionRows([
      ['Encours production'],
      ['Transaction VCR', 'Numéro OF', 'Article', 'Désignation'],
      ['23531', '18100563', '0101A03-1', 'Corps mini vanne'],
      ['23532', 'OF18100563', '0101A03-1', 'Corps mini vanne']
    ]);

    expect(mouvements).toEqual([
      {
        transactionVcr: '23531',
        numeroOF: '',
        dateMouvement: '2026-07-15',
        codeTypeMatiere: 'M3010L0',
        sortieMP: 1600.5
      },
      {
        transactionVcr: '23532',
        numeroOF: '',
        dateMouvement: '2026-07-15',
        codeTypeMatiere: 'M3010L0',
        sortieMP: -40
      }
    ]);
    expect(encours[0]).toMatchObject({
      transactionVcr: '23531',
      numeroOF: 'OF18100563',
      codeArticle: '0101A03-1',
      designationArticle: 'Corps mini vanne'
    });
  });

  it('additionne les sorties et déduit les retours pour un même OF', () => {
    const result = mergeConsommationRows(
      [
        {
          transactionVcr: '23531',
          numeroOF: '',
          dateMouvement: '2026-07-15',
          codeTypeMatiere: 'M3010L0',
          sortieMP: 1600
        },
        {
          transactionVcr: '23532',
          numeroOF: '',
          dateMouvement: '2026-07-15',
          codeTypeMatiere: 'M3010L0',
          sortieMP: -40
        },
        {
          transactionVcr: 'sans-lien',
          numeroOF: '',
          dateMouvement: '2026-07-15',
          codeTypeMatiere: 'M3010L0',
          sortieMP: 25
        }
      ],
      [
        {
          transactionVcr: '23531',
          numeroOF: 'OF18100563',
          codeArticle: '0101A03-1',
          designationArticle: 'Corps mini vanne'
        },
        {
          transactionVcr: '23532',
          numeroOF: 'OF18100563',
          codeArticle: '0101A03-1',
          designationArticle: 'Corps mini vanne'
        }
      ]
    );

    expect(result.rows).toEqual([
      {
        id: 1,
        dateMouvement: '2026-07-15',
        numeroOF: 'OF18100563',
        codeArticle: '0101A03-1',
        designationArticle: 'Corps mini vanne',
        codeTypeMatiere: 'M3010L0',
        sortieMP: 1560
      }
    ]);
    expect(result.unmatchedCount).toBe(1);
  });

  it('utilise l’OF directement lorsque le mouvement contient déjà cette information', () => {
    const result = mergeConsommationRows(
      [
        {
          transactionVcr: '',
          numeroOF: 'OF18100564',
          dateMouvement: '2026-07-16',
          codeTypeMatiere: 'M3000L0',
          sortieMP: 800
        }
      ],
      [
        {
          transactionVcr: '23533',
          numeroOF: 'OF18100564',
          codeArticle: '0102A03-1',
          designationArticle: 'Corps mini vanne MF'
        }
      ]
    );

    expect(result.rows[0]).toMatchObject({
      dateMouvement: '2026-07-16',
      sortieMP: 800,
      numeroOF: 'OF18100564',
      codeArticle: '0102A03-1',
      codeTypeMatiere: 'M3000L0'
    });
  });

  it('normalise les nombres et les numéros OF sans perdre les valeurs négatives', () => {
    expect(toNumber('-12,5')).toBe(-12.5);
    expect(toNumber('1 250 kg')).toBe(1250);
    expect(toNumber('invalide')).toBe(0);
    expect(normalizeOF(18100563)).toBe('OF18100563');
    expect(normalizeOF(' OF18100564 ')).toBe('OF18100564');
    expect(parseExcelDate('17/07/2026')).toBe('2026-07-17');
  });
});
