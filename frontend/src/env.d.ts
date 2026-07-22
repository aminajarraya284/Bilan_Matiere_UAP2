/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  readonly VITE_USE_REAL_API?: string;
  readonly VITE_TYPES_MATIERE_SOURCE?: 'mock' | 'api';
  readonly VITE_DECHETS_SOURCE?: 'mock' | 'api';
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

declare module '*.vue' {
  import type { DefineComponent } from 'vue';
  const component: DefineComponent<Record<string, unknown>, Record<string, unknown>, unknown>;
  export default component;
}
