import { BUSINESS_RULES } from '@/config/businessRules';
import api from '@/services/api';
import {
  calculateBilanFonderie,
  calculateBilanUap2,
  calculateBilanUsinage2
} from '@/services/bilanCalculation.service';
import { testDataRepository } from '@/services/testDataRepository.service';
import type { CalculateBilanRequest, VerifyBilanRequest } from '@/types/api.types';
import type { RebutUap2Saisie, RebutUsinage2 } from '@/mocks/sopalTestDataset';
import type {
  BilanMatiereReport,
  BilanUap2ParOF,
  BilanUsinage2Api,
  BilanUsinage2Ligne,
  BilanUsinage2ParPiece
} from './bilan.types';
import { isMockSource } from '@/config/dataSources';

type BilanResult = BilanMatiereReport | BilanUsinage2ParPiece | BilanUap2ParOF;
type ViewBilanType = 'fonderie' | 'usinage' | 'uap2';

// Chaque bilan a sa propre source : Fonderie et UAP2 n'ont pas d'endpoint backend,
// contrairement au Bilan Usinage 2 qui est calcule server-side.
const isMockFonderie = (): boolean => isMockSource('bilanFonderie');
const isMockUap2 = (): boolean => isMockSource('bilanUap2');

// Adaptation du jeu de demonstration vers le contrat reel du Bilan Usinage 2.
// Les champs absents du mock restent null : aucune valeur n'est fabriquee.
const toRealBilanShape = (item: BilanUsinage2ParPiece, periode: string): BilanUsinage2Ligne => ({
  numeroOF: item.numeroOF,
  codeArticle: item.codeArticle,
  designationArticle: item.designationArticle ?? null,
  atelierOrigine: null,
  typeMatiere: item.codeTypeMatiere,
  periode,

  quantiteCtrlEmrPieces: item.quantiteEmrPieces,
  rebutEmrPieces: item.quantiteRebutEmrX3Pieces,
  nombreDePieces: item.quantiteEmrNetteX3Pieces,
  quantiteReceptionneePieces: item.quantiteReceptionneeU2Pieces,
  quantiteNonReceptionneePieces: item.quantiteNonReceptionneePieces,
  rebutUsinage2Pieces: item.rebutUsinage2Pieces ?? 0,
  entreeOfPieces: item.entreeOfPieces ?? null,
  bilanUsinage2Pieces:
    item.entreeOfPieces === undefined || item.entreeOfPieces === null
      ? null
      : item.quantiteReceptionneeU2Pieces - (item.rebutUsinage2Pieces ?? 0) - item.entreeOfPieces,

  tauxReceptionPourcent: null,
  tauxNonReceptionPourcent: null,

  poidsReceptionneTotalKg: null,
  poidsPhysiqueUnitaireKg: null,
  poidsTheoriqueUnitaireKg: null,
  ecartPoidsUnitaireKg: null,
  tauxEcartPoidsPourcent: null,

  statutReception: item.statutControle === 'OK' ? 'OK' : 'AVerifier',
  statutPoids: 'DonneesManquantes',
  anomalies: []
});

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
    if (isMockFonderie()) return calculateBilanFonderie(testDataRepository.getDataset(), periode);
    const response = await api.get<BilanMatiereReport[]>('/bilans/fonderie', {
      params: { periode, dateDebut, dateFin }
    });
    return response.data;
  },

  async getBilansUsinage(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    _dateDebut?: string,
    _dateFin?: string
  ): Promise<BilanUsinage2Api> {
    if (isMockSource('bilanU2')) {
      const lignes = calculateBilanUsinage2(testDataRepository.getDataset(), periode).map((item) =>
        toRealBilanShape(item, periode)
      );
      return {
        periode,
        lignes,
        quantiteTotaleReceptionneePieces: lignes.reduce((total, ligne) => total + ligne.quantiteReceptionneePieces, 0),
        nombreLignes: lignes.length,
        nombreLignesDonneesManquantes: lignes.filter(
          (ligne) => ligne.statutReception === 'DonneesManquantes' || ligne.statutPoids === 'DonneesManquantes'
        ).length
      };
    }
    const response = await api.get<BilanUsinage2Api>(`/v1/bilans/usinage2/${encodeURIComponent(periode)}`);
    return response.data;
  },

  async getBilansUap2(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanUap2ParOF[]> {
    if (isMockUap2()) return calculateBilanUap2(testDataRepository.getDataset(), periode);
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
    if (isMockFonderie()) {
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

  async calculerBilanUsinage(periode: string, dateDebut?: string, dateFin?: string): Promise<BilanUsinage2Api> {
    return this.getBilansUsinage(periode, dateDebut, dateFin);
  },

  async calculerBilanUap2(periode: string, dateDebut?: string, dateFin?: string): Promise<BilanUap2ParOF[]> {
    if (isMockUap2()) return this.getBilansUap2(periode, dateDebut, dateFin);
    const request: CalculateBilanRequest = { periode, dateDebut, dateFin };
    return (await api.post<BilanUap2ParOF[]>('/bilans/uap2/calculer', request)).data;
  },

  // ---------------------------------------------------------------------------
  // Saisies et workflow de contrôle ci-dessous : AUCUN endpoint backend n'existe,
  // quel que soit le bilan concerné (y compris le Bilan U2, dont seul le calcul en
  // lecture est exposé par `GET /v1/bilans/usinage2/{periode}`). Ces opérations
  // restent donc en données de démonstration. Les appels API correspondants ont
  // été retirés : ils visaient des routes inexistantes (`/rebuts-usinage-2`,
  // `/rebuts-uap2`, `/bilans/{cle}/verifier|justifier|cloturer`) et produisaient
  // systématiquement des 404. Ils seront réintroduits avec les contrôleurs réels.
  // ---------------------------------------------------------------------------

  async createRebutUsinage2(data: Omit<RebutUsinage2, 'id' | 'statut' | 'creePar'>): Promise<RebutUsinage2> {
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
  },

  async saveRebutsUap2(data: Omit<RebutUap2Saisie, 'id' | 'statut' | 'creePar'>): Promise<RebutUap2Saisie> {
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
      ...current.filter((item) => !(item.numeroOF === data.numeroOF && item.dateDeclaration.startsWith(periode)))
    ]);
    return created;
  },

  async verifierBilan(
    bilanKey: string,
    type: ViewBilanType,
    payload: Omit<VerifyBilanRequest, 'bilanType'>
  ): Promise<BilanResult> {
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
  },

  async justifierBilan(bilanKey: string, type: ViewBilanType, justification: string): Promise<BilanResult> {
    const current = findMockBilan(bilanKey, type);
    if (!current) throw new Error('Bilan non trouvé');
    return type === 'fonderie'
      ? { ...(current as BilanMatiereReport), justification }
      : { ...current, observation: justification };
  },

  async cloturerBilan(bilanKey: string, type: ViewBilanType): Promise<BilanResult> {
    const current = findMockBilan(bilanKey, type);
    if (!current) throw new Error('Bilan non trouvé');
    if (type === 'fonderie') return { ...(current as BilanMatiereReport), statut: 'Cloture' };
    if (type === 'usinage') {
      return { ...(current as BilanUsinage2ParPiece), statutControle: 'Regularise' };
    }
    return { ...(current as BilanUap2ParOF), statut: 'Cloture' };
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
