import api from '@/services/api';
import { isApiSource } from '@/config/dataSources';
import { ReceptionUsinage2Mouvement } from './receptions.types';
import { testDataRepository } from '@/services/testDataRepository.service';
import { calculateReceptionWeight, buildReceptionDefaults } from '@/services/bilanCalculation.service';
import type { ReceptionUsinage2Test } from '@/mocks/sopalTestDataset';
import type { CreateReceptionU2Request, ReceptionU2DefaultsResponse, ReceptionU2Response } from '@/types/api.types';

// Contrat reel renvoye par BilanMatiereUAP2.Contracts.ReceptionsUsinage2.ReceptionUsinage2Response
// (voir backend/src/BilanMatiereUAP2.Contracts/ReceptionsUsinage2/ReceptionUsinage2Contracts.cs).
type ReceptionUsinage2ApiResponse = {
  id: string;
  numeroOF: string;
  dateReception: string;
  nombrePiecesReceptionnees: number;
  poidsTotalReceptionneKg: number | null;
  origine: string | null;
  observation: string | null;
  codeArticle: string | null;
  designationArticle: string | null;
  situationX3: string | null;
  atelierOrigine: string | null;
  codeTypeMatiere: string | null;
  quantiteLienUtilisee: number | null;
  uniteQuantiteLien: string | null;
  poidsTheoriqueUnitaireKg: number | null;
  poidsTheoriqueTotalKg: number | null;
  poidsPhysiqueUnitaireKg: number | null;
  ecartPoidsUnitaireKg: number | null;
  tauxEcartPoidsPct: number | null;
  totalCtrlSci: number | null;
  totalCtrlEmr: number | null;
  totalCtrlTrn: number | null;
  totalRebutSci: number | null;
  totalRebutEmr: number | null;
  totalRebutTrn: number | null;
  quantiteNonReceptionnee: number | null;
  statutReception: 'Brouillon' | 'Valide' | 'Annule';
  creePar: string | null;
  dateCreationUtc: string;
};

type PagedReceptionsApiResponse = { items: ReceptionUsinage2ApiResponse[]; total: number };

const toReceptionMouvementFromApi = (item: ReceptionUsinage2ApiResponse, index: number): ReceptionU2Response => ({
  id: index + 1,
  apiId: item.id,
  numeroOF: item.numeroOF,
  dateReception: item.dateReception,
  codeArticle: item.codeArticle ?? '',
  designationArticle: item.designationArticle ?? '',
  origine: (item.origine ?? 'AUTRE') as 'FONDERIE' | 'AUTRE',
  atelierOrigine: item.atelierOrigine ?? '',
  codeTypeMatiere: item.codeTypeMatiere ?? '',
  designationTypeMatiere: `Matière ${item.codeTypeMatiere ?? ''}`,
  quantiteLienUtilisee: item.quantiteLienUtilisee ?? 0,
  uniteQuantiteLien: (item.uniteQuantiteLien ?? 'KG') as 'KG',
  poidsTheoriqueUnitaireKg: item.poidsTheoriqueUnitaireKg ?? 0,
  poidsTheoriqueTotalKg: item.poidsTheoriqueTotalKg ?? 0,
  poidsPhysiqueUnitaireKg: item.poidsPhysiqueUnitaireKg ?? 0,
  poidsTotalReceptionneKg: item.poidsTotalReceptionneKg ?? 0,
  nombrePiecesReceptionnees: item.nombrePiecesReceptionnees,
  ecartPoidsUnitaireKg: item.ecartPoidsUnitaireKg ?? 0,
  tauxEcartPoids: item.tauxEcartPoidsPct ?? 0,
  statutAlertePoids: 'Conforme',
  seuilAlertePoidsPourcentage: 0,
  gammeFabricationId: 0,
  nomenclatureId: 0,
  mois: item.dateReception.slice(0, 7),
  statut: item.statutReception,
  source: 'SaisieManuelle',
  utilisateur: item.creePar ?? '',
  creeLe: item.dateCreationUtc,
  controlePoidsFonderieId: null,
  sourcePoidsPhysique: null,
  totalCtrlSci: item.totalCtrlSci,
  totalCtrlEmr: item.totalCtrlEmr,
  totalCtrlTrn: item.totalCtrlTrn,
  totalRebutSci: item.totalRebutSci,
  totalRebutEmr: item.totalRebutEmr,
  totalRebutTrn: item.totalRebutTrn,
  piecesReceptionneesU2: null,
  quantiteNonReceptionnee: item.quantiteNonReceptionnee,
  statutArticle: undefined,
  statutAtelier: undefined,
  statutTypeMatiere: undefined,
  statutNomenclature: undefined,
  statutPoidsPhysique: undefined,
  statutRapportMachine: undefined,
  avertissements: []
});

const toReceptionMouvement = (rec: ReceptionUsinage2Test): ReceptionUsinage2Mouvement => ({
  id: rec.id,
  numeroOF: rec.numeroOF,
  dateReception: rec.dateReception,
  codeArticle: rec.codeArticle,
  designationArticle: rec.designationArticle,
  origine: rec.origine,
  poidsTotalReceptionneKg: rec.poidsTotalReceptionneKg,
  nombrePiecesReceptionnees: rec.nombrePiecesReceptionnees,
  atelierOrigine: rec.atelierOrigine,
  codeTypeMatiere: rec.codeTypeMatiere,
  designationTypeMatiere: `Matiere ${rec.codeTypeMatiere}`,
  mois: rec.mois,
  poidsPhysiqueUnitaireKg: rec.poidsPhysiqueUnitaireKg,
  quantiteLienUtilisee: rec.quantiteLienUtilisee,
  uniteQuantiteLien: rec.uniteQuantiteLien,
  poidsTheoriqueUnitaireKg: rec.poidsTheoriqueUnitaireKg,
  poidsTheoriqueTotalKg: rec.poidsTheoriqueTotalKg,
  ecartPoidsUnitaireKg: rec.ecartPoidsUnitaireKg,
  tauxEcartPoids: rec.tauxEcartPoids,
  statutAlertePoids: rec.statutAlertePoids,
  seuilAlertePoidsPourcentage: rec.seuilAlertePoidsPourcentage,
  gammeFabricationId: rec.gammeFabricationId,
  nomenclatureId: rec.nomenclatureId,
  statut: rec.statut,
  source: rec.source,
  utilisateur: rec.creePar,
  creeLe: rec.creeLe
});

export type ReceptionInput = CreateReceptionU2Request;

export const receptionsService = {
  async getReceptionDefaults(numeroOF: string, dateReception?: string): Promise<ReceptionU2DefaultsResponse> {
    if (!isApiSource('receptionsU2')) {
      return buildReceptionDefaults(testDataRepository.getDataset(), numeroOF);
    }
    const res = await api.get(`/v1/receptions-usinage2/contexte/${encodeURIComponent(numeroOF)}`, {
      params: dateReception ? { dateReception } : undefined
    });
    const context = res.data as {
      numeroOF: string;
      codeArticle: string | null;
      designationArticle: string | null;
      atelierOrigine: string | null;
      codeTypeMatiere: string | null;
      quantiteLienUtilisee: number | null;
      uniteQuantiteLien: string | null;
      poidsTheoriqueUnitaireKg: number | null;
      poidsTheoriqueTotalKg: number | null;
      poidsPhysiqueUnitaireKg: number | null;
      sourcePoidsPhysique: string | null;
      totalCtrlSci: number | null;
      totalCtrlEmr: number | null;
      totalCtrlTrn: number | null;
      totalRebutSci: number | null;
      totalRebutEmr: number | null;
      totalRebutTrn: number | null;
      piecesReceptionneesU2: number | null;
      quantiteNonReceptionnee: number | null;
      nombreLignesRapportMachine: number;
      nombreLignesRapportMachineNonMappees: number;
      statutArticle: string;
      statutAtelier: string;
      statutTypeMatiere: string;
      statutNomenclature: string;
      statutPoidsPhysique: string;
      statutRapportMachine: string;
      avertissements: string[];
    };
    return {
      ...context,
      numeroOF: context.numeroOF,
      codeArticle: context.codeArticle ?? '',
      designationArticle: context.designationArticle ?? '',
      atelierOrigine: context.atelierOrigine ?? '',
      codeTypeMatiere: context.codeTypeMatiere ?? '',
      quantiteLienUtilisee: context.quantiteLienUtilisee ?? 0,
      uniteQuantiteLien: (context.uniteQuantiteLien ?? 'KG') as 'KG',
      poidsTheoriqueUnitaireKg: context.poidsTheoriqueUnitaireKg ?? 0,
      gammeFabricationId: 0,
      nomenclatureId: 0
    };
  },

  async getReceptions(): Promise<ReceptionUsinage2Mouvement[]> {
    if (!isApiSource('receptionsU2')) {
      return testDataRepository.getReceptions().map(toReceptionMouvement);
    }
    const res = await api.get<PagedReceptionsApiResponse>('/v1/receptions-usinage2', {
      params: { page: 1, pageSize: 100 }
    });
    return res.data.items.map(toReceptionMouvementFromApi);
  },

  async createReception(data: ReceptionInput): Promise<ReceptionUsinage2Mouvement> {
    if (!isApiSource('receptionsU2')) {
      const dataset = testDataRepository.getDataset();
      const calculated = calculateReceptionWeight(dataset, {
        numeroOF: data.numeroOF,
        dateReception: data.dateReception,
        origine: data.origine,
        poidsTotalReceptionneKg: data.poidsTotalReceptionneKg,
        nombrePiecesReceptionnees: data.nombrePiecesReceptionnees
      });
      const created = testDataRepository.addReception({
        ...calculated,
        statut: 'Valide',
        source: 'SaisieManuelle',
        creePar: 'Responsable Usinage 2',
        creeLe: new Date().toISOString()
      });
      return toReceptionMouvement(created);
    }
    const res = await api.post<ReceptionUsinage2ApiResponse>('/v1/receptions-usinage2', data);
    return toReceptionMouvementFromApi(res.data, 0);
  },

  async deleteReception(id: number): Promise<void> {
    // Suppression physique reservee au mode mock : le backend n'expose pas de DELETE, seulement
    // valider/annuler (voir validerReception/annulerReception ci-dessous).
    testDataRepository.deleteReception(id);
  },

  async validerReception(apiId: string, derogationAdmin = false, derogationJustification?: string): Promise<void> {
    await api.post(`/v1/receptions-usinage2/${apiId}/valider`, {
      derogationAdmin,
      derogationJustification: derogationJustification ?? null
    });
  },

  async annulerReception(apiId: string): Promise<void> {
    await api.post(`/v1/receptions-usinage2/${apiId}/annuler`);
  },

};
