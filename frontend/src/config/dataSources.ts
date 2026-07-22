export type DataSource = 'mock' | 'api';

const legacySource: DataSource = import.meta.env.VITE_USE_REAL_API === 'true' ? 'api' : 'mock';

const parseSource = (value: string | undefined): DataSource => {
  return value === 'api' || value === 'mock' ? value : legacySource;
};

export const dataSources = {
  dechetsFonderie: parseSource(import.meta.env.VITE_DECHETS_SOURCE),
  typesMatiere: parseSource(import.meta.env.VITE_TYPES_MATIERE_SOURCE)
} as const;
