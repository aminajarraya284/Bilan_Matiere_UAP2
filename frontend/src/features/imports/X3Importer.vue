<template>
  <div class="space-y-6">
    <PageHeader
      title="Importation des données X3"
      description="Importer et contrôler les sorties magasin issues d’un fichier Excel X3."
    />

    <AppCard accent>
      <div class="space-y-4">
        <SectionHeader
          title="Source d’import"
          description="Sélectionnez un fichier Excel X3 ou utilisez la source locale."
        />
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
          <input
            class="min-w-0 flex-1"
            type="file"
            accept=".xlsx,.xls"
            aria-label="Fichier Excel X3"
            @change="onFile"
          />
          <AppButton :loading="isLoadingReference" @click="loadIntegratedReference"
            >Charger les données locales</AppButton
          >
        </div>
        <div v-if="referenceSummary" class="rounded-lg border border-blue-200 bg-blue-50 p-3 text-sm text-blue-900">
          Données locales : <strong>{{ referenceSummary.sorties }}</strong> sorties magasin,
          <strong>{{ referenceSummary.ofs }}</strong> OF, <strong>{{ referenceSummary.articles }}</strong> articles et
          <strong>{{ referenceSummary.materials }}</strong> types matière.
        </div>
      </div>
    </AppCard>

    <Transition name="page-motion">
      <ProgressSteps v-if="isLoadingReference" label="Chargement et contrôle de la référence X3" kind="import" />
    </Transition>

    <section v-if="parses.length" class="space-y-4">
      <article v-for="(p, i) in parses" :key="i" class="surface-card space-y-5 p-5">
        <SectionHeader
          :title="`Feuille ${p.sheetName}`"
          :description="`${p.ofs.length} OF détecté(s) dans cette feuille.`"
        />
        <p class="rounded-lg bg-slate-50 p-3 text-xs leading-relaxed text-slate-600">
          <span class="font-semibold text-slate-700">Colonnes :</span> {{ p.headers.join(', ') }}
        </p>
        <div class="flex flex-wrap gap-2">
          <AppButton @click="importOfs(p.ofs)">Importer ces OF comme réceptions de test</AppButton>
          <AppButton variant="secondary" @click="downloadOfList(p.ofs)">Télécharger la liste OF (.txt)</AppButton>
        </div>

        <div class="space-y-4 border-t border-slate-200 pt-5">
          <SectionHeader
            title="Correspondance des colonnes"
            description="Associez les colonnes du fichier aux données attendues avant l’import."
            :level="4"
          >
            <template #action
              ><AppButton variant="secondary" size="sm" @click="autoDetect(i)"
                >Détection automatique</AppButton
              ></template
            >
          </SectionHeader>
          <div class="grid gap-4 md:grid-cols-3">
            <label class="space-y-1.5 text-sm font-semibold text-slate-700"
              >Colonne OF
              <select v-model="mappings[i].ofIndex" class="w-full">
                <option v-for="(h, idx) in p.headers" :key="idx" :value="idx">{{ idx }} — {{ h || '(vide)' }}</option>
              </select>
            </label>
            <label class="space-y-1.5 text-sm font-semibold text-slate-700"
              >Article
              <select v-model="mappings[i].articleIndex" class="w-full">
                <option value="">(aucun)</option>
                <option v-for="(h, idx) in p.headers" :key="'a' + idx" :value="idx">
                  {{ idx }} — {{ h || '(vide)' }}
                </option>
              </select>
            </label>
            <label class="space-y-1.5 text-sm font-semibold text-slate-700"
              >Poids
              <select v-model="mappings[i].poidsIndex" class="w-full">
                <option value="">(aucun)</option>
                <option v-for="(h, idx) in p.headers" :key="'w' + idx" :value="idx">
                  {{ idx }} — {{ h || '(vide)' }}
                </option>
              </select>
            </label>
          </div>
          <div class="flex flex-wrap gap-x-5 gap-y-2 text-sm text-slate-700">
            <label class="flex items-center gap-2"
              ><input v-model="mappings[i].sync" type="checkbox" /> Synchronisation ERP</label
            >
            <label class="flex items-center gap-2"
              ><input v-model="mappings[i].backup" type="checkbox" /> Sauvegarder avant écriture</label
            >
          </div>
          <div class="flex flex-wrap gap-2">
            <AppButton @click="importMapped(i)">Importer avec la correspondance</AppButton>
            <AppButton variant="secondary" @click="previewMapped(i)">Prévisualiser 10 lignes</AppButton>
          </div>
        </div>

        <details class="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm text-slate-700">
          <summary class="cursor-pointer font-semibold">Aperçu des 10 premières lignes</summary>
          <pre class="technical-value mt-3 max-h-80 overflow-auto whitespace-pre-wrap text-xs">{{
            p.rows.slice(0, 10)
          }}</pre>
        </details>
      </article>
    </section>

    <section v-if="created?.length" class="surface-card p-5">
      <SectionHeader title="Réceptions créées" description="Réceptions générées dans l’environnement de test." />
      <ul class="mt-4 grid gap-2 text-sm text-slate-700 sm:grid-cols-2">
        <li v-for="r in created" :key="r.id" class="rounded-lg bg-slate-50 px-3 py-2">
          <span class="technical-value font-semibold">{{ r.numeroOF }}</span> — identifiant {{ r.id }}
        </li>
      </ul>
    </section>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { testDataRepository } from '@/services/testDataRepository.service';
import { useAppDialog } from '@/composables/useAppDialog';
import PageHeader from '@/components/ui/PageHeader.vue';
import SectionHeader from '@/components/ui/SectionHeader.vue';
import AppButton from '@/components/ui/AppButton.vue';
import AppCard from '@/components/ui/AppCard.vue';
import ProgressSteps from '@/components/ui/ProgressSteps.vue';
import { ReceptionUsinage2Mouvement } from '@/features/receptions-usinage2/receptions.types';
import {
  X3Cell,
  X3ParseResult,
  parseX3File,
  insertOfsAsReceptions,
  mapRowsToReceptions,
  detectMappingFromHeaders
} from './importer.service';

type SheetMapping = {
  ofIndex: number | '';
  articleIndex: number | '';
  poidsIndex: number | '';
  sync: boolean;
  backup: boolean;
};

const dialog = useAppDialog();

const parses = ref<X3ParseResult[]>([]);
const created = ref<ReceptionUsinage2Mouvement[]>([]);
const isLoadingReference = ref(false);
const referenceSummary = ref<{ sorties: number; ofs: number; articles: number; materials: number } | null>(null);
const mappings = reactive<SheetMapping[]>([]);

function resetMappings(results: X3ParseResult[]) {
  mappings.length = 0;
  results.forEach((p, idx) => {
    mappings[idx] = {
      ofIndex: p.headers.length ? 0 : '',
      articleIndex: '',
      poidsIndex: '',
      sync: false,
      backup: true
    };
  });
}

function buildLocalParseResult(): X3ParseResult[] {
  const dataset = testDataRepository.getDataset();
  const ordres = dataset.x3OrdresFabrication;
  const composants = dataset.x3ComposantsOF;
  const headers = ['OF', 'Article', 'Type matiere', 'Quantite consommee'];
  const rows = composants.map((composant) => [
    composant.numeroOF,
    ordres.find((of) => of.numeroOF === composant.numeroOF)?.codeArticleParent || '',
    composant.codeComposant,
    composant.quantiteConsommee
  ]);
  return [
    {
      sheetName: 'TEST_SORTIES_MAGASIN',
      headers,
      rows,
      ofs: ordres.map((of) => of.numeroOF)
    }
  ];
}

async function onFile(e: Event) {
  const input = e.target as HTMLInputElement;
  const f = input.files && input.files[0];
  if (!f) return;
  try {
    const res = await parseX3File(f);
    parses.value = res;
    created.value = [];
    referenceSummary.value = null;
    resetMappings(res);
  } catch (error) {
    parses.value = [];
    created.value = [];
    mappings.length = 0;
    input.value = '';
    await dialog.alert(error instanceof Error ? error.message : 'Import impossible.', {
      title: 'Import',
      tone: 'warning'
    });
  }
}

async function loadIntegratedReference() {
  isLoadingReference.value = true;
  try {
    const results = buildLocalParseResult();
    parses.value = results;
    created.value = [];
    const dataset = testDataRepository.getDataset();
    referenceSummary.value = {
      sorties: dataset.x3MouvementsStock.length,
      ofs: dataset.x3OrdresFabrication.length,
      articles: dataset.refNomenclature.length,
      materials: dataset.refTypesMatiere.length
    };
    resetMappings(results);
  } finally {
    isLoadingReference.value = false;
  }
}

async function importOfs(ofs: string[]) {
  if (!ofs || !ofs.length) return;
  const createdRecs = insertOfsAsReceptions(ofs, { user: 'IMPORT_UI' });
  created.value = createdRecs;
  await dialog.alert(`${createdRecs.length} receptions de test ajoutees.`, {
    title: 'Import termine',
    tone: 'success'
  });
}

function downloadOfList(ofs: string[]) {
  const blob = new Blob([ofs.join('\n')], { type: 'text/plain;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'of_list.txt';
  a.click();
  URL.revokeObjectURL(url);
}

async function importMapped(sheetIndex: number) {
  const p = parses.value[sheetIndex];
  const m = mappings[sheetIndex];
  if (!p || !m) {
    await dialog.alert('Mapping introuvable.', { title: 'Import', tone: 'warning' });
    return;
  }
  if (typeof m.ofIndex !== 'number' || m.ofIndex < 0) {
    await dialog.alert('Selectionner la colonne OF.', { title: 'Import', tone: 'warning' });
    return;
  }
  const createdRecs = mapRowsToReceptions(
    p.headers,
    p.rows,
    {
      ofIndex: m.ofIndex,
      articleIndex: typeof m.articleIndex === 'number' ? m.articleIndex : undefined,
      poidsIndex: typeof m.poidsIndex === 'number' ? m.poidsIndex : undefined,
      user: 'IMPORT_UI'
    },
    { sync: !!m.sync, backup: !!m.backup }
  );
  created.value = createdRecs;
  await dialog.alert(`${createdRecs.length} receptions importees et ajoutees au stockage local.`, {
    title: 'Import termine',
    tone: 'success'
  });
}

async function previewMapped(sheetIndex: number) {
  const p = parses.value[sheetIndex];
  const m = mappings[sheetIndex];
  if (!p || !m || typeof m.ofIndex !== 'number') {
    await dialog.alert('Mapping introuvable.', { title: 'Apercu mapping', tone: 'warning' });
    return;
  }
  // Les index sont extraits avant la fermeture : dans la callback, TypeScript perd
  // l'affinement de type fait par la garde ci-dessus sur `m`.
  const ofIndex = m.ofIndex;
  const articleIndex = typeof m.articleIndex === 'number' ? m.articleIndex : null;
  const poidsIndex = typeof m.poidsIndex === 'number' ? m.poidsIndex : null;

  const preview = p.rows.slice(0, 10).map((row: X3Cell[]) => {
    const of = row[ofIndex] ? String(row[ofIndex]).trim() : '';
    const article = articleIndex !== null && row[articleIndex] ? String(row[articleIndex]).trim() : '';
    const poids = poidsIndex !== null && row[poidsIndex] !== undefined ? row[poidsIndex] : '';
    return { of, article, poids };
  });
  await dialog.alert('Apercu des 10 premieres lignes mappees.', {
    title: 'Apercu mapping',
    details: JSON.stringify(preview, null, 2)
  });
}

async function autoDetect(sheetIndex: number) {
  const p = parses.value[sheetIndex];
  if (!p) return;
  const mapping = detectMappingFromHeaders(p.sheetName, p.headers);
  if (!mapping) {
    await dialog.alert('Auto-detection impossible pour cette feuille.', { title: 'Mapping', tone: 'warning' });
    return;
  }
  mappings[sheetIndex].ofIndex = mapping.ofIndex;
  mappings[sheetIndex].articleIndex = mapping.articleIndex !== undefined ? mapping.articleIndex : '';
  mappings[sheetIndex].poidsIndex = mapping.poidsIndex !== undefined ? mapping.poidsIndex : '';
  await dialog.alert('Mapping detecte automatiquement. Verifiez puis importez.', {
    title: 'Mapping',
    tone: 'success'
  });
}
</script>
