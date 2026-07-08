<template>
  <div class="p-4">
    <h2 class="text-lg font-semibold mb-2">Importeur X3 - Aperçu OF</h2>
    <div class="flex flex-wrap items-center gap-3">
      <input type="file" accept=".xlsx,.xls" @change="onFile" />
      <button class="btn btn-primary" :disabled="isLoadingReference" @click="loadIntegratedReference">
        {{ isLoadingReference ? 'Chargement...' : 'Charger la référence ListeOFX3' }}
      </button>
      <a class="btn" :href="referenceWorkbookUrl" download>Télécharger Excel référence</a>
    </div>

    <div v-if="referenceSummary" class="mt-3 text-xs text-slate-600 bg-slate-50 border border-slate-200 rounded p-3">
      Référence intégrée :
      {{ referenceSummary.sorties }} lignes OF/composant,
      {{ referenceSummary.ofs }} OF,
      {{ referenceSummary.articles }} articles,
      {{ referenceSummary.materials }} matières.
    </div>

    <div v-if="parses.length" class="mt-4">
      <div v-for="(p, i) in parses" :key="i" class="mb-4 border-b pb-3">
        <h3 class="font-medium">Feuille: {{ p.sheetName }} ({{ p.ofs.length }} OFs détectés)</h3>
        <div class="text-sm text-gray-600">Headers: {{ p.headers.join(', ') }}</div>
        <div class="mt-2">
          <button class="btn btn-primary mr-2" @click="importOfs(p.ofs)">
            Importer ces OFs comme réceptions de test
          </button>
          <button class="btn mr-2" @click="downloadOfList(p.ofs)">Télécharger la liste OF (.txt)</button>
        </div>

        <div class="mt-3">
          <h4 class="font-medium">Importer en mappant les colonnes</h4>
          <div class="flex items-center gap-2 mt-2">
            <button class="btn mr-2" @click="autoDetect(i)">Auto-detect</button>
            <label class="text-sm">Colonne OF:</label>
            <select v-model="mappings[i].ofIndex">
              <option v-for="(h, idx) in p.headers" :key="idx" :value="idx">{{ idx }} - {{ h || '(vide)' }}</option>
            </select>
            <label class="text-sm">Article:</label>
            <select v-model="mappings[i].articleIndex">
              <option value="">(aucun)</option>
              <option v-for="(h, idx) in p.headers" :key="'a' + idx" :value="idx">
                {{ idx }} - {{ h || '(vide)' }}
              </option>
            </select>
            <label class="text-sm">Poids:</label>
            <select v-model="mappings[i].poidsIndex">
              <option value="">(aucun)</option>
              <option v-for="(h, idx) in p.headers" :key="'w' + idx" :value="idx">
                {{ idx }} - {{ h || '(vide)' }}
              </option>
            </select>
          </div>
          <div class="mt-2">
            <button class="btn btn-primary mr-2" @click="importMapped(i)">Importer avec mapping</button>
            <button class="btn" @click="previewMapped(i)">Aperçu mapping (10 lignes)</button>
            <label class="ml-4 text-sm"
              ><input type="checkbox" v-model="mappings[i].sync" /> Marquer comme synchronisation (Synchronisé
              ERP)</label
            >
            <label class="ml-2 text-sm"
              ><input type="checkbox" v-model="mappings[i].backup" /> Sauvegarder backup avant écriture</label
            >
          </div>
        </div>

        <div class="mt-2 text-xs">
          Aperçu des 10 premières lignes:
          <pre class="bg-gray-100 p-2 mt-1 overflow-auto">{{ p.rows.slice(0, 10) }}</pre>
        </div>
      </div>
    </div>

    <div v-if="created && created.length" class="mt-4">
      <h3 class="font-medium">Réceptions créées (test)</h3>
      <ul class="list-disc pl-6 text-sm">
        <li v-for="r in created" :key="r.id">{{ r.numOf }} — id {{ r.id }}</li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { ReceptionUsinage2Mouvement } from '@/features/receptions-usinage2/receptions.types';
import { useAppDialog } from '@/composables/useAppDialog';
import {
  X3Cell,
  X3ParseResult,
  parseX3File,
  insertOfsAsReceptions,
  mapRowsToReceptions,
  detectMappingFromHeaders
} from './importer.service';
import { x3ReferenceService } from '@/features/reference/x3Reference.service';

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
const referenceWorkbookUrl = x3ReferenceService.getWorkbookUrl();
const referenceSummary = ref<{ sorties: number; ofs: number; articles: number; materials: number } | null>(null);
// mappings per sheet index
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
    await dialog.alert(error instanceof Error ? error.message : 'Import X3 impossible.', {
      title: 'Import X3',
      tone: 'warning'
    });
  }
}

async function loadIntegratedReference() {
  isLoadingReference.value = true;
  try {
    const [reference, results] = await Promise.all([
      x3ReferenceService.getReference(),
      x3ReferenceService.getReferenceAsParseResult()
    ]);
    parses.value = results;
    created.value = [];
    referenceSummary.value = {
      sorties: reference.sheets.LISTEOFX3.aggregatedRows,
      ofs: reference.sheets.LISTEOFX3.uniqueOf,
      articles: reference.sheets.GAMME.uniqueArticles,
      materials: reference.sheets.BOM.uniqueMaterials
    };
    resetMappings(results);
  } catch (error) {
    await dialog.alert(error instanceof Error ? error.message : 'Chargement de la référence impossible.', {
      title: 'Référence X3',
      tone: 'warning'
    });
  } finally {
    isLoadingReference.value = false;
  }
}

async function importOfs(ofs: string[]) {
  if (!ofs || !ofs.length) return;
  const createdRecs = insertOfsAsReceptions(ofs, { user: 'IMPORT_UI' });
  created.value = createdRecs;
  await dialog.alert(`${createdRecs.length} réceptions de test ajoutées.`, {
    title: 'Import terminé',
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
    await dialog.alert('Mapping introuvable.', { title: 'Import X3', tone: 'warning' });
    return;
  }
  if (typeof m.ofIndex !== 'number' || m.ofIndex < 0) {
    await dialog.alert('Sélectionner la colonne OF.', { title: 'Import X3', tone: 'warning' });
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
  await dialog.alert(`${createdRecs.length} réceptions importées et ajoutées au stockage local.`, {
    title: 'Import terminé',
    tone: 'success'
  });
}

async function previewMapped(sheetIndex: number) {
  const p = parses.value[sheetIndex];
  const m = mappings[sheetIndex];
  if (!p || !m || typeof m.ofIndex !== 'number') {
    await dialog.alert('Mapping introuvable.', { title: 'Aperçu mapping', tone: 'warning' });
    return;
  }
  const preview = p.rows.slice(0, 10).map((row: X3Cell[]) => {
    const of = row[m.ofIndex] ? String(row[m.ofIndex]).trim() : '';
    const article = typeof m.articleIndex === 'number' && row[m.articleIndex] ? String(row[m.articleIndex]).trim() : '';
    const poids = typeof m.poidsIndex === 'number' && row[m.poidsIndex] !== undefined ? row[m.poidsIndex] : '';
    return { of, article, poids };
  });
  await dialog.alert('Aperçu des 10 premières lignes mappées.', {
    title: 'Aperçu mapping',
    details: JSON.stringify(preview, null, 2)
  });
}

async function autoDetect(sheetIndex: number) {
  const p = parses.value[sheetIndex];
  if (!p) return;
  const mapping = detectMappingFromHeaders(p.sheetName, p.headers);
  if (!mapping) {
    await dialog.alert('Auto-détection impossible pour cette feuille.', { title: 'Mapping X3', tone: 'warning' });
    return;
  }
  mappings[sheetIndex].ofIndex = mapping.ofIndex;
  mappings[sheetIndex].articleIndex = mapping.articleIndex !== undefined ? mapping.articleIndex : '';
  mappings[sheetIndex].poidsIndex = mapping.poidsIndex !== undefined ? mapping.poidsIndex : '';
  await dialog.alert('Mapping détecté automatiquement. Vérifiez puis importez.', {
    title: 'Mapping X3',
    tone: 'success'
  });
}
</script>

<style scoped>
.btn {
  padding: 0.4rem 0.75rem;
  border-radius: 0.375rem;
  background: #eee;
  border: 1px solid #ddd;
}
.btn-primary {
  background: #0069d9;
  color: white;
  border-color: #0056b3;
}
</style>
