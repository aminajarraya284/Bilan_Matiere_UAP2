import { BUSINESS_RULES } from '@/config/businessRules';
import { AlertesWeightAlert, AlertesBilanAlert, AlertesSummary } from './alertes.types';
import { testDataRepository } from '@/services/testDataRepository.service';
import {
  findNomenclatureMatiereByArticle,
  findOrdreFabrication,
  getActiveAlerts
} from '@/services/bilanCalculation.service';
import { readControlesPoidsFonderie } from '@/features/controle-poids-fonderie/controlePoidsFonderie.storage';
import { assertMockSource } from '@/config/dataSources';

const buildControlePoidsAlerts = (periode: string): AlertesWeightAlert[] => {
  const dataset = testDataRepository.getDataset();
  const seuil = dataset.parametresApplication.seuilAlertePoidsPourcentage;

  return readControlesPoidsFonderie()
    .filter((controle) => controle.dateControle?.slice(0, 7) === periode)
    .map((controle, index): AlertesWeightAlert | null => {
      const pointValues = Object.values(controle.points ?? {});
      const totalPieces =
        controle.totalPiecesControle ?? pointValues.reduce((sum, point) => sum + (Number(point.pieces) || 0), 0);
      const totalPoidsNet =
        controle.totalPoidsNetKg ?? pointValues.reduce((sum, point) => sum + (Number(point.poidsNetKg) || 0), 0);
      const poidsPhyKg =
        controle.poidsPhysiqueUnitaireKg ?? controle.poidsPhyKg ?? (totalPieces > 0 ? totalPoidsNet / totalPieces : 0);

      if (totalPieces <= 0 || poidsPhyKg <= 0) return null;

      const ordre = findOrdreFabrication(dataset, controle.numeroOF);
      const codeArticle = ordre?.codeArticleParent ?? controle.code;
      const nomenclature = findNomenclatureMatiereByArticle(dataset, codeArticle);
      const poidsTheoriqueUnitaireKg = nomenclature?.quantiteLienCumulee ?? 0;

      if (poidsTheoriqueUnitaireKg <= 0) return null;

      const tauxEcartPoids = Math.abs((poidsPhyKg - poidsTheoriqueUnitaireKg) / poidsTheoriqueUnitaireKg) * 100;

      if (tauxEcartPoids <= seuil) return null;

      const poidsTheoriqueTotalKg = poidsTheoriqueUnitaireKg * totalPieces;
      const poidsTotalReceptionneKg = totalPoidsNet || poidsPhyKg * totalPieces;

      return {
        id: 900000 + index,
        numeroOF: controle.numeroOF,
        dateReception: controle.dateControle,
        codeArticle,
        designationArticle: controle.designation || ordre?.designationArticle || codeArticle,
        codeTypeMatiere: nomenclature?.codeComposant ?? controle.code,
        poidsTheoriqueTotalKg,
        poidsTotalReceptionneKg,
        poidsKg: poidsTheoriqueTotalKg,
        poidsPhysique: poidsTotalReceptionneKg,
        poidsTheoriqueUnitaireKg,
        poidsPhysiqueUnitaireKg: poidsPhyKg,
        ecartPoidsUnitaireKg: Number((poidsPhyKg - poidsTheoriqueUnitaireKg).toFixed(3)),
        tauxEcartPoids: Number(tauxEcartPoids.toFixed(2)),
        difference: Number((poidsTotalReceptionneKg - poidsTheoriqueTotalKg).toFixed(3)),
        atelierOrigine: controle.atelier,
        mois: controle.dateControle.slice(0, 7),
        source: 'SaisieManuelle'
      };
    })
    .filter((alert): alert is AlertesWeightAlert => Boolean(alert));
};

export const alertesService = {
  async getWeightAlerts(_tolerance: number): Promise<AlertesWeightAlert[]> {
    assertMockSource('alertes');
    const dataset = testDataRepository.getDataset();
    const { weightAlerts } = getActiveAlerts(dataset, BUSINESS_RULES.periodeParDefaut);
    return [...weightAlerts, ...buildControlePoidsAlerts(BUSINESS_RULES.periodeParDefaut)].sort(
      (a, b) => Math.abs(b.difference) - Math.abs(a.difference)
    );
  },

  async getBilanAlerts(): Promise<AlertesBilanAlert[]> {
    assertMockSource('alertes');
    const { bilanAlerts } = getActiveAlerts(testDataRepository.getDataset(), BUSINESS_RULES.periodeParDefaut);
    return bilanAlerts.sort((a, b) => Math.abs(b.tauxEcart) - Math.abs(a.tauxEcart));
  },

  async getSummary(tolerance: number): Promise<AlertesSummary> {
    assertMockSource('alertes');
    const dataset = testDataRepository.getDataset();
    const weightAlerts = await this.getWeightAlerts(tolerance);
    const totalLostKg = dataset.dechetsFonderie.reduce((sum, dechet) => sum + dechet.poidsDechetKg, 0);

    return {
      totalLostKg,
      totalWeightAlerts: weightAlerts.length,
      totalBilanAlerts: (await this.getBilanAlerts()).length
    };
  }
};
