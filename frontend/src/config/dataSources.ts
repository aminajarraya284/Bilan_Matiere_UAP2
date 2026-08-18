/**
 * Source de donnees par verticale fonctionnelle.
 *
 * Ce module est le SEUL endroit du frontend autorise a lire les variables
 * d'environnement de routage. Aucun service metier ne doit lire directement
 * `import.meta.env.VITE_USE_REAL_API` ni `VITE_*_SOURCE`.
 *
 * Modes :
 *  - `api`      : appels backend uniquement. Une erreur reste une erreur, jamais
 *                 de bascule silencieuse vers les donnees de demonstration.
 *  - `mock`     : donnees de demonstration uniquement, aucun appel reseau.
 *  - `disabled` : aucun appel, aucune donnee fictive ; l'ecran affiche
 *                 « Fonctionnalite non disponible ».
 *
 * DEPRECATION : `VITE_USE_REAL_API` n'est conserve que comme valeur de repli
 * historique lorsqu'aucune variable par verticale n'est definie. Il sera
 * SUPPRIME DEFINITIVEMENT dans un lot ulterieur — definissez les variables
 * `VITE_*_SOURCE` (voir .env.example).
 */

export type DataSourceMode = 'api' | 'mock' | 'disabled';

/** Alias historique conserve pour les imports existants. */
export type DataSource = DataSourceMode;

export type DataVertical =
  | 'auth'
  | 'etatsX3'
  | 'typesMatiere'
  | 'dechetsFonderie'
  | 'controlePoids'
  | 'receptionsU2'
  | 'referencesFabrication'
  | 'bilanU2'
  | 'sortiesX3'
  | 'historiqueImports'
  | 'dashboard'
  | 'bilanFonderie'
  | 'bilanUap2'
  | 'inventaire'
  | 'rapports'
  | 'alertes'
  | 'audit'
  | 'administration'
  | 'synchronisationX3';

const VALID_MODES: readonly DataSourceMode[] = ['api', 'mock', 'disabled'];

export const isDataSourceMode = (value: unknown): value is DataSourceMode =>
  typeof value === 'string' && (VALID_MODES as readonly string[]).includes(value);

/**
 * Valeur par defaut de chaque verticale, appliquee quand aucune variable
 * d'environnement n'est fournie. Elle reflete l'etat reel du backend :
 * `api` uniquement la ou une route existe et est alignee.
 */
const DEFAULTS: Readonly<Record<DataVertical, DataSourceMode>> = {
  // Backend confirme, service frontend aligne, tests verts.
  auth: 'api',
  etatsX3: 'api',
  typesMatiere: 'api',
  dechetsFonderie: 'api',
  controlePoids: 'api',
  receptionsU2: 'api',
  referencesFabrication: 'api',
  bilanU2: 'api',
  sortiesX3: 'api',
  historiqueImports: 'api',
  inventaire: 'api',
  bilanFonderie: 'api',
  bilanUap2: 'api',
  dashboard: 'api',
  alertes: 'api',
  rapports: 'api',

  // Aucun endpoint backend : les routes appelees renverraient 404.
  // Les donnees de demonstration restent exploitables, d'ou `mock`.
  audit: 'mock',
  administration: 'mock',

  // Aucun endpoint backend ET aucune donnee de demonstration credible :
  // afficher des synchronisations fictives induirait l'utilisateur en erreur.
  synchronisationX3: 'disabled'
};

/** Nom de la variable d'environnement pilotant chaque verticale. */
const ENV_KEYS: Readonly<Record<DataVertical, string>> = {
  auth: 'VITE_AUTH_SOURCE',
  etatsX3: 'VITE_ETATS_X3_SOURCE',
  typesMatiere: 'VITE_TYPES_MATIERE_SOURCE',
  dechetsFonderie: 'VITE_DECHETS_FONDERIE_SOURCE',
  controlePoids: 'VITE_CONTROLE_POIDS_SOURCE',
  receptionsU2: 'VITE_RECEPTIONS_U2_SOURCE',
  referencesFabrication: 'VITE_REFERENCES_FABRICATION_SOURCE',
  bilanU2: 'VITE_BILAN_U2_SOURCE',
  sortiesX3: 'VITE_SORTIES_X3_SOURCE',
  historiqueImports: 'VITE_HISTORIQUE_IMPORTS_SOURCE',
  dashboard: 'VITE_DASHBOARD_SOURCE',
  bilanFonderie: 'VITE_BILAN_FONDERIE_SOURCE',
  bilanUap2: 'VITE_BILAN_UAP2_SOURCE',
  inventaire: 'VITE_INVENTAIRE_SOURCE',
  rapports: 'VITE_RAPPORTS_SOURCE',
  alertes: 'VITE_ALERTES_SOURCE',
  audit: 'VITE_AUDIT_SOURCE',
  administration: 'VITE_ADMINISTRATION_SOURCE',
  synchronisationX3: 'VITE_SYNCHRONISATION_X3_SOURCE'
};

/**
 * Anciens noms de variables encore acceptes, par verticale. Consultes apres
 * `ENV_KEYS` et avant le repli `VITE_USE_REAL_API`, pour ne pas casser un
 * `.env` deja en place.
 */
const LEGACY_ENV_KEYS: Partial<Readonly<Record<DataVertical, string>>> = {
  dechetsFonderie: 'VITE_DECHETS_SOURCE',
  historiqueImports: 'VITE_IMPORTS_HISTORIQUE_SOURCE',
  administration: 'VITE_ADMIN_SOURCE'
};

/** Libelle lisible de chaque verticale (messages d'erreur et badges). */
const LABELS: Readonly<Record<DataVertical, string>> = {
  auth: 'Authentification',
  etatsX3: 'États X3',
  typesMatiere: 'Types matière',
  dechetsFonderie: 'Déchets Fonderie',
  controlePoids: 'Contrôle poids Fonderie',
  receptionsU2: 'Réceptions Usinage 2',
  referencesFabrication: 'Références fabrication',
  bilanU2: 'Bilan Usinage 2',
  sortiesX3: 'Sorties magasin',
  historiqueImports: 'Historique des imports',
  dashboard: 'Tableau de bord',
  bilanFonderie: 'Bilan Fonderie',
  bilanUap2: 'Bilan UAP2',
  inventaire: 'Inventaire Fonderie',
  rapports: 'Rapports',
  alertes: 'Alertes',
  audit: "Journal d'audit",
  administration: 'Administration',
  synchronisationX3: 'Synchronisation Sage X3'
};

const warned = new Set<string>();

/**
 * Avertissement de CONFIGURATION INVALIDE : emis dans tous les modes.
 * Une valeur inconnue est un defaut de configuration ; la taire en production
 * est precisement ce qui fait passer une erreur de deploiement inapercue.
 */
const warnInvalidOnce = (message: string): void => {
  if (warned.has(message)) return;
  warned.add(message);
  console.warn(`[dataSources] ${message}`);
};

/**
 * Avertissement de DEPRECATION : purement informatif, reserve a Development
 * pour ne pas polluer la console des utilisateurs finaux.
 */
const warnDeprecationOnce = (message: string): void => {
  if (warned.has(message)) return;
  warned.add(message);
  if (import.meta.env.DEV) console.warn(`[dataSources] ${message}`);
};

/** Expose la remise a zero des avertissements pour les tests. */
export const __resetDataSourceWarnings = (): void => warned.clear();

const readEnv = (key: string): string | undefined => {
  const value = (import.meta.env as Record<string, unknown>)[key];
  return typeof value === 'string' && value.trim() !== '' ? value.trim().toLowerCase() : undefined;
};

const resolve = (vertical: DataVertical): DataSourceMode => {
  const envKey = ENV_KEYS[vertical];
  const raw = readEnv(envKey);

  // 1. Variable par verticale : toujours prioritaire.
  if (raw !== undefined) {
    if (isDataSourceMode(raw)) return raw;
    warnInvalidOnce(
      `${envKey}="${raw}" est une valeur inconnue (attendu : ${VALID_MODES.join(' | ')}). ` +
        `Repli sur la valeur par défaut "${DEFAULTS[vertical]}".`
    );
    return DEFAULTS[vertical];
  }

  // 2. Ancien nom de variable pour cette verticale, s'il existe.
  const legacyKey = LEGACY_ENV_KEYS[vertical];
  if (legacyKey) {
    const legacyRaw = readEnv(legacyKey);
    if (legacyRaw !== undefined) {
      warnDeprecationOnce(`${legacyKey} est déprécié : renommez-le en ${envKey}.`);
      if (isDataSourceMode(legacyRaw)) return legacyRaw;
      warnInvalidOnce(`${legacyKey}="${legacyRaw}" est une valeur inconnue. Repli sur "${DEFAULTS[vertical]}".`);
      return DEFAULTS[vertical];
    }
  }

  // 3. Repli historique VITE_USE_REAL_API (déprécié).
  const legacy = readEnv('VITE_USE_REAL_API');
  if (legacy !== undefined) {
    warnDeprecationOnce(
      `VITE_USE_REAL_API est déprécié et sera supprimé dans un lot ultérieur. ` +
        `Définissez ${envKey} explicitement (voir .env.example).`
    );
    return legacy === 'true' ? 'api' : 'mock';
  }

  // 4. Valeur par defaut explicite.
  return DEFAULTS[vertical];
};

/** Mode courant d'une verticale. */
export const getDataSourceMode = (vertical: DataVertical): DataSourceMode => resolve(vertical);
export const getDataSource = getDataSourceMode;

export const isApiSource = (vertical: DataVertical): boolean => resolve(vertical) === 'api';
export const isMockSource = (vertical: DataVertical): boolean => resolve(vertical) === 'mock';
export const isDisabledSource = (vertical: DataVertical): boolean => resolve(vertical) === 'disabled';
export const isApi = isApiSource;
export const isMock = isMockSource;
export const isDisabled = isDisabledSource;

export const getVerticalLabel = (vertical: DataVertical): string => LABELS[vertical];

export const ALL_VERTICALS = Object.keys(DEFAULTS) as DataVertical[];

/** Instantane de toutes les verticales (diagnostic, tests, affichage). */
export const getAllDataSources = (): Record<DataVertical, DataSourceMode> =>
  ALL_VERTICALS.reduce(
    (acc, vertical) => {
      acc[vertical] = resolve(vertical);
      return acc;
    },
    {} as Record<DataVertical, DataSourceMode>
  );

export type GlobalMode = 'api' | 'mock' | 'hybride' | 'disabled';

/**
 * Mode global affiche dans la barre laterale, calcule sur les verticales
 * ACTIVES (les verticales `disabled` sont exclues : elles ne consomment
 * aucune source). Afficher « API réelle » alors qu'une partie de l'application
 * lit des mocks serait trompeur.
 */
export const getGlobalMode = (): GlobalMode => {
  const active = ALL_VERTICALS.map(resolve).filter((mode) => mode !== 'disabled');
  if (active.length === 0) return 'disabled';
  if (active.every((mode) => mode === 'api')) return 'api';
  if (active.every((mode) => mode === 'mock')) return 'mock';
  return 'hybride';
};

export const GLOBAL_MODE_LABELS: Readonly<Record<GlobalMode, string>> = {
  api: 'API réelle',
  mock: 'Mode démonstration',
  hybride: 'Mode hybride',
  disabled: 'Aucune source active'
};

/**
 * Erreur levee lorsqu'une verticale `disabled` est sollicitee. Permet aux vues
 * de distinguer « fonctionnalite non disponible » d'une panne reseau.
 */
export class FeatureDisabledError extends Error {
  constructor(public readonly vertical: DataVertical) {
    super(`Fonctionnalité non disponible : ${LABELS[vertical]}`);
    this.name = 'FeatureDisabledError';
  }
}

/** Garde a placer en tete des services d'une verticale susceptible d'etre desactivee. */
export const assertEnabled = (vertical: DataVertical): void => {
  if (resolve(vertical) === 'disabled') throw new FeatureDisabledError(vertical);
};

/** Garde des services qui ne possèdent volontairement aucun équivalent mock. */
export const assertApiSource = (vertical: DataVertical): void => {
  assertEnabled(vertical);
  if (resolve(vertical) !== 'api') {
    throw new Error(`${LABELS[vertical]} nécessite une source API.`);
  }
};

/** Garde des services de démonstration qui ne possèdent pas encore d'API. */
export const assertMockSource = (vertical: DataVertical): void => {
  assertEnabled(vertical);
  if (resolve(vertical) !== 'mock') {
    throw new Error(`${LABELS[vertical]} ne possède pas encore de source API.`);
  }
};

/**
 * Compatibilite ascendante : l'objet `dataSources` historique reste expose pour
 * `dechets.service.ts` et ses tests existants. Les nouvelles utilisations
 * doivent passer par `getDataSource()`.
 * Les accesseurs sont paresseux afin que `vi.stubEnv` reste pris en compte.
 */
export const dataSources = {
  get dechetsFonderie(): DataSourceMode {
    return resolve('dechetsFonderie');
  },
  get typesMatiere(): DataSourceMode {
    return resolve('typesMatiere');
  }
};
