export interface RapportsListType {
  id: number;
  titre: string;
  description: string;
  module: 'Bilan' | 'Déchets' | 'Réceptions' | 'Sage X3' | 'Inventaire' | 'Alertes';
  dateGeneration: string;
  formatDisponible: string[];
}
