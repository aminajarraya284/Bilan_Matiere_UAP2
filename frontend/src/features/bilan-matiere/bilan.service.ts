import { BUSINESS_RULES } from '@/config/businessRules';
import api from '@/services/api';
import {
  calculateBilanFonderie,
  calculateBilanUap2,
  calculateBilanUsinage2
} from '@/services/bilanCalculation.service';
import { testDataRepository } from '@/services/testDataRepository.service';
import type {
  BilanFonderieApiLigne,
  BilanFonderieApiResponse,
  BilanUap2ApiResponse
} from '@/types/api.types';
import type { RebutUap2Saisie, RebutUsinage2 } from '@/mocks/sopalTestDataset';
import type {
  BilanMatiereReport,
  BilanUap2ParOF,
  BilanUsinage2Api,
  BilanUsinage2Ligne,
  BilanUsinage2ParPiece
} from './bilan.types';
import { assertApiSource, isMockSource } from '@/config/dataSources';

// Chaque bilan dispose d'une API de calcul en lecture et d'une source configurable.
const isMockFonderie = (): boolean => isMockSource('bilanFonderie');
const isMockUap2 = (): boolean => isMockSource('bilanUap2');

/**
 * L'ancien contrat de demonstration du Bilan UAP2 exprime tout en kilogrammes et porte
 * un terme « entrees semi-finies » absent de la regle reelle (RM-UAP2-002). L'API, elle,
 * calcule l'ecart EN PIECES. Les deux ne sont pas convertibles l'un dans l'autre sans
 * fabriquer des valeurs : en mode `api`, les ecrans doivent appeler `getBilanUap2Reel`.
 */
const BILAN_UAP2_ANCIEN_CONTRAT =
  "L'ancien contrat du Bilan UAP2 (tout en kg) n'est pas alimenté par l'API, qui calcule " +
  "l'écart en pièces. Utilisez getBilanUap2Reel(). Voir docs/ReglesMetier.md, RM-UAP2-002.";

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

/**
 * Adapte une ligne de `GET /v1/bilans/fonderie/{periode}` au contrat d'ecran.
 * Les `null` sont conserves tels quels : une composante inconnue ne devient pas zero
 * (RM-COM-003), sans quoi l'ecart affiche serait faux sans que rien ne le signale.
 */
const toBilanFonderieReport = (ligne: BilanFonderieApiLigne): BilanMatiereReport => ({
  bilanKey: `${ligne.periode}|${ligne.codeTypeMatiere}`,
  periode: ligne.periode,
  typeMatiere: ligne.codeTypeMatiere,
  encoursDebut: ligne.stockDebutKg,
  sortiesX3: ligne.sortiesMagasinX3Kg,
  inputTotal: ligne.inputKg,
  encoursFinReel: ligne.stockFinKg,
  dechetsFonderie: ligne.dechetsFonderieKg,
  receptionsUsinage2: ligne.receptionsUsinage2Kg,
  // Sans endpoint dedie, ces deux mesures ne sont pas calculees cote serveur.
  poidsNonReceptionne: 0,
  nombrePiecesNonReceptionnees: 0,
  outputTotal: ligne.outputKg,
  ecartKg: ligne.ecartKg,
  tauxEcart: ligne.tauxEcartPourcent,
  // La complétude des données ne constitue pas une validation métier.
  statut: ligne.statut,
  dateCalcul: new Date().toISOString(),
  anomalies: ligne.anomalies
});

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
    if (dateDebut !== undefined || dateFin !== undefined) {
      assertApiSource('bilanFonderie');
      const response = await api.get<{ dateDebut: string; dateFin: string; bilan: BilanFonderieApiResponse }>(
        '/v1/bilans/fonderie', { params: { dateDebut, dateFin } }
      );
      return response.data.bilan.lignes.map(toBilanFonderieReport);
    }
    if (isMockFonderie()) return calculateBilanFonderie(testDataRepository.getDataset(), periode);
    const response = await api.get<BilanFonderieApiResponse>(
      `/v1/bilans/fonderie/${encodeURIComponent(periode)}`
    );
    return response.data.lignes.map(toBilanFonderieReport);
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
        ).length,
        // En mode mock, `lignes` contient toute la periode : les agregats se calculent ici,
        // la ou l'API reelle les fournit deja calcules sur l'ensemble des lignes.
        totalRebutUsinage2Pieces: lignes.reduce((total, ligne) => total + ligne.rebutUsinage2Pieces, 0),
        totalEntreeOfPieces: lignes.reduce((total, ligne) => total + (ligne.entreeOfPieces ?? 0), 0),
        // Une seule ligne non calculable rend le total indisponible : jamais un faux zero.
        totalBilanUsinage2Pieces: lignes.some((ligne) => ligne.bilanUsinage2Pieces === null)
          ? null
          : lignes.reduce((total, ligne) => total + (ligne.bilanUsinage2Pieces ?? 0), 0),
        nombreAnomalies: lignes.reduce((total, ligne) => total + ligne.anomalies.length, 0),
        total: lignes.length,
        page: 1,
        pageSize: lignes.length
      };
    }
    const response = await api.get<BilanUsinage2Api>(`/v1/bilans/usinage2/${encodeURIComponent(periode)}`);
    return response.data;
  },

  async getBilansUap2(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    _dateDebut?: string,
    _dateFin?: string
  ): Promise<BilanUap2ParOF[]> {
    if (isMockUap2()) return calculateBilanUap2(testDataRepository.getDataset(), periode);
    throw new Error(BILAN_UAP2_ANCIEN_CONTRAT);
  },

  /**
   * Bilan UAP2 reel (RM-UAP2-001 et RM-UAP2-002). Renvoie le contrat de l'API tel quel :
   * l'ecart y est EN PIECES, ce que l'ancien contrat de demonstration ne sait pas
   * representer. Les ecrans qui l'affichent doivent lire `BilanUap2ApiResponse`.
   */
  async getBilanUap2Reel(periode: string = BUSINESS_RULES.periodeParDefaut): Promise<BilanUap2ApiResponse> {
    // Le routage des sources reste pilote par dataSources, jamais contourne.
    assertApiSource('bilanUap2');
    return (await api.get<BilanUap2ApiResponse>(`/v1/bilans/uap2/${encodeURIComponent(periode)}`)).data;
  },

  /**
   * Le bilan est recalcule a chaque lecture cote serveur : il n'existe pas d'endpoint
   * de calcul separe. Le filtre par type matiere est applique ici, sur le resultat.
   */
  async getPeriodesUap2Disponibles(): Promise<string[]> {
    assertApiSource('bilanUap2');
    return (await api.get<string[]>('/v1/bilans/uap2/periodes')).data;
  },

  async calculerBilanFonderie(
    periode: string,
    codeTypeMatiere: string,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanMatiereReport[]> {
    const bilans = await this.getBilansFonderie(periode, dateDebut, dateFin);
    return codeTypeMatiere === 'TOUS' ? bilans : bilans.filter((item) => item.typeMatiere === codeTypeMatiere);
  },

  async calculerBilanUsinage(periode: string, dateDebut?: string, dateFin?: string): Promise<BilanUsinage2Api> {
    return this.getBilansUsinage(periode, dateDebut, dateFin);
  },

  async calculerBilanUap2(periode: string, dateDebut?: string, dateFin?: string): Promise<BilanUap2ParOF[]> {
    if (isMockUap2()) return this.getBilansUap2(periode, dateDebut, dateFin);
    throw new Error(BILAN_UAP2_ANCIEN_CONTRAT);
  },

  // Anciennes saisies du jeu de démonstration uniquement. Les saisies U2 réelles
  // passent par rebutsUsinage2.service.ts et son API dédiée.

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
  }
};
