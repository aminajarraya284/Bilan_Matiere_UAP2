<script setup lang="ts">
import { computed, ref } from 'vue';
import { useAuthStore } from '@/features/auth/auth.store';
import PageHeader from '@/components/ui/PageHeader.vue';
import { useAppDialog } from '@/composables/useAppDialog';
import { Database, FileUp, LoaderCircle, RefreshCw } from 'lucide-vue-next';
import {
  referenceService,
  type GammeReference,
  type NomenclatureReference,
  type ReferenceTabKey
} from './reference.service';

const authStore = useAuthStore();
const dialog = useAppDialog();
const isAdmin = computed(() => authStore.userRole === 'Admin');
const activeTab = ref<ReferenceTabKey>('gamme');
const gammeReferences = ref<GammeReference[]>(referenceService.getGammes());
const nomenclatureReferences = ref<NomenclatureReference[]>(referenceService.getNomenclatures());
const gammeFileInput = ref<HTMLInputElement | null>(null);
const nomenclatureFileInput = ref<HTMLInputElement | null>(null);
const importing = ref<ReferenceTabKey | null>(null);
const PAGE_SIZE = 100;
const gammePage = ref(1);
const nomenclaturePage = ref(1);
const gammePageCount = computed(() => Math.max(1, Math.ceil(gammeReferences.value.length / PAGE_SIZE)));
const nomenclaturePageCount = computed(() => Math.max(1, Math.ceil(nomenclatureReferences.value.length / PAGE_SIZE)));
const paginatedGammes = computed(() =>
  gammeReferences.value.slice((gammePage.value - 1) * PAGE_SIZE, gammePage.value * PAGE_SIZE)
);
const paginatedNomenclatures = computed(() =>
  nomenclatureReferences.value.slice((nomenclaturePage.value - 1) * PAGE_SIZE, nomenclaturePage.value * PAGE_SIZE)
);

const loadDataTestGamme = async () => {
  gammeReferences.value = referenceService.resetGammes();
  gammePage.value = 1;
  await dialog.alert('Data test gamme chargée.', { title: 'Références fabrication', tone: 'success' });
};

const loadDataTestNomenclature = async () => {
  nomenclatureReferences.value = referenceService.resetNomenclatures();
  nomenclaturePage.value = 1;
  await dialog.alert('Data test nomenclature chargée.', { title: 'Références fabrication', tone: 'success' });
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
      gammeReferences.value = result.items;
      gammePage.value = 1;
      await showImportResult(result.importedCount, result.ignoredCount, result.fileName);
    } else {
      const result = await referenceService.importNomenclatures(file);
      nomenclatureReferences.value = result.items;
      nomenclaturePage.value = 1;
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

        <div v-if="isAdmin" class="mt-4 grid gap-3 md:grid-cols-3">
          <input
            ref="gammeFileInput"
            class="hidden"
            type="file"
            accept=".xlsx,.xls"
            @change="handleImport('gamme', $event)"
          />
          <button
            class="flex items-center gap-2 rounded-xl border border-slate-200 px-4 py-3 text-xs font-bold text-slate-700"
            @click="loadDataTestGamme"
          >
            <Database class="w-4 h-4 text-blue-700" />
            Utiliser data test gamme
          </button>
          <button
            class="flex items-center gap-2 rounded-xl border border-slate-200 px-4 py-3 text-xs font-bold text-slate-700"
            :disabled="importing !== null"
            @click="selectImportFile('gamme')"
          >
            <LoaderCircle v-if="importing === 'gamme'" class="w-4 h-4 animate-spin text-blue-700" />
            <FileUp v-else class="w-4 h-4 text-blue-700" />
            {{ importing === 'gamme' ? 'Import en cours…' : 'Importer GAMMEFAB' }}
          </button>
          <button
            class="flex items-center gap-2 rounded-xl bg-blue-700 px-4 py-3 text-xs font-bold text-white"
            @click="handleSync('gamme')"
          >
            <RefreshCw class="w-4 h-4" />
            Synchroniser gamme
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
            <tbody>
              <tr
                v-for="row in paginatedGammes"
                :key="row.codeArticle + row.numeroOperation"
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
          <span>{{ gammeReferences.length }} référence(s) — 100 lignes maximum par page</span>
          <div class="flex items-center gap-2">
            <button
              class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="gammePage <= 1"
              @click="gammePage--"
            >
              Précédent
            </button>
            <span>Page {{ gammePage }} / {{ gammePageCount }}</span>
            <button
              class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="gammePage >= gammePageCount"
              @click="gammePage++"
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
            @click="loadDataTestNomenclature"
          >
            <Database class="w-4 h-4 text-blue-700" />
            Utiliser data test nomenclature
          </button>
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
            <tbody>
              <tr
                v-for="row in paginatedNomenclatures"
                :key="row.articleParent + row.composant"
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
          <span>{{ nomenclatureReferences.length }} référence(s) — 100 lignes maximum par page</span>
          <div class="flex items-center gap-2">
            <button
              class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="nomenclaturePage <= 1"
              @click="nomenclaturePage--"
            >
              Précédent
            </button>
            <span>Page {{ nomenclaturePage }} / {{ nomenclaturePageCount }}</span>
            <button
              class="rounded-lg border border-slate-200 px-3 py-2 font-bold disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="nomenclaturePage >= nomenclaturePageCount"
              @click="nomenclaturePage++"
            >
              Suivant
            </button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
