import { SortieX3Mouvement } from '@/features/sorties-x3/sortiesX3.types';
import { ReceptionUsinage2Mouvement } from '@/features/receptions-usinage2/receptions.types';
import { BilanMatiereReport, BilanUsinage2ParPiece, BilanUap2ParOF } from '@/features/bilan-matiere/bilan.types';
import { AuditLogEntry } from '@/features/audit/audit.types';
import {
  testBilans,
  testReceptionsUsinage2,
  testSortiesMagasin
} from '@/data/testData';

// Keys
const KEYS = {
  SORTIES_X3: 'sopal_sorties_x3',
  DECHETS: 'sopal_dechets',
  RECEPTIONS: 'sopal_receptions',
  INVENTAIRE_CONTENEURS: 'sopal_inventaire_conteneurs',
  INVENTAIRE_CATEGORIES: 'sopal_inventaire_categories',
  INVENTAIRE_FOURS: 'sopal_inventaire_fours',
  BILANS: 'sopal_bilans',
  BILANS_USINAGE: 'sopal_bilans_usinage',
  BILANS_UAP2: 'sopal_bilans_uap2',
  AUDIT_LOGS: 'sopal_audit_logs'
};

const ALLOWED_TYPE_MATIERES = new Set(['M3010L0', 'M3000L0', 'M3060L0']);
const hasAllowedMatiere = (value?: string): boolean => Boolean(value && ALLOWED_TYPE_MATIERES.has(value));

// --- NORMALIZATION HELPERS ---

// --- STORAGE MANAGER ---

const hasCompatibleStorageShape = <T>(value: unknown, defaultValue: T): value is T => {
  if (Array.isArray(defaultValue)) return Array.isArray(value);
  if (defaultValue !== null && typeof defaultValue === 'object') {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
  }
  return typeof value === typeof defaultValue;
};

export const getStorageItem = <T>(key: string, defaultValue: T): T => {
  const stored = localStorage.getItem(key);
  if (!stored) {
    localStorage.setItem(key, JSON.stringify(defaultValue));
    return defaultValue;
  }
  try {
    const parsed = JSON.parse(stored) as unknown;
    if (!hasCompatibleStorageShape(parsed, defaultValue)) {
      localStorage.setItem(key, JSON.stringify(defaultValue));
      return defaultValue;
    }
    return parsed;
  } catch (e) {
    console.error('Error parsing localStorage key ' + key, e);
    localStorage.setItem(key, JSON.stringify(defaultValue));
    return defaultValue;
  }
};

export const setStorageItem = <T>(key: string, value: T): void => {
  localStorage.setItem(key, JSON.stringify(value));
};

export const storage = {
  // Sorties X3
  getSortiesX3(): SortieX3Mouvement[] {
    return getStorageItem(KEYS.SORTIES_X3, testSortiesMagasin);
  },
  saveSortiesX3(data: SortieX3Mouvement[]): void {
    setStorageItem(KEYS.SORTIES_X3, data);
  },

  // Réceptions
  getReceptions(): ReceptionUsinage2Mouvement[] {
    return getStorageItem(KEYS.RECEPTIONS, testReceptionsUsinage2).filter((item) =>
      hasAllowedMatiere(item.codeTypeMatiere)
    );
  },
  saveReceptions(data: ReceptionUsinage2Mouvement[]): void {
    setStorageItem(
      KEYS.RECEPTIONS,
      data.filter((item) => hasAllowedMatiere(item.codeTypeMatiere))
    );
  },

  // Bilans
  getBilans(): BilanMatiereReport[] {
    return getStorageItem(KEYS.BILANS, testBilans.fonderie).filter((item) => hasAllowedMatiere(item.typeMatiere));
  },
  saveBilans(data: BilanMatiereReport[]): void {
    setStorageItem(
      KEYS.BILANS,
      data.filter((item) => hasAllowedMatiere(item.typeMatiere))
    );
  },

  // Bilans Usinage
  getBilansUsinage(): BilanUsinage2ParPiece[] {
    return getStorageItem(KEYS.BILANS_USINAGE, testBilans.usinage2).filter((item) =>
      hasAllowedMatiere(item.codeTypeMatiere)
    );
  },
  saveBilansUsinage(data: BilanUsinage2ParPiece[]): void {
    setStorageItem(
      KEYS.BILANS_USINAGE,
      data.filter((item) => hasAllowedMatiere(item.codeTypeMatiere))
    );
  },

  // Bilans UAP2
  getBilansUap2(): BilanUap2ParOF[] {
    return getStorageItem(KEYS.BILANS_UAP2, testBilans.uap2).filter((item) => hasAllowedMatiere(item.codeTypeMatiere));
  },
  saveBilansUap2(data: BilanUap2ParOF[]): void {
    setStorageItem(
      KEYS.BILANS_UAP2,
      data.filter((item) => hasAllowedMatiere(item.codeTypeMatiere))
    );
  },

  // Audit
  getAuditLogs(): AuditLogEntry[] {
    return getStorageItem(KEYS.AUDIT_LOGS, []);
  },
  saveAuditLogs(data: AuditLogEntry[]): void {
    setStorageItem(KEYS.AUDIT_LOGS, data);
  },
  addAuditLog(entry: Omit<AuditLogEntry, 'id'>): AuditLogEntry {
    const current = this.getAuditLogs();
    const newEntry: AuditLogEntry = {
      ...entry,
      id: current.length ? Math.max(...current.map((log) => log.id)) + 1 : 1000
    };
    this.saveAuditLogs([newEntry, ...current]);
    return newEntry;
  },

  // Reset helper
  resetAll(): void {
    localStorage.removeItem(KEYS.SORTIES_X3);
    localStorage.removeItem(KEYS.DECHETS);
    localStorage.removeItem(KEYS.RECEPTIONS);
    localStorage.removeItem(KEYS.INVENTAIRE_CONTENEURS);
    localStorage.removeItem(KEYS.INVENTAIRE_CATEGORIES);
    localStorage.removeItem(KEYS.INVENTAIRE_FOURS);
    localStorage.removeItem(KEYS.BILANS);
    localStorage.removeItem(KEYS.BILANS_USINAGE);
    localStorage.removeItem(KEYS.BILANS_UAP2);
    localStorage.removeItem(KEYS.AUDIT_LOGS);
    window.location.reload();
  }
};
