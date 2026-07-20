import { BUSINESS_RULES } from '@/config/businessRules';
import api from '@/services/api';
import {
  calculateBilanFonderie,
  calculateBilanUap2,
  calculateBilanUsinage2
} from '@/services/bilanCalculation.service';
import { testDataRepository } from '@/services/testDataRepository.service';
import type { CalculateBilanRequest, VerifyBilanRequest } from '@/types/api.types';
import type { BilanType } from '@/types/domain.types';
import type { RebutUap2Saisie, RebutUsinage2 } from '@/mocks/sopalTestDataset';
import type { BilanMatiereReport, BilanUap2ParOF, BilanUsinage2ParPiece } from './bilan.types';

type BilanResult = BilanMatiereReport | BilanUsinage2ParPiece | BilanUap2ParOF;
type ViewBilanType = 'fonderie' | 'usinage' | 'uap2';

const isMock = (): boolean => import.meta.env.VITE_USE_REAL_API !== 'true';

const toApiBilanType = (type: ViewBilanType): BilanType => (type === 'usinage' ? 'usinage2' : type);

const findMockBilan = (bilanKey: string, type: ViewBilanType): BilanResult | undefined => {
  const dataset = testDataRepository.getDataset();
  if (type === 'fonderie') {
    return calculateBilanFonderie(dataset).find((item) => item.bilanKey === bilanKey);
  }
  if (type === 'usinage') {
    return calculateBilanUsinage2(dataset).find((item) => item.bilanKey === bilanKey);
  }
  return calculateBilanUap2(dataset).find((item) => item.bilanKey === bilanKey);
};

export const bilanService = {
  async getBilans(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanMatiereReport[]> {
    return this.getBilansFonderie(periode, dateDebut, dateFin);
  },

  async calculerBilan(
    periode: string,
    codeTypeMatiere: string,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanMatiereReport[]> {
    return this.calculerBilanFonderie(periode, codeTypeMatiere, dateDebut, dateFin);
  },

  async getBilansFonderie(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanMatiereReport[]> {
    if (isMock()) return calculateBilanFonderie(testDataRepository.getDataset(), periode);
    const response = await api.get<BilanMatiereReport[]>('/bilans/fonderie', {
      params: { periode, dateDebut, dateFin }
    });
    return response.data;
  },

  async getBilansUsinage(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanUsinage2ParPiece[]> {
    if (isMock()) return calculateBilanUsinage2(testDataRepository.getDataset(), periode);
    const response = await api.get<BilanUsinage2ParPiece[]>('/bilans/usinage-2/par-piece', {
      params: { periode, dateDebut, dateFin }
    });
    return response.data;
  },

  async getBilansUap2(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanUap2ParOF[]> {
    if (isMock()) return calculateBilanUap2(testDataRepository.getDataset(), periode);
    const response = await api.get<BilanUap2ParOF[]>('/bilans/uap2/par-of', {
      params: { periode, dateDebut, dateFin }
    });
    return response.data;
  },

  async calculerBilanFonderie(
    periode: string,
    codeTypeMatiere: string,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanMatiereReport[]> {
    if (isMock()) {
      const bilans = calculateBilanFonderie(testDataRepository.getDataset(), periode);
      return codeTypeMatiere === 'TOUS' ? bilans : bilans.filter((item) => item.typeMatiere === codeTypeMatiere);
    }
    const request: CalculateBilanRequest = {
      periode,
      codeTypeMatiere: codeTypeMatiere === 'TOUS' ? undefined : codeTypeMatiere,
      dateDebut,
      dateFin
    };
    return (await api.post<BilanMatiereReport[]>('/bilans/fonderie/calculer', request)).data;
  },

  async calculerBilanUsinage(periode: string, dateDebut?: string, dateFin?: string): Promise<BilanUsinage2ParPiece[]> {
    if (isMock()) return this.getBilansUsinage(periode, dateDebut, dateFin);
    const request: CalculateBilanRequest = { periode, dateDebut, dateFin };
    return (await api.post<BilanUsinage2ParPiece[]>('/bilans/usinage-2/calculer', request)).data;
  },

  async calculerBilanUap2(periode: string, dateDebut?: string, dateFin?: string): Promise<BilanUap2ParOF[]> {
    if (isMock()) return this.getBilansUap2(periode, dateDebut, dateFin);
    const request: CalculateBilanRequest = { periode, dateDebut, dateFin };
    return (await api.post<BilanUap2ParOF[]>('/bilans/uap2/calculer', request)).data;
  },

  async createRebutUsinage2(
    data: Omit<RebutUsinage2, 'id' | 'statut' | 'creePar'>
  ): Promise<RebutUsinage2> {
    if (isMock()) {
      const current = testDataRepository.getRebutsUsinage2();
      const id = current.length ? Math.max(...current.map((item) => item.id)) + 1 : 1;
      const created: RebutUsinage2 = {
        ...data,
        id,
        statut: 'Valide',
        creePar: 'Responsable Usinage 2'
      };
      testDataRepository.saveRebutsUsinage2([created, ...current]);
      return created;
    }
    return (await api.post<RebutUsinage2>('/rebuts-usinage-2', data)).data;
  },

  async saveRebutsUap2(
    data: Omit<RebutUap2Saisie, 'id' | 'statut' | 'creePar'>
  ): Promise<RebutUap2Saisie> {
    if (isMock()) {
      const current = testDataRepository.getRebutsUap2();
      const periode = data.dateDeclaration.slice(0, 7);
      const previous = current.find(
        (item) => item.numeroOF === data.numeroOF && item.dateDeclaration.startsWith(periode)
      );
      const created: RebutUap2Saisie = {
        ...data,
        id: previous?.id ?? (current.length ? Math.max(...current.map((item) => item.id)) + 1 : 1),
        statut: 'Valide',
        creePar: 'Administrateur SOPAL'
      };
      testDataRepository.saveRebutsUap2([
        created,
        ...current.filter(
          (item) => !(item.numeroOF === data.numeroOF && item.dateDeclaration.startsWith(periode))
        )
      ]);
      return created;
    }
    return (await api.post<RebutUap2Saisie>('/rebuts-uap2', data)).data;
  },

  async verifierBilan(
    bilanKey: string,
    type: ViewBilanType,
    payload: Omit<VerifyBilanRequest, 'bilanType'>
  ): Promise<BilanResult> {
    if (isMock()) {
      const current = findMockBilan(bilanKey, type);
      if (!current) throw new Error('Bilan non trouvé');
      if (type === 'fonderie') {
        return {
          ...(current as BilanMatiereReport),
          statut: payload.statutControle as BilanMatiereReport['statut'],
          justification: payload.justification ?? (current as BilanMatiereReport).justification
        };
      }
      if (type === 'usinage') {
        const currentUsinage = current as BilanUsinage2ParPiece;
        return {
          ...currentUsinage,
          statutControle: payload.statutControle as BilanUsinage2ParPiece['statutControle'],
          observation: payload.observation ?? currentUsinage.observation
        };
      }
      const currentUap2 = current as BilanUap2ParOF;
      return {
        ...currentUap2,
        statut: payload.statutControle as BilanUap2ParOF['statut'],
        observation: payload.observation ?? currentUap2.observation
      };
    }
    const request: VerifyBilanRequest = { bilanType: toApiBilanType(type), ...payload };
    return (await api.post<BilanResult>(`/bilans/${encodeURIComponent(bilanKey)}/verifier`, request)).data;
  },

  async justifierBilan(bilanKey: string, type: ViewBilanType, justification: string): Promise<BilanResult> {
    if (isMock()) {
      const current = findMockBilan(bilanKey, type);
      if (!current) throw new Error('Bilan non trouvé');
      return type === 'fonderie'
        ? { ...(current as BilanMatiereReport), justification }
        : { ...current, observation: justification };
    }
    return (
      await api.post<BilanResult>(`/bilans/${encodeURIComponent(bilanKey)}/justifier`, {
        bilanType: toApiBilanType(type),
        justification
      })
    ).data;
  },

  async cloturerBilan(bilanKey: string, type: ViewBilanType): Promise<BilanResult> {
    if (isMock()) {
      const current = findMockBilan(bilanKey, type);
      if (!current) throw new Error('Bilan non trouvé');
      if (type === 'fonderie') return { ...(current as BilanMatiereReport), statut: 'Cloture' };
      if (type === 'usinage') {
        return { ...(current as BilanUsinage2ParPiece), statutControle: 'Regularise' };
      }
      return { ...(current as BilanUap2ParOF), statut: 'Cloture' };
    }
    return (
      await api.post<BilanResult>(`/bilans/${encodeURIComponent(bilanKey)}/cloturer`, {
        bilanType: toApiBilanType(type)
      })
    ).data;
  },

  async validerBilan(bilanKey: string): Promise<BilanMatiereReport> {
    return this.verifierBilan(bilanKey, 'fonderie', {
      statutControle: 'Valide'
    }) as Promise<BilanMatiereReport>;
  },

  async justifierEcart(bilanKey: string, justification: string): Promise<BilanMatiereReport> {
    return this.justifierBilan(bilanKey, 'fonderie', justification) as Promise<BilanMatiereReport>;
  }
};
