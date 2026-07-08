const ENCOURS_DEBUT_PAR_TYPE_MATIERE: Record<string, number> = {
  M3010L0: 4800,
  M3000L0: 1200
};

// Constantes temporaires cote frontend. Le backend ASP.NET Core deviendra la source officielle.
export const BUSINESS_RULES = {
  periodeParDefaut: '2026-06',
  dateDebutParDefaut: '2026-06-01',
  dateFinParDefaut: '2026-06-30',
  semaineDeclarationParDefaut: 26,
  seuilEcartPourcentage: 3,
  seuilEcartBilanAutoPourcentage: 1.5,
  seuilCritiqueEcartPourcentage: 5,
  seuilAlertePoidsKg: 500,
  seuilEcartKgHighlight: 100,
  seuilDecalageImportantKg: 500,
  typeMatieresParDefaut: ['M3010L0', 'M3000L0', 'M3020L0'] as const,
  encoursDebutParTypeMatiere: ENCOURS_DEBUT_PAR_TYPE_MATIERE,
  statutBilanAvecEcart: 'AvecEcart',
  statutsVerificationControleNonReception: ['AVerifier', 'Justifie', 'Regularise', 'Anomalie'] as const
} as const;
