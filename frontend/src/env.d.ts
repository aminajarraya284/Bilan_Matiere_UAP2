/// <reference types="vite/client" />

/** Modes autorises pour une source de donnees (voir src/config/dataSources.ts). */
type ViteDataSourceMode = 'api' | 'mock' | 'disabled';
type ViteAuthMode = 'development' | 'entra';

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  readonly VITE_AUTH_MODE?: ViteAuthMode;
  readonly VITE_ENTRA_CLIENT_ID?: string;
  readonly VITE_ENTRA_TENANT_ID?: string;
  readonly VITE_ENTRA_AUTHORITY?: string;
  readonly VITE_ENTRA_REDIRECT_URI?: string;
  readonly VITE_ENTRA_POST_LOGOUT_REDIRECT_URI?: string;
  readonly VITE_ENTRA_API_SCOPE?: string;
  readonly VITE_SEUIL_ECART_FONDERIE?: string;
  readonly VITE_SEUIL_ECART_UAP2?: string;
  readonly VITE_TYPES_MATIERE?: string;
  readonly VITE_SEUIL_ALERTE_POIDS_KG?: string;
  readonly VITE_SEUIL_ECART_KG_HIGHLIGHT?: string;
  readonly VITE_SEUIL_DECALAGE_IMPORTANT_KG?: string;

  /**
   * @deprecated Repli historique uniquement. Sera supprime dans un lot ulterieur.
   * Utiliser les variables `VITE_*_SOURCE` ci-dessous.
   */
  readonly VITE_USE_REAL_API?: string;

  // Verticales adossees a un backend reel.
  readonly VITE_AUTH_SOURCE?: ViteDataSourceMode;
  readonly VITE_ETATS_X3_SOURCE?: ViteDataSourceMode;
  readonly VITE_TYPES_MATIERE_SOURCE?: ViteDataSourceMode;
  readonly VITE_DECHETS_FONDERIE_SOURCE?: ViteDataSourceMode;
  readonly VITE_CONTROLE_POIDS_SOURCE?: ViteDataSourceMode;
  readonly VITE_RECEPTIONS_U2_SOURCE?: ViteDataSourceMode;
  readonly VITE_REFERENCES_FABRICATION_SOURCE?: ViteDataSourceMode;
  readonly VITE_BILAN_U2_SOURCE?: ViteDataSourceMode;
  readonly VITE_SORTIES_X3_SOURCE?: ViteDataSourceMode;
  readonly VITE_HISTORIQUE_IMPORTS_SOURCE?: ViteDataSourceMode;

  // Verticales sans backend a ce jour.
  readonly VITE_DASHBOARD_SOURCE?: ViteDataSourceMode;
  readonly VITE_BILAN_FONDERIE_SOURCE?: ViteDataSourceMode;
  readonly VITE_BILAN_UAP2_SOURCE?: ViteDataSourceMode;
  readonly VITE_INVENTAIRE_SOURCE?: ViteDataSourceMode;
  readonly VITE_RAPPORTS_SOURCE?: ViteDataSourceMode;
  readonly VITE_ALERTES_SOURCE?: ViteDataSourceMode;
  readonly VITE_AUDIT_SOURCE?: ViteDataSourceMode;
  readonly VITE_ADMINISTRATION_SOURCE?: ViteDataSourceMode;
  readonly VITE_SYNCHRONISATION_X3_SOURCE?: ViteDataSourceMode;

  /** @deprecated Ancien nom de VITE_DECHETS_FONDERIE_SOURCE, encore accepte. */
  readonly VITE_DECHETS_SOURCE?: ViteDataSourceMode;
  readonly VITE_IMPORTS_HISTORIQUE_SOURCE?: ViteDataSourceMode;
  readonly VITE_ADMIN_SOURCE?: ViteDataSourceMode;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

declare module '*.vue' {
  import type { DefineComponent } from 'vue';
  const component: DefineComponent<Record<string, unknown>, Record<string, unknown>, unknown>;
  export default component;
}
