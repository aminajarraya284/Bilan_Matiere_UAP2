import api from '@/services/api';
import { BUSINESS_RULES } from '@/config/businessRules';
import { DashboardStats, DashboardAlert, DashboardBilanRow } from './dashboard.types';
import { bilanService } from '../bilan-matiere/bilan.service';
import { storage } from '@/utils/storage';
import { BilanMatiereReport } from '@/features/bilan-matiere/bilan.types';

type DashboardData = {
  stats: DashboardStats;
  alerts: DashboardAlert[];
  rows: DashboardBilanRow[];
};

const buildStatsFromBilans = (bilans: BilanMatiereReport[], mois: string): DashboardStats => {
  const inputTotal = bilans.reduce((sum, b) => sum + b.inputTotal, 0);
  const outputTotal = bilans.reduce((sum, b) => sum + b.outputTotal, 0);
  const ecartKg = inputTotal - outputTotal;
  const tauxEcart = inputTotal > 0 ? (ecartKg / inputTotal) * 100 : 0;

  return {
    inputMatiere: inputTotal,
    outputMatiere: outputTotal,
    ecartMatiere: ecartKg,
    tauxEcart,
    periodeLabel: mois
  };
};

const buildAlertsFromBilans = (bilans: BilanMatiereReport[], mois: string): DashboardAlert[] => {
  const alertsList: DashboardAlert[] = [];
  let alertId = 1;
  const seuilEcartLabel = BUSINESS_RULES.seuilEcartBilanAutoPourcentage.toLocaleString('fr-FR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });

  bilans.forEach((b) => {
    if (Math.abs(b.tauxEcart) > BUSINESS_RULES.seuilEcartBilanAutoPourcentage) {
      alertsList.push({
        id: alertId++,
        type: 'danger',
        title: `Alerte Écart Critique : ${b.typeMatiere}`,
        description: `Le laiton de type ${b.typeMatiere} présente un écart significatif de ${b.ecartKg.toLocaleString('fr-FR', { maximumFractionDigits: 0 })} kg (${b.tauxEcart.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}%), dépassant la tolérance usine de ${seuilEcartLabel}%.`
      });
    }
  });

  const conteneurs = storage.getConteneurs();
  const brouillonsInventaire = conteneurs.filter((c) => {
    const itemMonth = c.dateInventaire
      ? c.dateInventaire.slice(0, 7)
      : `${c.annee}-${c.mois.toString().padStart(2, '0')}`;
    return c.statut === 'Brouillon' && itemMonth === mois;
  }).length;

  if (brouillonsInventaire > 0) {
    alertsList.push({
      id: alertId++,
      type: 'warning',
      title: "Fiches d'inventaire non validées",
      description: `${brouillonsInventaire} fiches d'inventaire conteneur sont encore au statut Brouillon et ne sont pas incluses dans le calcul du bilan.`
    });
  }

  const dechets = storage.getDechets();
  const dechetsBrouillons = dechets.filter(
    (d) => d.statut === 'Brouillon' && d.dateDeclaration.slice(0, 7) === mois
  ).length;

  if (dechetsBrouillons > 0) {
    alertsList.push({
      id: alertId++,
      type: 'warning',
      title: 'Déclarations déchets en attente',
      description: `${dechetsBrouillons} déclarations de déchets Fonderie n'ont pas encore été validées par le chef d'atelier.`
    });
  }

  if (alertsList.length === 0) {
    alertsList.push({
      id: alertId++,
      type: 'info',
      title: 'Bilan de Période Conforme',
      description: 'Tous les écarts matières calculés sur la période actuelle respectent les tolérances de production.'
    });
  }

  return alertsList;
};

const buildRowsFromBilans = (bilans: BilanMatiereReport[]): DashboardBilanRow[] =>
  bilans.map((b) => ({
    typeMatiere: b.typeMatiere,
    inputX3: b.inputTotal,
    encoursFin: b.encoursFinReel,
    dechets: b.dechetsFonderie,
    usinage2: b.receptionsUsinage2,
    poidsNonReceptionne: b.poidsNonReceptionne,
    ecartKg: -b.ecartKg,
    statut: b.statut
  }));

export const dashboardService = {
  async getStats(mois: string, dateDebut?: string, dateFin?: string): Promise<DashboardStats> {
    try {
      if (import.meta.env.VITE_USE_REAL_API !== 'true') {
        const bilans = await bilanService.getBilans(mois, dateDebut, dateFin);
        return buildStatsFromBilans(bilans, mois);
      }
      const res = await api.get<DashboardStats>('/dashboard/summary', { params: { mois, dateDebut, dateFin } });
      return res.data;
    } catch {
      return {
        inputMatiere: 37214.0,
        outputMatiere: 35557.75,
        ecartMatiere: 1656.25,
        tauxEcart: 4.45,
        periodeLabel: mois
      };
    }
  },

  async getAlerts(mois: string, dateDebut?: string, dateFin?: string): Promise<DashboardAlert[]> {
    try {
      if (import.meta.env.VITE_USE_REAL_API !== 'true') {
        const bilans = await bilanService.getBilans(mois, dateDebut, dateFin);
        return buildAlertsFromBilans(bilans, mois);
      }
      const res = await api.get<DashboardAlert[]>('/dashboard/alerts', { params: { mois, dateDebut, dateFin } });
      return res.data;
    } catch {
      return [
        {
          id: 1,
          type: 'danger',
          title: 'Alerte Écart Matière Critique',
          description: 'Type M3010L0 présente un écart de 1 656 kg (4,45%), dépassant le seuil de tolérance usine.'
        }
      ];
    }
  },

  async getBilanParMatiere(mois: string, dateDebut?: string, dateFin?: string): Promise<DashboardBilanRow[]> {
    try {
      if (import.meta.env.VITE_USE_REAL_API !== 'true') {
        const bilans = await bilanService.getBilans(mois, dateDebut, dateFin);
        return buildRowsFromBilans(bilans);
      }
      const res = await api.get<DashboardBilanRow[]>('/dashboard/bilan-par-matiere', {
        params: { mois, dateDebut, dateFin }
      });
      return res.data;
    } catch {
      return [
        {
          typeMatiere: 'M3010L0',
          inputX3: 37214,
          encoursFin: 5410,
          dechets: 752,
          usinage2: 29395,
          poidsNonReceptionne: 0,
          ecartKg: -1656,
          statut: 'AvecEcart'
        }
      ];
    }
  },

  async getDashboardData(mois: string, dateDebut?: string, dateFin?: string): Promise<DashboardData> {
    try {
      if (import.meta.env.VITE_USE_REAL_API !== 'true') {
        const bilans = await bilanService.getBilans(mois, dateDebut, dateFin);
        return {
          stats: buildStatsFromBilans(bilans, mois),
          alerts: buildAlertsFromBilans(bilans, mois),
          rows: buildRowsFromBilans(bilans)
        };
      }

      const [stats, alerts, rows] = await Promise.all([
        this.getStats(mois, dateDebut, dateFin),
        this.getAlerts(mois, dateDebut, dateFin),
        this.getBilanParMatiere(mois, dateDebut, dateFin)
      ]);

      return { stats, alerts, rows };
    } catch {
      const rows = await this.getBilanParMatiere(mois, dateDebut, dateFin);
      return {
        stats: {
          inputMatiere: 37214,
          outputMatiere: 35557.75,
          ecartMatiere: 1656.25,
          tauxEcart: 4.45,
          periodeLabel: mois
        },
        alerts: [
          {
            id: 1,
            type: 'danger',
            title: 'Alerte Écart Matière Critique',
            description: 'Type M3010L0 présente un écart de 1 656 kg (4,45%), dépassant le seuil de tolérance usine.'
          }
        ],
        rows
      };
    }
  }
};
