import type { X3ExcelFileType } from '@/services/x3ExcelImportService';

export interface X3CardContent {
  fileType: X3ExcelFileType;
  title: string;
  subtitle: string;
  description: string;
  usedBy: string[];
  flow: string[];
  notice: { tone: 'info'; text: string };
}

export const X3_CARDS: X3CardContent[] = [
  {
    fileType: 'YRJMCH',
    title: 'YRJMCH — Rapports machine',
    subtitle: 'Quantités contrôlées et rebuts',
    description:
      'Importe les quantités contrôlées et les rebuts par date, Numéro OF, Code article et Poste. ' +
      'Le processus SCI, EMR ou TRN est déterminé automatiquement à partir des trois premiers caractères de la colonne B.',
    usedBy: ['Bilan Usinage 2', 'Bilan UAP2', 'Alertes de non-réception'],
    flow: [
      'Ligne YRJMCH',
      '→ Date du rapport',
      '→ Numéro OF',
      '→ Code article',
      '→ Poste, colonne B',
      '→ Lecture des 3 premiers caractères',
      '→ SCI / EMR / TRN / Non classé',
      '→ Quantité CTRL, colonne H',
      '→ Quantité rebut, colonne I',
      '→ Regroupement par période + Numéro OF + Code article'
    ],
    notice: {
      tone: 'info',
      text:
        'Les postes commençant par SCI, EMR ou TRN sont classés automatiquement. ' +
        'Tout autre préfixe reste “Non classé” et doit être vérifié dans les données X3.'
    }
  },
  {
    fileType: 'MVTSTOTRSVCR',
    title: 'MVTSTOTRSVCR — Entrées OF',
    subtitle: 'Mouvements de stock X3',
    description:
      'Ce fichier contient les mouvements Entrée OF. Il ne sert pas à déterminer les processus SCI, EMR ou TRN.',
    usedBy: ['Bilan Usinage 2', 'Bilan UAP2'],
    flow: [
      'Ligne MVT',
      '→ Date d’imputation',
      '→ Pièce origine / Numéro OF',
      '→ Code article',
      '→ Quantité signée',
      '→ Transaction Entrée OF',
      '→ Regroupement par période + Numéro OF + Code article'
    ],
    notice: {
      tone: 'info',
      text: 'Les quantités restent signées. Exemple : -980 + 980 = 0.'
    }
  }
];

export const AUCUNE_ENTREE_OF_MESSAGE =
  'Ce fichier ne contient aucune transaction Entrée OF. Vérifiez que vous avez sélectionné le bon export X3.';

export const USAGE_BLOCK = {
  title: 'Comment ces données sont utilisées',
  yrjmch: {
    title: 'YRJMCH',
    items: [
      'fournit les quantités CTRL et rebut',
      'classe automatiquement SCI, EMR et TRN par préfixe',
      'regroupe les résultats par période + Numéro OF + Code article'
    ]
  },
  mvt: {
    title: 'MVTSTOTRSVCR',
    items: [
      'fournit les Entrées OF signées',
      'ne détermine aucun processus',
      'regroupe les résultats par période + Numéro OF + Code article'
    ]
  },
  cle: 'Période + Numéro OF + Code article'
} as const;

export const FORMULES = [
  {
    bilan: 'Bilan Usinage 2',
    lignes: ['Bilan U2 = Réceptions U2 validées − Rebut U2 validé − Entrée OF signée']
  },
  {
    bilan: 'Alerte de non-réception',
    lignes: ['Quantité non réceptionnée = Quantité CTRL EMR − Rebut EMR − Réceptions U2 validées']
  }
] as const;
