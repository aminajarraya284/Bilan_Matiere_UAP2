import api from '@/services/api';
import {
  ControleNonReception,
  ReceptionUsinage2Mouvement,
  VerificationControleNonReceptionPayload
} from './receptions.types';
import { testDataRepository } from '@/services/testDataRepository.service';
import {
  calculateNonReceivedQuantities,
  calculateReceptionWeight,
  buildReceptionDefaults
} from '@/services/bilanCalculation.service';
import type { ReceptionUsinage2Test } from '@/mocks/sopalTestDataset';
import type { CreateReceptionU2Request, ReceptionU2DefaultsResponse } from '@/types/api.types';

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

const toControle = (item: ReturnType<typeof calculateNonReceivedQuantities>[number]): ControleNonReception => ({
  bilanKey: item.bilanKey,
  numeroOF: item.numeroOF,
  codeArticle: item.codeArticle,
  designationArticle: item.designationArticle,
  codeTypeMatiere: item.codeTypeMatiere,
  quantiteEmrPieces: item.quantiteEmrPieces,
  quantiteReceptionneeU2Pieces: item.quantiteReceptionneeU2Pieces,
  quantiteNonReceptionneePieces: item.quantiteNonReceptionneePieces,
  poidsNonReceptionneEstimeKg: item.poidsNonReceptionneEstimeKg,
  statutControle: item.statutControle,
  observation: item.observation,
  justification: '',
  dateSynchronisationX3: new Date().toISOString(),
  sourceEmr: 'RapportJournalierMachine'
});

export type ReceptionInput = CreateReceptionU2Request;

export const receptionsService = {
  async getReceptionDefaults(numeroOF: string): Promise<ReceptionU2DefaultsResponse> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return buildReceptionDefaults(testDataRepository.getDataset(), numeroOF);
    }
    const res = await api.get(`/receptions-usinage-2/defaults/${encodeURIComponent(numeroOF)}`);
    return res.data;
  },

  async getReceptions(): Promise<ReceptionUsinage2Mouvement[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return testDataRepository.getReceptions().map(toReceptionMouvement);
    }
    const res = await api.get<ReceptionUsinage2Mouvement[]>('/receptions-usinage-2');
    return res.data;
  },

  async createReception(data: ReceptionInput): Promise<ReceptionUsinage2Mouvement> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
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
    const res = await api.post<ReceptionUsinage2Mouvement>('/receptions-usinage-2', data);
    return res.data;
  },

  async deleteReception(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      testDataRepository.deleteReception(id);
      return;
    }
    await api.delete(`/receptions-usinage-2/${id}`);
  },

  async triggerSynchroniser(): Promise<{ count: number }> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const dataset = testDataRepository.synchronizeX3();
      return { count: dataset.x3OrdresFabrication.length };
    }
    const res = await api.post<{ count: number }>('/receptions-usinage-2/synchroniser');
    return res.data;
  },

  async getControleNonReception(): Promise<ControleNonReception[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return calculateNonReceivedQuantities(testDataRepository.getDataset()).map(toControle);
    }
    const res = await api.get<ControleNonReception[]>('/receptions-usinage-2/controle-non-reception');
    return res.data;
  },

  async verifierControleNonReception(
    bilanKey: string,
    payload: VerificationControleNonReceptionPayload
  ): Promise<ControleNonReception> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = (await this.getControleNonReception()).find((item) => item.bilanKey === bilanKey);
      if (!current) throw new Error('Controle non reception introuvable');
      return {
        ...current,
        statutControle: payload.statutControle,
        observation: payload.commentaireControle || current.observation,
        justification: payload.justification || current.justification
      };
    }
    const res = await api.post<ControleNonReception>(
      `/receptions-usinage-2/controle-non-reception/${encodeURIComponent(bilanKey)}/verifier`,
      payload
    );
    return res.data;
  },

  async justifierControleNonReception(
    bilanKey: string,
    payload: VerificationControleNonReceptionPayload
  ): Promise<ControleNonReception> {
    return this.verifierControleNonReception(bilanKey, payload);
  }
};
