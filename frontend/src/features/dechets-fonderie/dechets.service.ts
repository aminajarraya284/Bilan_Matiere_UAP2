import api from '@/services/api';
import { dataSources } from '@/config/dataSources';
import { testDataRepository } from '@/services/testDataRepository.service';
import type { DechetFonderieTest, TypeMatiereCode } from '@/mocks/sopalTestDataset';
import type {
  DechetFonderiePayload,
  DechetFonderieResponse,
  DechetsFonderieFilters,
  DechetType,
  PagedDechetsFonderieResponse,
  ResumeDechetsFonderieResponse
} from './dechets.types';
import type { TypeMatiereResponse } from './typesMatiere.types';
import { getIsoWeek } from './dechets.utils';

const materialIds: Record<TypeMatiereCode, string> = {
  M3000L0: '30000000-0000-0000-0000-000000000001',
  M3010L0: '30100000-0000-0000-0000-000000000001',
  M3060L0: '30600000-0000-0000-0000-000000000001'
};

const mockWasteId = (id: number): string => `f0000000-0000-0000-0000-${String(id).padStart(12, '0')}`;
const parseMockWasteId = (id: string): number => Number(id.slice(-12));

const mockTypes = (): TypeMatiereResponse[] =>
  testDataRepository.getDataset().refTypesMatiere.map((item) => ({
    id: materialIds[item.codeTypeMatiere],
    code: item.codeTypeMatiere,
    libelle: item.libelle,
    estActif: item.actif,
    dateCreationUtc: '2026-01-01T00:00:00Z',
    dateModificationUtc: null
  }));

const toResponse = (item: DechetFonderieTest): DechetFonderieResponse => {
  const material = mockTypes().find((type) => type.code === item.codeTypeMatiere)!;
  return {
    id: mockWasteId(item.id),
    dateDeclaration: item.dateDeclaration,
    semaineIso: item.semaine,
    typeMatiereId: material.id,
    typeMatiereCode: material.code,
    typeMatiereLibelle: material.libelle,
    typeDechet: item.typeDechet,
    poidsKg: item.poidsDechetKg,
    observation: item.observation || null,
    statut: item.statut === 'Valide' ? 'Valide' : 'Annule',
    declarant: item.declarantNom,
    dateCreationUtc: item.creeLe,
    dateModificationUtc: null
  };
};

const mockPayload = (payload: DechetFonderiePayload, id: number, createdAt: string): DechetFonderieTest => {
  const material = mockTypes().find((type) => type.id === payload.typeMatiereId);
  if (!material?.estActif) throw new Error('Type matière actif introuvable.');
  return {
    id,
    dateDeclaration: payload.dateDeclaration,
    semaine: getIsoWeek(payload.dateDeclaration),
    codeTypeMatiere: material.code as TypeMatiereCode,
    typeDechet: payload.typeDechet,
    poidsDechetKg: payload.poidsKg,
    observation: payload.observation ?? '',
    declarantId: 'U-FONDERIE',
    declarantNom: 'Responsable Fonderie',
    statut: 'Valide',
    creeLe: createdAt
  };
};

export const dechetsService = {
  async getTypesMatiere(): Promise<TypeMatiereResponse[]> {
    if (dataSources.typesMatiere === 'mock') return mockTypes().filter((item) => item.estActif);
    return (await api.get<TypeMatiereResponse[]>('/v1/types-matiere', { params: { actif: true } })).data;
  },

  async getDechetsFonderie(filters: DechetsFonderieFilters): Promise<PagedDechetsFonderieResponse> {
    if (dataSources.dechetsFonderie === 'api') {
      return (await api.get<PagedDechetsFonderieResponse>('/v1/dechets-fonderie', { params: filters })).data;
    }

    const page = filters.page ?? 1;
    const pageSize = filters.pageSize ?? 20;
    const filtered = testDataRepository
      .getDataset()
      .dechetsFonderie.map(toResponse)
      .filter((item) => item.dateDeclaration.startsWith(filters.periode))
      .filter((item) => !filters.typeMatiereId || item.typeMatiereId === filters.typeMatiereId)
      .filter((item) => !filters.typeDechet || item.typeDechet === filters.typeDechet)
      .sort(
        (left, right) =>
          right.dateDeclaration.localeCompare(left.dateDeclaration) ||
          right.dateCreationUtc.localeCompare(left.dateCreationUtc)
      );
    return {
      items: filtered.slice((page - 1) * pageSize, page * pageSize),
      total: filtered.length,
      page,
      pageSize
    };
  },

  async getDechetFonderieById(id: string): Promise<DechetFonderieResponse> {
    if (dataSources.dechetsFonderie === 'api') {
      return (await api.get<DechetFonderieResponse>(`/v1/dechets-fonderie/${id}`)).data;
    }
    const item = testDataRepository
      .getDataset()
      .dechetsFonderie.find((candidate) => candidate.id === parseMockWasteId(id));
    if (!item) throw new Error('Déchet Fonderie introuvable.');
    return toResponse(item);
  },

  async createDechetFonderie(payload: DechetFonderiePayload): Promise<DechetFonderieResponse> {
    if (dataSources.dechetsFonderie === 'api') {
      return (await api.post<DechetFonderieResponse>('/v1/dechets-fonderie', payload)).data;
    }
    const current = testDataRepository.getDataset().dechetsFonderie;
    const nextId = current.length ? Math.max(...current.map((item) => item.id)) + 1 : 1;
    const created = mockPayload(payload, nextId, new Date().toISOString());
    testDataRepository.saveDechets([created, ...current]);
    return toResponse(created);
  },

  async updateDechetFonderie(id: string, payload: DechetFonderiePayload): Promise<DechetFonderieResponse> {
    if (dataSources.dechetsFonderie === 'api') {
      return (await api.put<DechetFonderieResponse>(`/v1/dechets-fonderie/${id}`, payload)).data;
    }
    const current = testDataRepository.getDataset().dechetsFonderie;
    const index = current.findIndex((item) => item.id === parseMockWasteId(id));
    if (index < 0) throw new Error('Déchet Fonderie introuvable.');
    current[index] = mockPayload(payload, current[index].id, current[index].creeLe);
    testDataRepository.saveDechets(current);
    return toResponse(current[index]);
  },

  async deleteDechetFonderie(id: string): Promise<void> {
    if (dataSources.dechetsFonderie === 'api') {
      await api.delete(`/v1/dechets-fonderie/${id}`);
      return;
    }
    const current = testDataRepository.getDataset().dechetsFonderie;
    testDataRepository.saveDechets(current.filter((item) => item.id !== parseMockWasteId(id)));
  },

  async getResumeDechetsFonderie(periode: string): Promise<ResumeDechetsFonderieResponse> {
    if (dataSources.dechetsFonderie === 'api') {
      return (await api.get<ResumeDechetsFonderieResponse>('/v1/dechets-fonderie/resume', { params: { periode } }))
        .data;
    }

    const items = testDataRepository
      .getDataset()
      .dechetsFonderie.map(toResponse)
      .filter((item) => item.dateDeclaration.startsWith(periode));
    const totals = mockTypes().map((material) => {
      const materialItems = items.filter((item) => item.typeMatiereId === material.id);
      const sum = (kind: DechetType) =>
        materialItems.filter((item) => item.typeDechet === kind).reduce((total, item) => total + item.poidsKg, 0);
      const totalBavureKg = sum('Bavure');
      const totalPoudreKg = sum('Poudre');
      return {
        typeMatiereId: material.id,
        typeMatiereCode: material.code,
        totalBavureKg,
        totalPoudreKg,
        totalKg: totalBavureKg + totalPoudreKg
      };
    });
    return {
      periode,
      totauxParTypeMatiere: totals,
      totalGeneralKg: totals.reduce((total, item) => total + item.totalKg, 0)
    };
  }
};
