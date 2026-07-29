import api from '@/services/api';
import { SourceSynchronisationX3, DonneeApplicationStatus } from '@/features/receptions-usinage2/receptions.types';
import { testDataRepository } from '@/services/testDataRepository.service';
import { assertEnabled, isMockSource } from '@/config/dataSources';

// Storage key for mock synchronization data to allow modifications in the UI
const KEYS = {
  MOCK_X3_STATUS: 'sopal_mock_x3_status',
  MOCK_APP_STATUS: 'sopal_mock_app_status'
};

const SEED_X3_STATUS: SourceSynchronisationX3[] = [
  {
    id: 'emr',
    nom: 'État EMR X3',
    statut: 'Synchronise',
    derniereSynchronisation: '2026-07-07 08:30:00',
    nombreLignes: 24,
    nombreErreurs: 0,
    source: 'X3'
  },
  {
    id: 'rapport-machine',
    nom: 'Rapport journalier machine',
    statut: 'Synchronise',
    derniereSynchronisation: '2026-07-07 09:15:00',
    nombreLignes: 142,
    nombreErreurs: 0,
    source: 'RapportMachine'
  },
  {
    id: 'sorties-mp',
    nom: 'Sorties MP X3',
    statut: 'ASynchroniser',
    derniereSynchronisation: '2026-07-06 17:00:00',
    nombreLignes: 89,
    nombreErreurs: 2,
    source: 'X3'
  },
  {
    id: 'rebuts',
    nom: 'Rebuts EMR / SCI / TRN',
    statut: 'Partiel',
    derniereSynchronisation: '2026-07-07 10:00:00',
    nombreLignes: 45,
    nombreErreurs: 1,
    source: 'X3'
  }
];

const SEED_APP_STATUS: DonneeApplicationStatus[] = [
  {
    id: 'receptions-u2',
    nom: 'Réceptions Usinage 2',
    origine: 'Application',
    derniereMiseAJour: '2026-07-07 11:20:00',
    statut: 'OK',
    nombreLignes: 185
  },
  {
    id: 'inventaire',
    nom: 'Inventaire',
    origine: 'Application',
    derniereMiseAJour: '2026-07-07 10:50:00',
    statut: 'OK',
    nombreLignes: 54
  },
  {
    id: 'justifications',
    nom: 'Justifications Admin',
    origine: 'Application',
    derniereMiseAJour: '2026-07-07 12:00:00',
    statut: 'OK',
    nombreLignes: 8
  },
  {
    id: 'statuts-controle',
    nom: 'Statuts de contrôle',
    origine: 'Application',
    derniereMiseAJour: '2026-07-07 12:10:00',
    statut: 'OK',
    nombreLignes: 3
  }
];

const getLocalStorageItem = <T>(key: string, defaultValue: T): T => {
  const stored = localStorage.getItem(key);
  if (!stored) {
    localStorage.setItem(key, JSON.stringify(defaultValue));
    return defaultValue;
  }
  try {
    const parsed = JSON.parse(stored) as unknown;
    const hasCompatibleShape = Array.isArray(defaultValue)
      ? Array.isArray(parsed)
      : parsed !== null && typeof parsed === typeof defaultValue;
    if (!hasCompatibleShape) {
      localStorage.setItem(key, JSON.stringify(defaultValue));
      return defaultValue;
    }
    return parsed as T;
  } catch {
    localStorage.setItem(key, JSON.stringify(defaultValue));
    return defaultValue;
  }
};

const setLocalStorageItem = <T>(key: string, value: T): void => {
  localStorage.setItem(key, JSON.stringify(value));
};

const updateMockSource = (id: string, updates: Partial<SourceSynchronisationX3>) => {
  const current = getLocalStorageItem(KEYS.MOCK_X3_STATUS, SEED_X3_STATUS);
  const updated = current.map((item) => {
    if (item.id === id) {
      return {
        ...item,
        ...updates,
        derniereSynchronisation: new Date().toISOString().replace('T', ' ').slice(0, 19)
      };
    }
    return item;
  });
  setLocalStorageItem(KEYS.MOCK_X3_STATUS, updated);
  return updated;
};

export const synchronisationService = {
  async getX3Status(): Promise<SourceSynchronisationX3[]> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      return getLocalStorageItem(KEYS.MOCK_X3_STATUS, SEED_X3_STATUS);
    }
    const res = await api.get<SourceSynchronisationX3[]>('/synchronisation/x3/status');
    return res.data;
  },

  async syncAll(): Promise<SourceSynchronisationX3[]> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      testDataRepository.synchronizeX3();
      // Simulate sync for all sources
      const current = getLocalStorageItem(KEYS.MOCK_X3_STATUS, SEED_X3_STATUS);
      const updated = current.map((item) => ({
        ...item,
        statut: 'Synchronise' as const,
        nombreErreurs: 0,
        derniereSynchronisation: new Date().toISOString().replace('T', ' ').slice(0, 19)
      }));
      setLocalStorageItem(KEYS.MOCK_X3_STATUS, updated);
      return updated;
    }
    const res = await api.post<SourceSynchronisationX3[]>('/synchronisation/x3/all');
    return res.data;
  },

  async syncEmr(): Promise<SourceSynchronisationX3[]> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      testDataRepository.synchronizeX3();
      return updateMockSource('emr', { statut: 'Synchronise', nombreErreurs: 0 });
    }
    await api.post('/synchronisation/x3/emr');
    return this.getX3Status();
  },

  async syncSortiesMp(): Promise<SourceSynchronisationX3[]> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      testDataRepository.synchronizeX3();
      return updateMockSource('sorties-mp', { statut: 'Synchronise', nombreErreurs: 0 });
    }
    await api.post('/synchronisation/x3/sorties-mp');
    return this.getX3Status();
  },

  async syncRapportMachine(): Promise<SourceSynchronisationX3[]> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      testDataRepository.synchronizeX3();
      return updateMockSource('rapport-machine', { statut: 'Synchronise', nombreErreurs: 0 });
    }
    await api.post('/synchronisation/x3/rapport-machine');
    return this.getX3Status();
  },

  async syncRebuts(): Promise<SourceSynchronisationX3[]> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      testDataRepository.synchronizeX3();
      return updateMockSource('rebuts', { statut: 'Synchronise', nombreErreurs: 0 });
    }
    await api.post('/synchronisation/x3/rebuts');
    return this.getX3Status();
  },

  async getLogs(): Promise<string[]> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      return [
        `[${new Date().toISOString().slice(0, 10)}] Synchronisation globale X3 lancée.`,
        'Vérification de la connectivité avec la base Sage X3 réussie.',
        "Importation de 24 nouvelles lignes de l'état EMR X3.",
        'Synchronisation réussie.'
      ];
    }
    const res = await api.get<string[]>('/synchronisation/x3/logs');
    return res.data;
  },

  async getReceptionsStatus(): Promise<DonneeApplicationStatus> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      const all = getLocalStorageItem(KEYS.MOCK_APP_STATUS, SEED_APP_STATUS);
      return all.find((item) => item.id === 'receptions-u2') || SEED_APP_STATUS[0];
    }
    const res = await api.get<DonneeApplicationStatus>('/receptions-usinage-2/status');
    return res.data;
  },

  async getInventairesStatus(): Promise<DonneeApplicationStatus> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      const all = getLocalStorageItem(KEYS.MOCK_APP_STATUS, SEED_APP_STATUS);
      return all.find((item) => item.id === 'inventaire') || SEED_APP_STATUS[1];
    }
    const res = await api.get<DonneeApplicationStatus>('/inventaires/status');
    return res.data;
  },

  async getAppStatusAll(): Promise<DonneeApplicationStatus[]> {
    assertEnabled('synchronisationX3');
    if (isMockSource('synchronisationX3')) {
      return getLocalStorageItem(KEYS.MOCK_APP_STATUS, SEED_APP_STATUS);
    }
    const rec = await this.getReceptionsStatus();
    const inv = await this.getInventairesStatus();
    // Return structured list
    return [
      rec,
      inv,
      {
        id: 'justifications',
        nom: 'Justifications Admin',
        origine: 'Application',
        derniereMiseAJour: new Date().toISOString().replace('T', ' ').slice(0, 10),
        statut: 'OK',
        nombreLignes: 8
      },
      {
        id: 'statuts-controle',
        nom: 'Statuts de contrôle',
        origine: 'Application',
        derniereMiseAJour: new Date().toISOString().replace('T', ' ').slice(0, 10),
        statut: 'OK',
        nombreLignes: 3
      }
    ];
  }
};
