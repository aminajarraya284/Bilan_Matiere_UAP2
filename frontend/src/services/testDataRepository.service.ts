import {
  sopalTestDataset,
  TEST_DATA_STORAGE_KEY,
  TEST_DATA_VERSION,
  type DechetFonderieTest,
  type InventaireFonderieCategorie,
  type InventaireFonderieConteneur,
  type InventaireFonderieFour,
  type ReceptionUsinage2Test,
  type RebutUsinage2,
  type RebutUap2Saisie,
  type SopalTestDataset
} from '@/mocks/sopalTestDataset';

export type ManualX3Record = {
  collection: keyof Pick<
    SopalTestDataset,
    | 'x3OrdresFabrication'
    | 'x3OperationsOF'
    | 'x3ComposantsOF'
    | 'refGammeFabrication'
    | 'refNomenclature'
    | 'x3RapportsMachine'
    | 'x3MouvementsStock'
    | 'importBatches'
  >;
  data: Record<string, unknown>;
};

const LEGACY_KEYS = [
  'sopal_sorties_x3',
  'sopal_dechets',
  'sopal_receptions',
  'sopal_inventaire_conteneurs',
  'sopal_inventaire_categories',
  'sopal_inventaire_fours',
  'sopal_bilans',
  'sopal_bilans_usinage',
  'sopal_bilans_uap2',
  'sopal_reference_fabrication_gamme',
  'sopal_reference_fabrication_nomenclature'
];

const clone = <T>(value: T): T => JSON.parse(JSON.stringify(value)) as T;

const canUseLocalStorage = (): boolean => typeof localStorage !== 'undefined';

const REQUIRED_DATASET_COLLECTIONS: Array<keyof SopalTestDataset> = [
  'refTypesMatiere',
  'x3OrdresFabrication',
  'x3OperationsOF',
  'x3ComposantsOF',
  'refGammeFabrication',
  'refNomenclature',
  'x3RapportsMachine',
  'x3MouvementsStock',
  'receptionsUsinage2',
  'inventaireConteneurs',
  'inventaireCategories',
  'inventaireFours',
  'stockDebut',
  'dechetsFonderie',
  'rebutsUsinage2',
  'importBatches'
];

const isDataset = (value: unknown): value is SopalTestDataset => {
  if (!value || typeof value !== 'object') return false;
  const dataset = value as Partial<SopalTestDataset>;
  return (
    dataset.version === TEST_DATA_VERSION &&
    REQUIRED_DATASET_COLLECTIONS.every((collection) => Array.isArray(dataset[collection])) &&
    (dataset.rebutsUap2 === undefined || Array.isArray(dataset.rebutsUap2))
  );
};

const writeDataset = (dataset: SopalTestDataset): void => {
  if (!canUseLocalStorage()) return;
  try {
    localStorage.setItem(TEST_DATA_STORAGE_KEY, JSON.stringify(dataset));
  } catch (error) {
    if (
      error instanceof DOMException &&
      (error.name === 'QuotaExceededError' || error.name === 'NS_ERROR_DOM_QUOTA_REACHED')
    ) {
      throw new Error(
        "Le fichier est trop volumineux pour le stockage local du navigateur. Activez le backend puis relancez l'import."
      );
    }
    throw error;
  }
};

const clearLegacyMockKeys = (): void => {
  if (!canUseLocalStorage()) return;
  LEGACY_KEYS.forEach((key) => localStorage.removeItem(key));
};

const loadDataset = (): SopalTestDataset => {
  if (!canUseLocalStorage()) return clone(sopalTestDataset);

  const raw = localStorage.getItem(TEST_DATA_STORAGE_KEY);
  if (raw) {
    try {
      const parsed = JSON.parse(raw) as unknown;
      if (isDataset(parsed)) return parsed;
    } catch {
      // fall through and reset the mock dataset
    }
  }

  clearLegacyMockKeys();
  const initial = clone(sopalTestDataset);
  writeDataset(initial);
  return initial;
};

const updateDataset = (updater: (dataset: SopalTestDataset) => void): SopalTestDataset => {
  const dataset = loadDataset();
  updater(dataset);
  writeDataset(dataset);
  return dataset;
};

export const testDataRepository = {
  getDataset(): SopalTestDataset {
    return clone(loadDataset());
  },

  findOrdreFabrication(numeroOF: string) {
    return loadDataset().x3OrdresFabrication.find((item) => item.numeroOF === numeroOF && item.isActive);
  },

  findGammeByArticle(codeArticle: string) {
    return loadDataset().refGammeFabrication.find((item) => item.codeArticle === codeArticle && item.isActive);
  },

  findNomenclatureMatiereByArticle(codeArticle: string) {
    return loadDataset().refNomenclature.find((item) => item.codeArticleParent === codeArticle && item.isActive);
  },

  resetDataset(): SopalTestDataset {
    clearLegacyMockKeys();
    const initial = clone(sopalTestDataset);
    writeDataset(initial);
    return clone(initial);
  },

  replaceDataset(dataset: SopalTestDataset): void {
    writeDataset(clone(dataset));
  },

  getReceptions(): ReceptionUsinage2Test[] {
    return clone(loadDataset().receptionsUsinage2);
  },

  saveReceptions(receptions: ReceptionUsinage2Test[]): void {
    updateDataset((dataset) => {
      dataset.receptionsUsinage2 = clone(receptions);
    });
  },

  saveX3Mouvements(items: SopalTestDataset['x3MouvementsStock']): void {
    updateDataset((dataset) => {
      dataset.x3MouvementsStock = clone(items);
    });
  },

  saveDechetsTest(items: DechetFonderieTest[]): void {
    this.saveDechets(items);
  },

  addReception(reception: Omit<ReceptionUsinage2Test, 'id'>): ReceptionUsinage2Test {
    const dataset = updateDataset((draft) => {
      const nextId = draft.receptionsUsinage2.length
        ? Math.max(...draft.receptionsUsinage2.map((item) => item.id)) + 1
        : 1;
      draft.receptionsUsinage2 = [{ ...reception, id: nextId }, ...draft.receptionsUsinage2];
    });
    return clone(dataset.receptionsUsinage2[0]);
  },

  deleteReception(id: number): void {
    updateDataset((dataset) => {
      dataset.receptionsUsinage2 = dataset.receptionsUsinage2.filter((item) => item.id !== id);
    });
  },

  getDechets(): DechetFonderieTest[] {
    return clone(loadDataset().dechetsFonderie);
  },

  saveDechets(dechets: DechetFonderieTest[]): void {
    updateDataset((dataset) => {
      dataset.dechetsFonderie = clone(dechets);
    });
  },

  getInventaireConteneurs(): InventaireFonderieConteneur[] {
    return clone(loadDataset().inventaireConteneurs);
  },

  saveInventaireConteneurs(items: InventaireFonderieConteneur[]): void {
    updateDataset((dataset) => {
      dataset.inventaireConteneurs = clone(items);
    });
  },

  getInventaireCategories(): InventaireFonderieCategorie[] {
    return clone(loadDataset().inventaireCategories);
  },

  saveInventaireCategories(items: InventaireFonderieCategorie[]): void {
    updateDataset((dataset) => {
      dataset.inventaireCategories = clone(items);
    });
  },

  getInventaireFours(): InventaireFonderieFour[] {
    return clone(loadDataset().inventaireFours);
  },

  saveInventaireFours(items: InventaireFonderieFour[]): void {
    updateDataset((dataset) => {
      dataset.inventaireFours = clone(items);
    });
  },

  getRebutsUsinage2(): RebutUsinage2[] {
    return clone(loadDataset().rebutsUsinage2);
  },

  saveRebutsUsinage2(items: RebutUsinage2[]): void {
    updateDataset((dataset) => {
      dataset.rebutsUsinage2 = clone(items);
    });
  },

  getRebutsUap2(): RebutUap2Saisie[] {
    return clone(loadDataset().rebutsUap2 ?? []);
  },

  saveRebutsUap2(items: RebutUap2Saisie[]): void {
    updateDataset((dataset) => {
      dataset.rebutsUap2 = clone(items);
    });
  },

  createManualX3Record({ collection, data }: ManualX3Record): unknown {
    const dataset = updateDataset((draft) => {
      const records = draft[collection] as unknown as Array<Record<string, unknown>>;
      const numericIds = records.map((item) => Number(item.id)).filter((id) => Number.isFinite(id));
      const nextId = numericIds.length ? Math.max(...numericIds) + 1 : 1;
      records.push({
        ...data,
        ...(collection === 'importBatches' ? {} : { id: nextId }),
        sourceSystem: 'SaisieManuelle'
      });
    });
    const records = dataset[collection] as unknown as Array<Record<string, unknown>>;
    return clone(records[records.length - 1]);
  },

  synchronizeX3(): SopalTestDataset {
    const current = loadDataset();
    const applicationData = {
      receptionsUsinage2: current.receptionsUsinage2,
      inventaireConteneurs: current.inventaireConteneurs,
      inventaireCategories: current.inventaireCategories,
      inventaireFours: current.inventaireFours,
      dechetsFonderie: current.dechetsFonderie,
      rebutsUsinage2: current.rebutsUsinage2,
      rebutsUap2: current.rebutsUap2 ?? []
    };
    const refreshed = {
      ...clone(sopalTestDataset),
      ...applicationData
    };
    writeDataset(refreshed);
    return clone(refreshed);
  }
};

export const createManualX3Record = (data: ManualX3Record): unknown => testDataRepository.createManualX3Record(data);
