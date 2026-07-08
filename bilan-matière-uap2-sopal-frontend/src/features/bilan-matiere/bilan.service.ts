import api from '@/services/api';
import { BUSINESS_RULES } from '@/config/businessRules';
import { BilanMatiereReport, BilanUsinage2ParPiece, BilanUap2ParOF } from './bilan.types';
import type { BilanStatus } from '@/types/common.types';
import { storage } from '@/utils/storage';
import { mergeBilansForPeriod } from './bilanMerge';

type PoidsNonReceptionnePayload = {
  poidsNonReceptionne: number;
  nombrePiecesNonReceptionnees: number;
  utilisateur: string;
  role: string;
  dateDebut?: string;
  dateFin?: string;
};

type BilanWithId = { id?: number };
type UsinageStatus = BilanUsinage2ParPiece['statutControle'];
type Uap2Status = BilanUap2ParOF['statut'];
type BilanResult = BilanMatiereReport | BilanUsinage2ParPiece | BilanUap2ParOF;

const DEFAULT_TYPE_MATIERES: string[] = [...BUSINESS_RULES.typeMatieresParDefaut];
const BILAN_STATUSES: BilanStatus[] = ['Brouillon', 'EnControle', 'AvecEcart', 'Valide', 'Cloture'];
const USINAGE_STATUSES: UsinageStatus[] = ['OK', 'AVerifier', 'Anomalie', 'Justifie', 'Regularise'];
const UAP2_STATUSES: Uap2Status[] = ['OK', 'AVerifier', 'Anomalie', 'Justifie', 'Regularise', 'Cloture'];

const getInitialEncoursDebut = (typeMatiere: string): number => {
  return BUSINESS_RULES.encoursDebutParTypeMatiere[typeMatiere] ?? 0;
};

const normalizePoids = (value: number): number => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.max(0, parsed) : 0;
};

const normalizePieces = (value: number): number => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.max(0, Math.trunc(parsed)) : 0;
};

const formatAuditDate = (date = new Date()): string => {
  const pad = (part: number) => String(part).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
};

const toBilanStatus = (status: string): BilanStatus => {
  return BILAN_STATUSES.includes(status as BilanStatus) ? (status as BilanStatus) : 'EnControle';
};

const toUsinageStatus = (status: string): UsinageStatus => {
  return USINAGE_STATUSES.includes(status as UsinageStatus) ? (status as UsinageStatus) : 'AVerifier';
};

const toUap2Status = (status: string): Uap2Status => {
  return UAP2_STATUSES.includes(status as Uap2Status) ? (status as Uap2Status) : 'AVerifier';
};

const getAllTypeMatieres = (existingBilans: BilanMatiereReport[]): string[] => {
  const codes = new Set<string>(DEFAULT_TYPE_MATIERES);

  existingBilans.forEach((b) => codes.add(b.typeMatiere));
  storage.getSortiesX3().forEach((s) => codes.add(s.codeMatiere));
  storage.getDechets().forEach((d) => codes.add(d.codeTypeMatiere));
  storage.getReceptions().forEach((r) => codes.add(r.codeTypeMatiere));
  storage.getConteneurs().forEach((c) => codes.add(c.codeTypeMatiere));
  storage.getCategories().forEach((c) => codes.add(c.codeTypeMatiere));

  return Array.from(codes)
    .filter(Boolean)
    .sort((a, b) => {
      const aDefaultIndex = DEFAULT_TYPE_MATIERES.indexOf(a);
      const bDefaultIndex = DEFAULT_TYPE_MATIERES.indexOf(b);
      if (aDefaultIndex !== -1 || bDefaultIndex !== -1) {
        return (
          (aDefaultIndex === -1 ? Number.MAX_SAFE_INTEGER : aDefaultIndex) -
          (bDefaultIndex === -1 ? Number.MAX_SAFE_INTEGER : bDefaultIndex)
        );
      }
      return a.localeCompare(b);
    });
};

const allocateBilanId = (existing: BilanWithId | undefined, usedIds: Set<number>): number => {
  if (existing?.id) {
    usedIds.add(existing.id);
    return existing.id;
  }

  let nextId = usedIds.size ? Math.max(...Array.from(usedIds)) + 1 : 1;
  while (usedIds.has(nextId)) {
    nextId++;
  }
  usedIds.add(nextId);
  return nextId;
};

const saveBilansForPeriod = (
  currentBilans: BilanMatiereReport[],
  periode: string,
  periodBilans: BilanMatiereReport[]
): BilanMatiereReport[] => {
  const merged = mergeBilansForPeriod(currentBilans, periode, periodBilans);
  storage.saveBilans(merged);
  return merged;
};

// Main mathematical balance calculation function
const computeBilanForPeriodAndMatiere = (
  periode: string,
  typeMatiere: string,
  existing?: BilanMatiereReport,
  dateDebut?: string,
  dateFin?: string,
  id?: number
): BilanMatiereReport => {
  const dDeb = dateDebut || `${periode}-01`;
  const dFin = dateFin || `${periode}-31`;

  const sorties = storage.getSortiesX3();
  const dechets = storage.getDechets();
  const receptions = storage.getReceptions();
  const conteneurs = storage.getConteneurs();
  const categories = storage.getCategories();

  // 1. Encours début
  const encoursDebut = getInitialEncoursDebut(typeMatiere);

  // 2. Sorties X3
  const sortiesX3 = sorties
    .filter((s) => s.codeMatiere === typeMatiere && s.dateImputation >= dDeb && s.dateImputation <= dFin)
    .reduce((sum, s) => sum + s.quantiteUtilisee, 0);

  const inputTotal = encoursDebut + sortiesX3;

  // 3. Encours fin réel (validated inventory items)
  const startYM = dDeb.slice(0, 7);
  const endYM = dFin.slice(0, 7);

  const totalP1 = conteneurs
    .filter((c) => {
      const matchMatiere = c.codeTypeMatiere === typeMatiere;
      const matchStatut = c.statut === 'Valide';
      if (c.dateInventaire) {
        return matchMatiere && matchStatut && c.dateInventaire >= dDeb && c.dateInventaire <= dFin;
      }
      const catYM = `${c.annee}-${c.mois.toString().padStart(2, '0')}`;
      return matchMatiere && matchStatut && catYM >= startYM && catYM <= endYM;
    })
    .reduce((sum, c) => sum + c.quantiteReelle, 0);

  const totalP2 = categories
    .filter((cat) => {
      const matchMatiere = cat.codeTypeMatiere === typeMatiere;
      const matchStatut = cat.statut === 'Valide';
      const includedInBilan = cat.inclusDansBilanFonderie === true;
      const catYM = `${cat.annee}-${cat.mois.toString().padStart(2, '0')}`;
      return matchMatiere && matchStatut && includedInBilan && catYM >= startYM && catYM <= endYM;
    })
    .reduce((sum, cat) => sum + cat.quantiteReelle, 0);

  const encoursFinReel = totalP1 + totalP2;

  // 4. Déchets fonderie
  const dechetsFonderie = dechets
    .filter(
      (d) =>
        d.codeTypeMatiere === typeMatiere &&
        d.dateDeclaration >= dDeb &&
        d.dateDeclaration <= dFin &&
        d.statut === 'Valide'
    )
    .reduce((sum, d) => sum + d.quantiteKg, 0);

  // 5. Réceptions usinage 2
  const receptionsUsinage2 = receptions
    .filter((r) => r.codeTypeMatiere === typeMatiere && r.dateReception >= dDeb && r.dateReception <= dFin)
    .reduce((sum, r) => sum + r.poidsKg, 0);

  const poidsNonReceptionne = normalizePoids(existing?.poidsNonReceptionne ?? 0);
  const nombrePiecesNonReceptionnees = normalizePieces(existing?.nombrePiecesNonReceptionnees ?? 0);
  const outputTotal = encoursFinReel + dechetsFonderie + receptionsUsinage2 + poidsNonReceptionne;

  // 6. Écarts et Taux
  const ecartKg = inputTotal - outputTotal;
  const tauxEcart = inputTotal > 0 ? (ecartKg / inputTotal) * 100 : 0;

  // Status determination
  let statut: BilanMatiereReport['statut'] = 'Valide';
  if (existing?.statut) {
    statut = existing.statut;
  } else if (Math.abs(tauxEcart) > BUSINESS_RULES.seuilEcartBilanAutoPourcentage) {
    statut = BUSINESS_RULES.statutBilanAvecEcart;
  }

  // Pre-seed default justification if empty and has variance
  let justification = existing?.justification || '';
  if (!justification && statut === 'AvecEcart') {
    justification =
      typeMatiere === 'M3010L0'
        ? "Écart de pesée sur le conteneur 702 en cours d'analyse."
        : 'Écart mineur conforme aux tolérances usine.';
  } else if (!justification && statut === 'Valide') {
    justification = 'Écart conforme aux tolérances usine.';
  }

  return {
    id: id ?? existing?.id ?? 0,
    periode,
    typeMatiere,
    encoursDebut,
    sortiesX3,
    inputTotal,
    encoursFinReel,
    dechetsFonderie,
    receptionsUsinage2,
    poidsNonReceptionne,
    nombrePiecesNonReceptionnees,
    outputTotal,
    ecartKg,
    tauxEcart,
    statut,
    justification,
    dateCalcul: new Date().toISOString().split('T')[0]
  };
};

// Mock Usinage 2 calculation function
const computeBilanUsinageForPeriodAndArticle = (
  periode: string,
  codeArticle: string,
  existing?: BilanUsinage2ParPiece,
  dateDebut?: string,
  dateFin?: string,
  id?: number
): BilanUsinage2ParPiece => {
  const dDeb = dateDebut || `${periode}-01`;
  const dFin = dateFin || `${periode}-31`;

  const receptions = storage.getReceptions().filter(
    (r) => r.codeArticle === codeArticle && r.dateReception >= dDeb && r.dateReception <= dFin
  );

  const quantiteReceptionneeU2 = receptions.reduce((sum, r) => sum + r.poidsKg, 0);
  const nombrePieces = receptions.reduce((sum, r) => sum + r.nombrePieces, 0);

  const quantiteEmrX3 = existing?.quantiteEmrX3 ?? (quantiteReceptionneeU2 > 0 ? Number((quantiteReceptionneeU2 * 1.0025).toFixed(2)) : 0);
  const quantiteNonReceptionnee = Number((quantiteEmrX3 - quantiteReceptionneeU2).toFixed(2));
  const ecartParPiece = nombrePieces > 0 ? Number((quantiteNonReceptionnee / nombrePieces).toFixed(6)) : 0;

  const designationArticle = receptions[0]?.designationArticle || existing?.designationArticle || 'Article ' + codeArticle;
  const codeTypeMatiere = receptions[0]?.codeTypeMatiere || existing?.codeTypeMatiere || 'M3010L0';

  return {
    id: id ?? existing?.id ?? 0,
    codeArticle,
    designationArticle,
    codeTypeMatiere,
    nombrePieces,
    quantiteEmrX3,
    quantiteReceptionneeU2,
    quantiteNonReceptionnee,
    ecartParPiece,
    statutControle: existing?.statutControle ?? 'OK',
    observation: existing?.observation || 'Calculé automatiquement',
    periode,
    dateCalcul: new Date().toISOString().split('T')[0]
  };
};

// Mock UAP2 OF calculation function
const computeBilanUap2ForPeriodAndOF = (
  periode: string,
  numOf: string,
  existing?: BilanUap2ParOF,
  dateDebut?: string,
  dateFin?: string,
  id?: number
): BilanUap2ParOF => {
  const dDeb = dateDebut || `${periode}-01`;
  const dFin = dateFin || `${periode}-31`;

  const receptions = storage.getReceptions().filter(
    (r) => r.numOf === numOf && r.dateReception >= dDeb && r.dateReception <= dFin
  );

  const quantiteReceptionneeU2 = receptions.reduce((sum, r) => sum + r.poidsKg, 0);
  const qteCtrl = receptions.reduce((sum, r) => sum + r.nombrePieces, 0);

  const codeArticle = receptions[0]?.codeArticle || existing?.codeArticle || 'ART-00912';
  const designationArticle = receptions[0]?.designationArticle || existing?.designationArticle || 'Corps Robinet 1/2';
  const codeTypeMatiere = receptions[0]?.codeTypeMatiere || existing?.codeTypeMatiere || 'M3010L0';

  const poidsPhy = existing?.poidsPhy ?? (codeArticle === 'ART-00984' ? 0.18 : codeArticle === 'ART-01005' ? 0.65 : 0.42);
  const poidsControle = Number((qteCtrl * poidsPhy).toFixed(2));

  const quantiteSortieMP = existing?.quantiteSortieMP ?? (quantiteReceptionneeU2 > 0 ? Number((quantiteReceptionneeU2 * 1.08).toFixed(2)) : 0);
  const rebutEMR = existing?.rebutEMR ?? (quantiteSortieMP > 0 ? Number((quantiteSortieMP * 0.01).toFixed(2)) : 0);
  const rebutSCI = existing?.rebutSCI ?? (quantiteSortieMP > 0 ? Number((quantiteSortieMP * 0.005).toFixed(2)) : 0);
  const rebutTRN = existing?.rebutTRN ?? (quantiteSortieMP > 0 ? Number((quantiteSortieMP * 0.002).toFixed(2)) : 0);
  const totalRebutEmrSciTrn = Number((rebutEMR + rebutSCI + rebutTRN).toFixed(2));

  const quantiteNonReceptionnee = Number((poidsControle - quantiteReceptionneeU2).toFixed(2));
  const ecartParPiece = qteCtrl > 0 ? Number((quantiteNonReceptionnee / qteCtrl).toFixed(6)) : 0;

  const rebutUsinage2 = existing?.rebutUsinage2 ?? (quantiteSortieMP > 0 ? Number((quantiteSortieMP * 0.005).toFixed(2)) : 0);
  const entreeSF = existing?.entreeSF ?? (quantiteReceptionneeU2 > 0 ? Number((quantiteReceptionneeU2 * 0.07).toFixed(2)) : 0);

  // Output OF = (Qté CTRL * poids PHY) + rebuts + Reception U2 + rebut U2 + SF
  const outputOF = Number((poidsControle + totalRebutEmrSciTrn + quantiteReceptionneeU2 + rebutUsinage2 + entreeSF).toFixed(2));

  // Ecart Bilan OF : (Qté CTRL*poids PHY-Qté sortie MP) – somme rebut EMR+ SCI ou TRN et totale Qté réceptionne usinage 2-(somme rebut usinage 2+ entré SF)
  // Which corresponds to: (poidsControle - quantiteSortieMP) - totalRebutEmrSciTrn - quantiteReceptionneeU2 - (rebutUsinage2 + entreeSF)
  const ecartBilanOF = Number(((poidsControle - quantiteSortieMP) - totalRebutEmrSciTrn - quantiteReceptionneeU2 - (rebutUsinage2 + entreeSF)).toFixed(2));
  const tauxEcartOF = quantiteSortieMP > 0 ? Number(((ecartBilanOF / quantiteSortieMP) * 100).toFixed(2)) : 0;

  return {
    id: id ?? existing?.id ?? 0,
    of: numOf,
    codeArticle,
    designationArticle,
    codeTypeMatiere,
    quantiteSortieMP,
    qteCtrl,
    poidsPhy,
    poidsControle,
    rebutEMR,
    rebutSCI,
    rebutTRN,
    totalRebutEmrSciTrn,
    quantiteReceptionneeU2,
    quantiteNonReceptionnee,
    ecartParPiece,
    rebutUsinage2,
    entreeSF,
    outputOF,
    ecartBilanOF,
    tauxEcartOF,
    statut: existing?.statut ?? 'OK',
    observation: existing?.observation || 'Calculé automatiquement',
    periode,
    dateCalcul: new Date().toISOString().split('T')[0]
  };
};

export const bilanService = {
  // Legacy method mapped to Bilan Fonderie
  async getBilans(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanMatiereReport[]> {
    return this.getBilansFonderie(periode, dateDebut, dateFin);
  },

  // Legacy method mapped to Bilan Fonderie
  async calculerBilan(
    periode: string,
    typeMatiere: string,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanMatiereReport[]> {
    return this.calculerBilanFonderie(periode, typeMatiere, dateDebut, dateFin);
  },

  // GET /api/bilans/fonderie
  async getBilansFonderie(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanMatiereReport[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const currentBilans = storage.getBilans();
      const usedIds = new Set(currentBilans.map((b) => b.id));
      const matieres = getAllTypeMatieres(currentBilans);
      const updatedBilans = matieres.map((mat) => {
        const existing = currentBilans.find((b) => b.periode === periode && b.typeMatiere === mat);
        const id = allocateBilanId(existing, usedIds);
        return computeBilanForPeriodAndMatiere(periode, mat, existing, dateDebut, dateFin, id);
      });

      saveBilansForPeriod(currentBilans, periode, updatedBilans);
      return updatedBilans;
    }
    const res = await api.get<BilanMatiereReport[]>('/bilans/fonderie', { params: { periode, dateDebut, dateFin } });
    return res.data;
  },

  // GET /api/bilans/usinage-2/par-piece
  async getBilansUsinage(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanUsinage2ParPiece[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const currentUsinage = storage.getBilansUsinage();
      const dDeb = dateDebut || `${periode}-01`;
      const dFin = dateFin || `${periode}-31`;

      const uniqueArticles = Array.from(
        new Set(
          storage.getReceptions()
            .filter((r) => r.dateReception >= dDeb && r.dateReception <= dFin)
            .map((r) => r.codeArticle)
        )
      );

      const usedIds = new Set(currentUsinage.map((b) => b.id));
      const updated = uniqueArticles.map((art) => {
        const existing = currentUsinage.find((b) => b.periode === periode && b.codeArticle === art);
        const id = allocateBilanId(existing, usedIds);
        return computeBilanUsinageForPeriodAndArticle(periode, art, existing, dateDebut, dateFin, id);
      });

      // save
      const otherPeriods = currentUsinage.filter((b) => b.periode !== periode);
      const list = [...otherPeriods, ...updated].sort((a, b) => a.id - b.id);
      storage.saveBilansUsinage(list);

      return updated;
    }
    const res = await api.get<BilanUsinage2ParPiece[]>('/bilans/usinage-2/par-piece', { params: { periode, dateDebut, dateFin } });
    return res.data;
  },

  // GET /api/bilans/uap2/par-of
  async getBilansUap2(
    periode: string = BUSINESS_RULES.periodeParDefaut,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanUap2ParOF[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const currentUap2 = storage.getBilansUap2();
      const dDeb = dateDebut || `${periode}-01`;
      const dFin = dateFin || `${periode}-31`;

      const uniqueOfs = Array.from(
        new Set(
          storage.getReceptions()
            .filter((r) => r.dateReception >= dDeb && r.dateReception <= dFin)
            .map((r) => r.numOf)
        )
      );

      const usedIds = new Set(currentUap2.map((b) => b.id));
      const updated = uniqueOfs.map((ofNum) => {
        const existing = currentUap2.find((b) => b.periode === periode && b.of === ofNum);
        const id = allocateBilanId(existing, usedIds);
        return computeBilanUap2ForPeriodAndOF(periode, ofNum, existing, dateDebut, dateFin, id);
      });

      // save
      const otherPeriods = currentUap2.filter((b) => b.periode !== periode);
      const list = [...otherPeriods, ...updated].sort((a, b) => a.id - b.id);
      storage.saveBilansUap2(list);

      return updated;
    }
    const res = await api.get<BilanUap2ParOF[]>('/bilans/uap2/par-of', { params: { periode, dateDebut, dateFin } });
    return res.data;
  },

  // POST /api/bilans/fonderie/calculer
  async calculerBilanFonderie(
    periode: string,
    typeMatiere: string,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanMatiereReport[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const currentBilans = storage.getBilans();

      if (typeMatiere === 'TOUS') {
        const usedIds = new Set(currentBilans.map((b) => b.id));
        const matieres = getAllTypeMatieres(currentBilans);
        const updatedBilans = matieres.map((mat) => {
          const existing = currentBilans.find((b) => b.periode === periode && b.typeMatiere === mat);
          const id = allocateBilanId(existing, usedIds);
          return computeBilanForPeriodAndMatiere(periode, mat, existing, dateDebut, dateFin, id);
        });
        saveBilansForPeriod(currentBilans, periode, updatedBilans);
        return updatedBilans;
      }

      const existing = currentBilans.find((b) => b.periode === periode && b.typeMatiere === typeMatiere);
      const usedIds = new Set(currentBilans.map((b) => b.id));
      const id = allocateBilanId(existing, usedIds);

      const newBilan = computeBilanForPeriodAndMatiere(periode, typeMatiere, existing, dateDebut, dateFin, id);

      const filtered = currentBilans.filter((b) => !(b.periode === periode && b.typeMatiere === typeMatiere));
      const updatedList = [...filtered, newBilan].sort((a, b) => a.id - b.id);
      storage.saveBilans(updatedList);

      return updatedList;
    }
    const res = await api.post<BilanMatiereReport[]>('/bilans/fonderie/calculer', { periode, typeMatiere, dateDebut, dateFin });
    return res.data;
  },

  // POST /api/bilans/usinage-2/calculer
  async calculerBilanUsinage(
    periode: string,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanUsinage2ParPiece[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return this.getBilansUsinage(periode, dateDebut, dateFin);
    }
    const res = await api.post<BilanUsinage2ParPiece[]>('/bilans/usinage-2/calculer', { periode, dateDebut, dateFin });
    return res.data;
  },

  // POST /api/bilans/uap2/calculer
  async calculerBilanUap2(
    periode: string,
    dateDebut?: string,
    dateFin?: string
  ): Promise<BilanUap2ParOF[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return this.getBilansUap2(periode, dateDebut, dateFin);
    }
    const res = await api.post<BilanUap2ParOF[]>('/bilans/uap2/calculer', { periode, dateDebut, dateFin });
    return res.data;
  },

  // POST /api/bilans/{id}/verifier
  async verifierBilan(
    id: number,
    type: 'fonderie' | 'usinage' | 'uap2',
    payload: { statutControle: string; observation?: string; justification?: string }
  ): Promise<BilanResult> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      if (type === 'fonderie') {
        const current = storage.getBilans();
        const idx = current.findIndex((b) => b.id === id);
        if (idx !== -1) {
          current[idx].statut = toBilanStatus(payload.statutControle);
          if (payload.justification) current[idx].justification = payload.justification;
          storage.saveBilans(current);
          return current[idx];
        }
      } else if (type === 'usinage') {
        const current = storage.getBilansUsinage();
        const idx = current.findIndex((b) => b.id === id);
        if (idx !== -1) {
          current[idx].statutControle = toUsinageStatus(payload.statutControle);
          if (payload.observation !== undefined) current[idx].observation = payload.observation;
          storage.saveBilansUsinage(current);
          return current[idx];
        }
      } else if (type === 'uap2') {
        const current = storage.getBilansUap2();
        const idx = current.findIndex((b) => b.id === id);
        if (idx !== -1) {
          current[idx].statut = toUap2Status(payload.statutControle);
          if (payload.observation !== undefined) current[idx].observation = payload.observation;
          storage.saveBilansUap2(current);
          return current[idx];
        }
      }
      throw new Error('Bilan non trouvé');
    }
    const res = await api.post(`/bilans/${id}/verifier`, { type, ...payload });
    return res.data;
  },

  // POST /api/bilans/{id}/justifier
  async justifierBilan(
    id: number,
    type: 'fonderie' | 'usinage' | 'uap2',
    justification: string
  ): Promise<BilanResult> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      if (type === 'fonderie') {
        const current = storage.getBilans();
        const idx = current.findIndex((b) => b.id === id);
        if (idx !== -1) {
          current[idx].justification = justification;
          storage.saveBilans(current);
          return current[idx];
        }
      } else if (type === 'usinage') {
        const current = storage.getBilansUsinage();
        const idx = current.findIndex((b) => b.id === id);
        if (idx !== -1) {
          current[idx].observation = justification;
          storage.saveBilansUsinage(current);
          return current[idx];
        }
      } else if (type === 'uap2') {
        const current = storage.getBilansUap2();
        const idx = current.findIndex((b) => b.id === id);
        if (idx !== -1) {
          current[idx].observation = justification;
          storage.saveBilansUap2(current);
          return current[idx];
        }
      }
      throw new Error('Bilan non trouvé');
    }
    const res = await api.post(`/bilans/${id}/justifier`, { type, justification });
    return res.data;
  },

  // POST /api/bilans/{id}/cloturer
  async cloturerBilan(id: number, type: 'fonderie' | 'usinage' | 'uap2'): Promise<BilanResult> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      if (type === 'fonderie') {
        const current = storage.getBilans();
        const idx = current.findIndex((b) => b.id === id);
        if (idx !== -1) {
          current[idx].statut = 'Cloture';
          storage.saveBilans(current);
          return current[idx];
        }
      } else if (type === 'usinage') {
        const current = storage.getBilansUsinage();
        const idx = current.findIndex((b) => b.id === id);
        if (idx !== -1) {
          current[idx].statutControle = 'Regularise';
          storage.saveBilansUsinage(current);
          return current[idx];
        }
      } else if (type === 'uap2') {
        const current = storage.getBilansUap2();
        const idx = current.findIndex((b) => b.id === id);
        if (idx !== -1) {
          current[idx].statut = 'Cloture';
          storage.saveBilansUap2(current);
          return current[idx];
        }
      }
      throw new Error('Bilan non trouvé');
    }
    const res = await api.post(`/bilans/${id}/cloturer`, { type });
    return res.data;
  },

  // Compatibility validerBilan (for fonderie)
  async validerBilan(id: number): Promise<BilanMatiereReport> {
    return this.verifierBilan(id, 'fonderie', { statutControle: 'Valide' }) as Promise<BilanMatiereReport>;
  },

  // Compatibility justifierEcart (for fonderie)
  async justifierEcart(id: number, justification: string): Promise<BilanMatiereReport> {
    return this.justifierBilan(id, 'fonderie', justification) as Promise<BilanMatiereReport>;
  },

  // Compatibility updatePoidsNonReceptionne (for fonderie)
  async updatePoidsNonReceptionne(id: number, payload: PoidsNonReceptionnePayload): Promise<BilanMatiereReport> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getBilans();
      const idx = current.findIndex((b) => b.id === id);
      if (idx !== -1) {
        const previous = current[idx];
        const oldBilan = computeBilanForPeriodAndMatiere(
          previous.periode,
          previous.typeMatiere,
          previous,
          payload.dateDebut,
          payload.dateFin,
          previous.id
        );
        const nextSeed: BilanMatiereReport = {
          ...previous,
          poidsNonReceptionne: normalizePoids(payload.poidsNonReceptionne),
          nombrePiecesNonReceptionnees: normalizePieces(payload.nombrePiecesNonReceptionnees)
        };
        const nextBilan = computeBilanForPeriodAndMatiere(
          previous.periode,
          previous.typeMatiere,
          nextSeed,
          payload.dateDebut,
          payload.dateFin,
          previous.id
        );

        current[idx] = nextBilan;
        storage.saveBilans(current);

        storage.addAuditLog({
          dateHeure: formatAuditDate(),
          utilisateur: payload.utilisateur || 'Administrateur SOPAL',
          role: payload.role || 'Admin',
          action: 'Modification poids non réceptionné',
          module: 'Bilan Matière',
          detail: [
            `Type matière ${previous.typeMatiere} (${previous.periode})`,
            `ancienne valeur: ${oldBilan.poidsNonReceptionne.toLocaleString('fr-FR')} kg / ${oldBilan.nombrePiecesNonReceptionnees.toLocaleString('fr-FR')} pièces`,
            `nouvelle valeur: ${nextBilan.poidsNonReceptionne.toLocaleString('fr-FR')} kg / ${nextBilan.nombrePiecesNonReceptionnees.toLocaleString('fr-FR')} pièces`,
            `ancien ecart: ${oldBilan.ecartKg.toLocaleString('fr-FR')} kg`,
            `nouvel ecart: ${nextBilan.ecartKg.toLocaleString('fr-FR')} kg`,
            `statut conserve: ${previous.statut}`
          ].join(' | '),
          statut: 'Succès'
        });

        return nextBilan;
      }
      throw new Error('Bilan non trouvé');
    }
    const res = await api.post<BilanMatiereReport>(`/bilans/${id}/poids-non-receptionne`, payload);
    return res.data;
  }
};
