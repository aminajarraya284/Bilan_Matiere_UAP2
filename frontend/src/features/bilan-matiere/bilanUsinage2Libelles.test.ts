import { describe, expect, it } from 'vitest';
import bilanView from './BilanMatiereView.vue?raw';
import receptionsView from '../receptions-usinage2/ReceptionsUsinage2View.vue?raw';

const bilanUsinageSection = bilanView.slice(
  bilanView.indexOf('<template v-if="activeTab === \'usinage2\'">'),
  bilanView.indexOf('<template v-if="activeTab === \'uap2\'">')
);

describe('Bilan Usinage 2 — écran dédié au bilan en pièces', () => {
  it('affiche uniquement les colonnes du Bilan U2', () => {
    for (const colonne of [
      'Numéro OF',
      'Code article',
      'Désignation',
      'Atelier',
      'Type matière',
      'Quantité réceptionnée U2 (pièces)',
      'Rebut U2 (pièces)',
      'Entrée OF signée (pièces)',
      'Résultat Bilan U2 (pièces)',
      'Statut'
    ]) {
      expect(bilanUsinageSection).toContain(colonne);
    }
  });

  it('n affiche plus les colonnes de non-réception ni de poids', () => {
    for (const texteInterdit of [
      'CTRL EMR',
      'Rebut EMR',
      'Nombre de pièces',
      'Quantité non réceptionnée',
      'Poids réceptionné total',
      'Poids physique unitaire',
      'Poids théorique unitaire',
      'Écart de poids',
      'Taux d’écart',
      'Statut poids',
      'Bloc B',
      'Contrôle des poids',
      'kg',
      'kg/pièce'
    ]) {
      expect(bilanUsinageSection).not.toContain(texteInterdit);
    }
  });

  it('affiche le titre sans mention technique', () => {
    expect(bilanUsinageSection).toContain('Bilan Usinage 2');
    expect(bilanUsinageSection).not.toContain('calcul backend');
  });

  it('affiche explicitement la formule du bilan', () => {
    expect(bilanUsinageSection).toContain('Réception U2 - Rebut U2 - Entrée OF');
  });

  it('ne rend qu une seule table pour le Bilan U2', () => {
    expect(bilanUsinageSection.match(/<table/g)).toHaveLength(1);
  });

  it('affiche les six KPI attendus', () => {
    for (const kpi of [
      'Quantité totale réceptionnée U2',
      'Total Rebut U2',
      'Total Entrée OF',
      'Écart total Bilan U2',
      'Nombre de lignes',
      'Nombre d’anomalies'
    ]) {
      expect(bilanUsinageSection).toContain(kpi);
    }
  });

  it('consomme directement les termes calculés par le backend', () => {
    expect(bilanUsinageSection).toContain('quantiteReceptionneePieces');
    expect(bilanUsinageSection).toContain('rebutUsinage2Pieces');
    expect(bilanUsinageSection).toContain('entreeOfPieces');
    expect(bilanUsinageSection).toContain('bilanUsinage2Pieces');
    expect(bilanUsinageSection).not.toMatch(/quantiteReceptionneePieces\s*-\s*/);
  });

  it('ne propose aucun accès aux postes processus', () => {
    expect(bilanUsinageSection).not.toContain('Administrer les postes');
    expect(bilanUsinageSection).not.toContain('/postes-processus');
    expect(bilanUsinageSection).not.toContain('Postes processus');
  });

  it('ne consomme plus les champs de non-réception ou de poids', () => {
    expect(bilanUsinageSection).not.toContain('quantiteCtrlEmrPieces');
    expect(bilanUsinageSection).not.toContain('rebutEmrPieces');
    expect(bilanUsinageSection).not.toContain('quantiteNonReceptionneePieces');
    expect(bilanUsinageSection).not.toContain('poidsReceptionneTotalKg');
    expect(bilanUsinageSection).not.toContain('statutPoids');
  });

  it('conserve uniquement créer une réception et recharger dans l état vide', () => {
    const emptyState = bilanUsinageSection.slice(bilanUsinageSection.indexOf('<tr v-if="!bilansUsinage.length">'));

    expect(emptyState).toContain('Créer une réception');
    expect(emptyState).toContain('Recharger');
    expect(emptyState).not.toContain('Imprimer');
    expect(bilanUsinageSection).toContain('v-if="bilansUsinage.length > 0"');
  });
});

describe('Réceptions Usinage 2 — responsabilités conservées', () => {
  it('conserve l alerte de non-réception dans l écran des réceptions', () => {
    for (const colonne of [
      'CTRL EMR',
      'Rebut EMR',
      'Nombre de pièces',
      'Quantité réceptionnée',
      'Quantité non réceptionnée'
    ]) {
      expect(receptionsView).toContain(colonne);
    }
  });

  it('conserve les poids uniquement pour compléter une réception', () => {
    expect(receptionsView).toContain('Poids réceptionné total (kg)');
    expect(receptionsView).toContain('Poids physique unitaire (kg/pièce)');
    expect(receptionsView).toContain('Poids théorique unitaire (kg/pièce)');
    expect(receptionsView).not.toMatch(/poidsTotalReceptionneKg\s*\/\s*/);
  });

  it('conserve les blocages de validation du formulaire', () => {
    expect(receptionsView).toContain('blocagesReception');
    expect(receptionsView).toContain(':disabled="blocagesReception.length > 0"');
  });
});
