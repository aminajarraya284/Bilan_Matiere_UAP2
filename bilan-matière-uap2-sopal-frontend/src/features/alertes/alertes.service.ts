import { storage } from '@/utils/storage';
import { BUSINESS_RULES } from '@/config/businessRules';
import { AlertesWeightAlert, AlertesBilanAlert, AlertesSummary } from './alertes.types';
import { bilanService } from '../bilan-matiere/bilan.service';
import { ReceptionUsinage2Mouvement } from '@/features/receptions-usinage2/receptions.types';

const createWeightAlert = (rec: ReceptionUsinage2Mouvement, tolerance: number): AlertesWeightAlert | null => {
  const difference = rec.poidsPhysique - rec.poidsKg;
  if (Math.abs(difference) >= tolerance && rec.atelierOrigine === 'FONDERIE') {
    return {
      id: rec.id,
      numOf: rec.numOf,
      dateReception: rec.dateReception,
      codeArticle: rec.codeArticle,
      designationArticle: rec.designationArticle,
      codeTypeMatiere: rec.codeTypeMatiere,
      poidsKg: rec.poidsKg,
      poidsPhysique: rec.poidsPhysique,
      difference,
      atelierOrigine: rec.atelierOrigine,
      mois: rec.mois,
      source: rec.source
    };
  }
  return null;
};

export const alertesService = {
  async getWeightAlerts(tolerance: number): Promise<AlertesWeightAlert[]> {
    const receptions = storage.getReceptions();
    return receptions
      .map((rec) => createWeightAlert(rec, tolerance))
      .filter((alert): alert is AlertesWeightAlert => alert !== null)
      .sort((a, b) => Math.abs(b.difference) - Math.abs(a.difference));
  },

  async getBilanAlerts(): Promise<AlertesBilanAlert[]> {
    const bilans = await bilanService.getBilans(BUSINESS_RULES.periodeParDefaut);
    return bilans
      .filter((b) => Math.abs(b.tauxEcart) >= BUSINESS_RULES.seuilEcartPourcentage)
      .map((b, index) => ({
        id: index + 1,
        typeMatiere: b.typeMatiere,
        inputTotal: b.inputTotal,
        outputTotal: b.outputTotal,
        ecartKg: b.ecartKg,
        tauxEcart: b.tauxEcart,
        statut: b.statut,
        justification: b.justification
      }))
      .sort((a, b) => Math.abs(b.tauxEcart) - Math.abs(a.tauxEcart));
  },

  async getSummary(tolerance: number): Promise<AlertesSummary> {
    const weightAlerts = await this.getWeightAlerts(tolerance);
    const dechets = storage.getDechets();
    const totalLostKg = dechets.reduce((sum, d) => sum + d.quantiteKg, 0);

    return {
      totalLostKg,
      totalWeightAlerts: weightAlerts.length,
      totalBilanAlerts: (await this.getBilanAlerts()).length
    };
  }
};
