<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useAuthStore } from '@/features/auth/auth.store';
import PageHeader from '@/components/ui/PageHeader.vue';
import { useAppDialog } from '@/composables/useAppDialog';
import { AlertTriangle, FileUp, LoaderCircle, RefreshCw } from 'lucide-vue-next';
import {
  DEFAULT_REFERENCE_PAGE_SIZE,
  REFERENCE_PAGE_SIZES,
  referenceService,
  type GammeReference,
  type NomenclatureReference,
  type ReferencePageSize,
  type ReferenceTabKey
} from './reference.service';
import { isApiSource } from '@/config/dataSources';

const authStore = useAuthStore();
const dialog = useAppDialog();
const isAdmin = computed(() => authStore.userRole === 'Admin');
const activeTab = ref<ReferenceTabKey>('gamme');
const gammeFileInput = ref<HTMLInputElement | null>(null);
const nomenclatureFileInput = ref<HTMLInputElement | null>(null);
const importing = ref<ReferenceTabKey | null>(null);
const pageSizes = REFERENCE_PAGE_SIZES;

/**
 * Etat de pagination propre a chaque onglet : la page des Gammes ne doit jamais
 * etre partagee avec celle des Nomenclatures.
 */
interface TabState<T> {
  rows: T[];
  page: number;
  pageSize: ReferencePageSize;
  total: number;
  loading: boolean;
  error: string;
  loaded: boolean;
}

const createTabState = <T,>(): TabState<T> => ({
  rows: [],
  page: 1,
  pageSize: DEFAULT_REFERENCE_PAGE_SIZE,
  total: 0,
  loading: false,
  error: '',
  loaded: false
});

const gamme = ref<TabState<GammeReference>>(createTabState<GammeReference>());
const nomenclature = ref<TabState<NomenclatureReference>>(createTabState<NomenclatureReference>());
const gammeSearch = ref('');
const nomenclatureSearch = ref('');
const normalizedGammeSearch = computed(() => gammeSearch.value.trim().toUpperCase());
const normalizedNomenclatureSearch = computed(() => nomenclatureSearch.value.trim().toUpperCase());

const countPages = (total: number, pageSize: number) => Math.max(1, Math.ceil(total / pageSize));
const gammePageCount = computed(() => countPages(gamme.value.total, gamme.value.pageSize));
const nomenclaturePageCount = computed(() => countPages(nomenclature.value.total, nomenclature.value.pageSize));

// Le tableau precedent reste visible pendant le chargement de la page suivante :
// l'etat vide n'apparait qu'au tout premier affichage d'un onglet.
const gammeFirstLoad = computed(() => gamme.value.loading && !gamme.value.loaded);
const nomenclatureFirstLoad = computed(() => nomenclature.value.loading && !nomenclature.value.loaded);

// Une requete en vol par onglet : toute nouvelle demande annule la precedente, afin
// qu'une reponse tardive ne vienne jamais ecraser une page plus recente.
const controllers: Record<ReferenceTabKey, AbortController | null> = { gamme: null, nomenclature: null };

const isAbort = (error: unknown): boolean => {
  const e = error as { code?: string; name?: string };
  return e?.code === 'ERR_CANCELED' || e?.name === 'CanceledError' || e?.name === 'AbortError';
};

const cancel = (tab: ReferenceTabKey) => {
  controllers[tab]?.abort();
  controllers[tab] = null;
};

const loadGammes = async () => {
  cancel('gamme');
  const controller = new AbortController();
  controllers.gamme = controller;
  const state = gamme.value;
  state.loading = true;
  state.error = '';
  try {
    const { items, total } = await referenceService.getGammes({
      page: state.page,
      pageSize: state.pageSize,
      codeArticle: normalizedGammeSearch.value || undefined,
      signal: controller.signal
    });
    if (controller.signal.aborted) return;
    state.rows = items;
    state.total = total;
    state.loaded = true;
  } catch (error) {
    if (isAbort(error)) return;
    state.error = error instanceof Error ? error.message : 'Le chargement des gammes a échoué.';
  } finally {
    if (controllers.gamme === controller) {
      controllers.gamme = null;
      state.loading = false;
    }
  }
};

const loadNomenclatures = async () => {
  cancel('nomenclature');
  const controller = new AbortController();
  controllers.nomenclature = controller;
  const state = nomenclature.value;
  state.loading = true;
  state.error = '';
  try {
    const { items, total } = await referenceService.getNomenclatures({
      page: state.page,
      pageSize: state.pageSize,
      codeArticleParent: normalizedNomenclatureSearch.value || undefined,
      signal: controller.signal
    });
    if (controller.signal.aborted) return;
    state.rows = items;
    state.total = total;
    state.loaded = true;
  } catch (error) {
    if (isAbort(error)) return;
    state.error = error instanceof Error ? error.message : 'Le chargement des nomenclatures a échoué.';
  } finally {
    if (controllers.nomenclature === controller) {
      controllers.nomenclature = null;
      state.loading = false;
    }
  }
};

const loadActiveTab = () => (activeTab.value === 'gamme' ? loadGammes() : loadNomenclatures());

// Seul l'onglet affiche est charge : ouvrir l'ecran ne rapatrie plus les deux referentiels.
onMounted(loadActiveTab);
onBeforeUnmount(() => {
  cancel('gamme');
  cancel('nomenclature');
});

watch(activeTab, (tab) => {
  const state = tab === 'gamme' ? gamme.value : nomenclature.value;
  // La page de chaque onglet est conservee. On ne recharge que si l'onglet n'a
  // jamais abouti (premier affichage, ou tentative precedente en erreur).
  if (!state.loaded) void loadActiveTab();
});

const goToPage = (tab: ReferenceTabKey, page: number) => {
  const state = tab === 'gamme' ? gamme.value : nomenclature.value;
  const max = tab === 'gamme' ? gammePageCount.value : nomenclaturePageCount.value;
  const target = Math.min(Math.max(1, page), max);
  if (target === state.page && state.loaded) return;
  state.page = target;
  void (tab === 'gamme' ? loadGammes() : loadNomenclatures());
};

const changePageSize = (tab: ReferenceTabKey, size: ReferencePageSize) => {
  const state = tab === 'gamme' ? gamme.value : nomenclature.value;
  if (state.pageSize === size) return;
  state.pageSize = size;
  state.page = 1; // La page courante n'a plus de sens avec une autre taille.
  void (tab === 'gamme' ? loadGammes() : loadNomenclatures());
};

const retry = (tab: ReferenceTabKey) => void (tab === 'gamme' ? loadGammes() : loadNomenclatures());

// La recherche est appliquee par le backend (filtre `codeArticle`). Le debounce evite
// une requete par frappe ; l'annulation garantit qu'une reponse lente ne remplace pas
// un resultat plus recent.
let gammeSearchTimer: ReturnType<typeof setTimeout> | undefined;
const handleGammeSearchInput = () => {
  if (gammeSearchTimer) clearTimeout(gammeSearchTimer);
  gammeSearchTimer = setTimeout(() => {
    gamme.value.page = 1;
    void loadGammes();
  }, 300);
};

let nomenclatureSearchTimer: ReturnType<typeof setTimeout> | undefined;
const handleNomenclatureSearchInput = () => {
  if (nomenclatureSearchTimer) clearTimeout(nomenclatureSearchTimer);
  nomenclatureSearchTimer = setTimeout(() => {
    nomenclature.value.page = 1;
    void loadNomenclatures();
  }, 300);
};

const selectImportFile = (target: ReferenceTabKey) => {
  if (!isAdmin.value) return;
  (target === 'gamme' ? gammeFileInput.value : nomenclatureFileInput.value)?.click();
};

const handleImport = async (target: ReferenceTabKey, event: Event) => {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file || !isAdmin.value) return;

  importing.value = target;
  try {
    if (target === 'gamme') {
      const result = await referenceService.importGammes(file);
      gamme.value.page = 1;
      if (isApiSource('referencesFabrication')) await loadGammes();
      else {
        gamme.value.rows = result.items.slice(0, gamme.value.pageSize);
        gamme.value.total = result.items.length;
        gamme.value.loaded = true;
      }
      await showImportResult(result.importedCount, result.ignoredCount, result.fileName);
    } else {
      const result = await referenceService.importNomenclatures(file);
      nomenclature.value.page = 1;
      if (isApiSource('referencesFabrication')) await loadNomenclatures();
      else {
        nomenclature.value.rows = result.items.slice(0, nomenclature.value.pageSize);
        nomenclature.value.total = result.items.length;
        nomenclature.value.loaded = true;
      }
      await showImportResult(result.importedCount, result.ignoredCount, result.fileName);
    }
  } catch (error) {
    await dialog.alert(error instanceof Error ? error.message : "L'import du fichier a échoué.", {
      title: "Erreur d'import",
      tone: 'danger'
    });
  } finally {
    importing.value = null;
    input.value = '';
  }
};

const showImportResult = async (importedCount: number, ignoredCount: number, fileName: string) => {
  const ignoredMessage = ignoredCount ? ` ${ignoredCount} ligne(s) invalide(s) ou en double ont été ignorées.` : '';
  await dialog.alert(`${importedCount} référence(s) importée(s) depuis « ${fileName} ».${ignoredMessage}`, {
    title: 'Import terminé',
    tone: ignoredCount ? 'warning' : 'success'
  });
};

const handleSync = async (target: ReferenceTabKey) => {
  await dialog.alert(target === 'gamme' ? 'Synchronisation gamme simulée.' : 'Synchronisation nomenclature simulée.', {
    title: 'Références fabrication',
    tone: 'success'
  });
};
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Références de fabrication"
      description="Import, synchronisation et consultation des références métier utilisées dans les réceptions Usinage 2."
    />

    <div class="app-tabs w-fit">
      <button class="app-tab" :class="activeTab === 'gamme' ? 'app-tab--active' : ''" @click="activeTab = 'gamme'">
        Gamme de fabrication
      </button>
      <button
        class="app-tab"
        :class="activeTab === 'nomenclature' ? 'app-tab--active' : ''"
        @click="activeTab = 'nomenclature'"
      >
        Nomenclature
      </button>
    </div>

    <section v-if="activeTab === 'gamme'" class="space-y-4">
      <div class="bg-white border border-slate-100 rounded-xl p-5 shadow-sm">
        <h3 class="text-sm font-black text-slate-800 uppercase tracking-wider">Gamme de fabrication</h3>

        <div v-if="isAdmin" class="mt-4 flex">
          <input
            ref="gammeFileInput"
            class="hidden"
            type="file"
            accept=".xlsx,.xls"
            @change="handleImport('gamme', $event)"
          />
          <button
            class="flex w-full items-center justify-center gap-2 rounded-xl border border-slate-200 px-4 py-3 text-xs font-bold text-slate-700 sm:w-auto sm:min-w-64"
            :disabled="importing !== null"
            @click="selectImportFile('gamme')"
          >
            <LoaderCircle v-if="importing === 'gamme'" class="w-4 h-4 animate-spin text-blue-700" />
            <FileUp v-else class="w-4 h-4 text-blue-700" />
            {{ importing === 'gamme' ? 'Import en cours…' : 'Importer GAMMEFAB' }}
          </button>
        </div>

        <div class="mt-4">
          <label for="gamme-code-search" class="mb-1 block text-[10px] font-black uppercase tracking-wider text-slate-500">
            Rechercher par code article exact
          </label>
          <input
            id="gamme-code-search"
            v-model="gammeSearch"
            type="search"
            class="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm font-semibold text-slate-700 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            placeholder="Ex. 0101A03-1"
            @input="handleGammeSearchInput"
          />
          <p class="mt-1 text-[10px] font-semibold text-slate-400">
            La recherche est effectuée par le serveur sur le code article complet (correspondance exacte).
          </p>
        </div>

        <div
          v-if="gamme.error"
          class="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-rose-100 bg-rose-50 p-4"
        >
          <span class="flex items-center gap-2 text-xs font-semibold text-rose-700">
            <AlertTriangle class="h-4 w-4" />
            {{ gamme.error }}
          </span>
          <button
            class="rounded-lg bg-rose-700 px-3 py-2 text-[10px] font-black uppercase text-white hover:bg-rose-800"
            @click="retry('gamme')"
          >
            Réessayer
          </button>
        </div>

        <div class="table-wrapper mt-5 border border-slate-200 rounded-xl">
          <table class="min-w-[1200px] w-full text-left">
            <thead class="bg-slate-50 text-[10px] uppercase tracking-wider text-slate-500">
              <tr>
                <th class="p-3">Code article</th>
                <th class="p-3">Désignation article</th>
                <th class="p-3">Atelier</th>
                <th class="p-3">Poste</th>
                <th class="p-3">Groupe poste de charge</th>
                <th class="p-3">Numéro opération</th>
                <th class="p-3">Désignation opération</th>
                <th class="p-3">Alternative</th>
                <th class="p-3">Date fin validité</th>
                <th class="p-3">Source</th>
                <th class="p-3">Statut</th>
              </tr>
            </thead>
            <tbody :class="gamme.loading && gamme.loaded ? 'opacity-60 transition-opacity' : ''">
              <tr v-if="gammeFirstLoad">
                <td class="p-4 text-center text-xs font-semibold text-slate-400" colspan="11">
                  Chargement de la page…
                </td>
              </tr>
              <tr v-else-if="!gamme.rows.length && !gamme.error">
                <td class="p-4 text-center text-xs font-semibold text-slate-400" colspan="11">
                  Aucune référence pour cette page.
                </td>
              </tr>
              <tr
                v-for="row in gamme.rows"
                :key="row.codeArticle + row.numeroOperation + row.alternative"
                class="border-t border-slate-100"
              >
                <td class="p-3 font-mono text-slate-700">{{ row.codeArticle }}</td>
                <td class="p-3">{{ row.designationArticle }}</td>
                <td class="p-3 font-mono">{{ row.atelier }}</td>
                <td class="p-3">{{ row.poste }}</td>
                <td class="p-3">{{ row.groupePosteCharge }}</td>
                <td class="p-3 font-mono">{{ row.numeroOperation }}</td>
                <td class="p-3">{{ row.designationOperation }}</td>
                <td class="p-3 font-mono">{{ row.alternative }}</td>
                <td class="p-3 font-mono">{{ row.dateFinValidite }}</td>
                <td class="p-3 text-slate-500">{{ row.source }}</td>
                <td class="p-3">
                  <span class="status-success">
                    {{ row.statut }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="mt-3 flex flex-wrap items-center justify-between gap-3 text-xs text-slate-500">
          <span class="flex items-center gap-2">
            <LoaderCircle v-if="gamme.loading" class="h-3.5 w-3.5 animate-spin text-blue-700" />
            <span>
              {{ gamme.total.toLocaleString('fr-FR') }} référence(s)
              <span v-if="normalizedGammeSearch">pour « {{ normalizedGammeSearch }} »</span>
            </span>
          </span>
          <div class="flex items-center gap-2">
            <label for="gamme-page-size" class="font-bold uppercase tracking-wider text-[10px]">Par page</label>
            <select
              id="gamme-page-size"
              class="rounded-lg border border-slate-200 px-2 py-2 font-bold"
              :value="gamme.pageSize"
              @change="changePageSize('gamme', Number(($event.target as HTMLSelectElement).value) as ReferencePageSize)"
            >
              <option v-for="size in pageSizes" :key="size" :value="size">{{ size }}</option>
            </select>
            <button
              class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="gamme.page <= 1 || gamme.loading"
              @click="goToPage('gamme', gamme.page - 1)"
            >
              Précédent
            </button>
            <span>Page {{ gamme.page }} / {{ gammePageCount }}</span>
            <button
              class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="gamme.page >= gammePageCount || gamme.loading"
              @click="goToPage('gamme', gamme.page + 1)"
            >
              Suivant
            </button>
          </div>
        </div>
      </div>
    </section>

    <section v-else class="space-y-4">
      <div class="bg-white border border-slate-100 rounded-xl p-5 shadow-sm">
        <h3 class="text-sm font-black text-slate-800 uppercase tracking-wider">Nomenclature</h3>

        <div v-if="isAdmin" class="mt-4 grid gap-3 md:grid-cols-3">
          <input
            ref="nomenclatureFileInput"
            class="hidden"
            type="file"
            accept=".xlsx,.xls"
            @change="handleImport('nomenclature', $event)"
          />
          <button
            class="flex items-center gap-2 rounded-xl border border-slate-200 px-4 py-3 text-xs font-bold text-slate-700"
            :disabled="importing !== null"
            @click="selectImportFile('nomenclature')"
          >
            <LoaderCircle v-if="importing === 'nomenclature'" class="w-4 h-4 animate-spin text-blue-700" />
            <FileUp v-else class="w-4 h-4 text-blue-700" />
            {{ importing === 'nomenclature' ? 'Import en cours…' : 'Importer BOMARTPARENT' }}
          </button>
          <button
            class="flex items-center gap-2 rounded-xl bg-blue-700 px-4 py-3 text-xs font-bold text-white"
            @click="handleSync('nomenclature')"
          >
            <RefreshCw class="w-4 h-4" />
            Synchroniser nomenclature
          </button>
        </div>

        <div class="mt-4">
          <label
            for="nomenclature-code-search"
            class="mb-1 block text-[10px] font-black uppercase tracking-wider text-slate-500"
          >
            Rechercher par code article parent exact
          </label>
          <input
            id="nomenclature-code-search"
            v-model="nomenclatureSearch"
            type="search"
            class="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm font-semibold text-slate-700 outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            placeholder="Ex. 0101A03-1"
            @input="handleNomenclatureSearchInput"
          />
          <p class="mt-1 text-[10px] font-semibold text-slate-400">
            La recherche est effectuée par le serveur sur le code article parent complet (correspondance exacte).
          </p>
        </div>

        <div
          v-if="nomenclature.error"
          class="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-rose-100 bg-rose-50 p-4"
        >
          <span class="flex items-center gap-2 text-xs font-semibold text-rose-700">
            <AlertTriangle class="h-4 w-4" />
            {{ nomenclature.error }}
          </span>
          <button
            class="rounded-lg bg-rose-700 px-3 py-2 text-[10px] font-black uppercase text-white hover:bg-rose-800"
            @click="retry('nomenclature')"
          >
            Réessayer
          </button>
        </div>

        <div class="table-wrapper mt-5 border border-slate-200 rounded-xl">
          <table class="min-w-[1400px] w-full text-left">
            <thead class="bg-slate-50 text-[10px] uppercase tracking-wider text-slate-500">
              <tr>
                <th class="p-3">Article parent</th>
                <th class="p-3">Désignation article parent</th>
                <th class="p-3">Composant</th>
                <th class="p-3">Désignation composant</th>
                <th class="p-3">Niveau</th>
                <th class="p-3">Séquence</th>
                <th class="p-3">Quantité lien</th>
                <th class="p-3">Quantité lien cumulée</th>
                <th class="p-3">Unité</th>
                <th class="p-3">Catégorie</th>
                <th class="p-3">Type lien</th>
                <th class="p-3">Pourcentage rebut</th>
                <th class="p-3">Source</th>
                <th class="p-3">Statut</th>
              </tr>
            </thead>
            <tbody :class="nomenclature.loading && nomenclature.loaded ? 'opacity-60 transition-opacity' : ''">
              <tr v-if="nomenclatureFirstLoad">
                <td class="p-4 text-center text-xs font-semibold text-slate-400" colspan="14">
                  Chargement de la page…
                </td>
              </tr>
              <tr v-else-if="!nomenclature.rows.length && !nomenclature.error">
                <td class="p-4 text-center text-xs font-semibold text-slate-400" colspan="14">
                  Aucune référence pour cette page.
                </td>
              </tr>
              <tr
                v-for="row in nomenclature.rows"
                :key="row.articleParent + row.composant + row.sequence"
                class="border-t border-slate-100"
              >
                <td class="p-3 font-mono text-slate-700">{{ row.articleParent }}</td>
                <td class="p-3">{{ row.designationArticleParent }}</td>
                <td class="p-3 font-mono">{{ row.composant }}</td>
                <td class="p-3">{{ row.designationComposant }}</td>
                <td class="p-3 font-mono">{{ row.niveau }}</td>
                <td class="p-3 font-mono">{{ row.sequence }}</td>
                <td class="p-3 font-mono">{{ row.quantiteLien }}</td>
                <td class="p-3 font-mono">{{ row.quantiteLienCumulee }}</td>
                <td class="p-3">{{ row.unite }}</td>
                <td class="p-3">{{ row.categorie }}</td>
                <td class="p-3">{{ row.typeLien }}</td>
                <td class="p-3 font-mono">{{ row.pourcentageRebut }}</td>
                <td class="p-3 text-slate-500">{{ row.source }}</td>
                <td class="p-3">
                  <span class="status-info">
                    {{ row.statut }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="mt-3 flex flex-wrap items-center justify-between gap-3 text-xs text-slate-500">
          <span class="flex items-center gap-2">
            <LoaderCircle v-if="nomenclature.loading" class="h-3.5 w-3.5 animate-spin text-blue-700" />
            <span>{{ nomenclature.total.toLocaleString('fr-FR') }} référence(s)</span>
          </span>
          <div class="flex items-center gap-2">
            <label for="nomenclature-page-size" class="font-bold uppercase tracking-wider text-[10px]">Par page</label>
            <select
              id="nomenclature-page-size"
              class="rounded-lg border border-slate-200 px-2 py-2 font-bold"
              :value="nomenclature.pageSize"
              @change="
                changePageSize('nomenclature', Number(($event.target as HTMLSelectElement).value) as ReferencePageSize)
              "
            >
              <option v-for="size in pageSizes" :key="size" :value="size">{{ size }}</option>
            </select>
            <button
              class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="nomenclature.page <= 1 || nomenclature.loading"
              @click="goToPage('nomenclature', nomenclature.page - 1)"
            >
              Précédent
            </button>
            <span>Page {{ nomenclature.page }} / {{ nomenclaturePageCount }}</span>
            <button
              class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="nomenclature.page >= nomenclaturePageCount || nomenclature.loading"
              @click="goToPage('nomenclature', nomenclature.page + 1)"
            >
              Suivant
            </button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
