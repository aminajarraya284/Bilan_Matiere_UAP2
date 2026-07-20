<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import {
  CheckCircle2,
  ClipboardCheck,
  Gauge,
  History,
  Plus,
  RotateCcw,
  Scale,
  Trash2
} from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import FormField from '@/components/ui/FormField.vue';
import { BUSINESS_RULES } from '@/config/businessRules';
import { formatNumber } from '@/utils/formatters';
import { useAppDialog } from '@/composables/useAppDialog';
import {
  readControlesPoidsFonderie,
  writeControlesPoidsFonderie,
  type ControlePoidsFonderieRecord
} from './controlePoidsFonderie.storage';

const pointKeys = ['P1', 'P2', 'P3', 'P4', 'P5'] as const;
type PointKey = (typeof pointKeys)[number];
type PointValues = {
  poidsBrutKg: number | null;
  poidsNetKg: number | null;
  pieces?: number | null;
};

type ControlePoids = Omit<
  ControlePoidsFonderieRecord,
  'points' | 'totalPiecesControle' | 'totalPoidsNetKg' | 'poidsPhysiqueUnitaireKg'
> & {
  id?: string;
  points: Record<PointKey, PointValues>;
  totalPiecesControle: number;
  totalPoidsNetKg: number;
  poidsPhysiqueUnitaireKg: number;
};

const dialog = useAppDialog();
const successMessage = ref('');
const controles = ref<ControlePoids[]>([]);

const createEmptyPoints = (): Record<PointKey, PointValues> =>
  pointKeys.reduce(
    (acc, point) => ({
      ...acc,
      [point]: {
        poidsBrutKg: null,
        poidsNetKg: null
      }
    }),
    {} as Record<PointKey, PointValues>
  );

const createEmptyForm = (): ControlePoids => ({
  dateControle: BUSINESS_RULES.dateFinParDefaut,
  code: '',
  designation: '',
  numeroOF: '',
  atelier: 'FONDERIE',
  points: createEmptyPoints(),
  totalPiecesControle: 0,
  totalPoidsNetKg: 0,
  poidsPhysiqueUnitaireKg: 0,
  controleurOperateur: '',
  visa: ''
});

const form = ref<ControlePoids>(createEmptyForm());

const validNetWeights = computed(() =>
  pointKeys
    .map((point) => Number(form.value.points[point].poidsNetKg))
    .filter((value) => Number.isFinite(value) && value > 0)
);

const totalPoidsNetKg = computed(() =>
  validNetWeights.value.reduce((total, value) => total + value, 0)
);

const poidsPhysiqueKg = computed(() =>
  validNetWeights.value.length
    ? Number((totalPoidsNetKg.value / validNetWeights.value.length).toFixed(3))
    : 0
);

const ecartBrutNetKg = computed(() =>
  pointKeys.reduce((total, point) => {
    const brut = Number(form.value.points[point].poidsBrutKg) || 0;
    const net = Number(form.value.points[point].poidsNetKg) || 0;
    return total + Math.max(0, brut - net);
  }, 0)
);

const getPoidsBrutGrammes = (point: PointKey): number | '' => {
  const poidsBrutKg = form.value.points[point].poidsBrutKg;
  return poidsBrutKg === null ? '' : Number((poidsBrutKg * 1000).toFixed(3));
};

const setPoidsBrutGrammes = (point: PointKey, event: Event) => {
  const rawValue = (event.target as HTMLInputElement).value;
  if (rawValue === '') {
    form.value.points[point].poidsBrutKg = null;
    return;
  }

  const poidsGrammes = Number(rawValue);
  form.value.points[point].poidsBrutKg =
    Number.isFinite(poidsGrammes) && poidsGrammes >= 0 ? Number((poidsGrammes / 1000).toFixed(6)) : null;
};

const getPoidsNetGrammes = (point: PointKey): number | '' => {
  const poidsNetKg = form.value.points[point].poidsNetKg;
  return poidsNetKg === null ? '' : Number((poidsNetKg * 1000).toFixed(3));
};

const setPoidsNetGrammes = (point: PointKey, event: Event) => {
  const rawValue = (event.target as HTMLInputElement).value;
  if (rawValue === '') {
    form.value.points[point].poidsNetKg = null;
    return;
  }

  const poidsGrammes = Number(rawValue);
  form.value.points[point].poidsNetKg =
    Number.isFinite(poidsGrammes) && poidsGrammes >= 0 ? Number((poidsGrammes / 1000).toFixed(6)) : null;
};

const lastControle = computed(() => controles.value[0] ?? null);

const loadControles = () => {
  controles.value = readControlesPoidsFonderie() as ControlePoids[];
};

const persistControles = () => {
  writeControlesPoidsFonderie(controles.value);
};

const resetForm = () => {
  form.value = createEmptyForm();
};

const saveControle = async () => {
  if (!form.value.dateControle || !form.value.code.trim() || !form.value.numeroOF.trim()) {
    await dialog.alert('Saisissez au minimum la date, le code et l’OF du contrôle poids.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  if (!validNetWeights.value.length) {
    await dialog.alert('Saisissez au moins un poids net pour calculer le poids physique moyen.', {
      title: 'Mesure manquante',
      tone: 'warning'
    });
    return;
  }

  const controle: ControlePoids = {
    ...JSON.parse(JSON.stringify(form.value)),
    id: `${Date.now()}-${Math.random().toString(16).slice(2)}`,
    code: form.value.code.trim().toUpperCase(),
    numeroOF: form.value.numeroOF.trim().toUpperCase(),
    totalPiecesControle: validNetWeights.value.length,
    totalPoidsNetKg: Number(totalPoidsNetKg.value.toFixed(3)),
    poidsPhysiqueUnitaireKg: poidsPhysiqueKg.value
  };

  controles.value = [controle, ...controles.value].slice(0, 20);
  persistControles();
  resetForm();
  successMessage.value = 'Résultat du contrôle poids enregistré.';
  setTimeout(() => (successMessage.value = ''), 4000);
};

const deleteControle = async (controle: ControlePoids, index: number) => {
  const confirmed = await dialog.confirm(
    `Supprimer le contrôle ${controle.numeroOF} du ${controle.dateControle} ?`,
    {
      title: 'Supprimer le contrôle',
      tone: 'danger',
      confirmLabel: 'Supprimer'
    }
  );
  if (!confirmed) return;

  controles.value = controles.value.filter((_, itemIndex) => itemIndex !== index);
  persistControles();
};

onMounted(loadControles);
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Résultat de contrôle poids"
      description="Saisir les mesures de pesée, calculer le poids physique moyen et conserver l’historique des contrôles."
      :breadcrumb="['Opérations', 'Contrôle poids']"
    />

    <Transition name="toast-motion">
      <div
        v-if="successMessage"
        class="motion-alert flex items-center gap-3 rounded-xl border border-emerald-100 bg-emerald-50 p-4 text-xs font-semibold text-emerald-800"
        role="status"
        aria-live="polite"
      >
        <CheckCircle2 class="h-5 w-5 shrink-0 text-emerald-500" />
        <span>{{ successMessage }}</span>
      </div>
    </Transition>

    <section class="grid grid-cols-1 gap-4 md:grid-cols-3">
      <article class="surface-card p-5">
        <div class="flex items-start justify-between gap-3">
          <div>
            <p class="text-xs font-bold uppercase tracking-wider text-slate-400">Poids physique moyen</p>
            <p class="mt-2 text-2xl font-black font-mono text-blue-900">
              {{ formatNumber(poidsPhysiqueKg, 3) }}
              <span class="text-xs text-slate-400">kg</span>
            </p>
          </div>
          <span class="rounded-xl bg-blue-50 p-2.5 text-blue-800"><Gauge class="h-5 w-5" /></span>
        </div>
        <p class="mt-3 text-xs font-semibold text-slate-500">
          Moyenne des {{ validNetWeights.length }} mesure{{ validNetWeights.length > 1 ? 's' : '' }} nette{{
            validNetWeights.length > 1 ? 's' : ''
          }} renseignée{{ validNetWeights.length > 1 ? 's' : '' }}.
        </p>
      </article>

      <article class="surface-card p-5">
        <div class="flex items-start justify-between gap-3">
          <div>
            <p class="text-xs font-bold uppercase tracking-wider text-slate-400">Poids net cumulé</p>
            <p class="mt-2 text-2xl font-black font-mono text-slate-900">
              {{ formatNumber(totalPoidsNetKg, 3) }}
              <span class="text-xs text-slate-400">kg</span>
            </p>
          </div>
          <span class="rounded-xl bg-emerald-50 p-2.5 text-emerald-700"><Scale class="h-5 w-5" /></span>
        </div>
        <p class="mt-3 text-xs font-semibold text-slate-500">Somme automatique des poids nets P1 à P5.</p>
      </article>

      <article class="surface-card p-5">
        <div class="flex items-start justify-between gap-3">
          <div>
            <p class="text-xs font-bold uppercase tracking-wider text-slate-400">Dernier contrôle</p>
            <p class="mt-2 text-lg font-black font-mono text-slate-900">
              {{ lastControle?.numeroOF || 'Aucun' }}
            </p>
          </div>
          <span class="rounded-xl bg-amber-50 p-2.5 text-amber-700"><ClipboardCheck class="h-5 w-5" /></span>
        </div>
        <p class="mt-3 text-xs font-semibold text-slate-500">
          {{ lastControle ? `${lastControle.dateControle} · ${formatNumber(lastControle.poidsPhysiqueUnitaireKg, 3)} kg` : 'Aucun contrôle enregistré.' }}
        </p>
      </article>
    </section>

    <form class="space-y-5" @submit.prevent="saveControle">
      <section class="surface-card overflow-hidden">
        <header class="flex items-center gap-3 border-b border-slate-100 px-5 py-4">
          <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-50 text-sm font-black text-blue-900">1</span>
          <div>
            <h2 class="text-sm font-black text-slate-800">Identification du contrôle</h2>
            <p class="mt-0.5 text-xs font-semibold text-slate-500">Renseignez la référence et l’origine de la pesée.</p>
          </div>
        </header>

        <div class="grid grid-cols-1 gap-4 p-5 md:grid-cols-2 xl:grid-cols-5">
          <FormField id="controle-poids-date" label="Date" required>
            <input v-model="form.dateControle" type="date" class="w-full rounded-lg border border-slate-200" required />
          </FormField>

          <FormField id="controle-poids-code" label="Code" required>
            <input
              v-model.trim="form.code"
              type="text"
              placeholder="Code article"
              class="w-full rounded-lg border border-slate-200 font-mono font-bold"
              required
            />
          </FormField>

          <FormField id="controle-poids-designation" label="Désignation">
            <input
              v-model.trim="form.designation"
              type="text"
              placeholder="Désignation"
              class="w-full rounded-lg border border-slate-200"
            />
          </FormField>

          <FormField id="controle-poids-of" label="OF" required>
            <input
              v-model.trim="form.numeroOF"
              type="text"
              placeholder="OF18100563"
              class="w-full rounded-lg border border-slate-200 font-mono font-bold"
              required
            />
          </FormField>

          <FormField id="controle-poids-atelier" label="Atelier">
            <input v-model.trim="form.atelier" type="text" class="w-full rounded-lg border border-slate-200" />
          </FormField>
        </div>
      </section>

      <section class="surface-card overflow-hidden">
        <header class="flex flex-col gap-3 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div class="flex items-center gap-3">
            <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-50 text-sm font-black text-blue-900">2</span>
            <div>
              <h2 class="text-sm font-black text-slate-800">Mesures de pesée</h2>
              <p class="mt-0.5 text-xs font-semibold text-slate-500">
                Saisissez les poids brut et net en grammes. La conversion en kilogrammes est automatique.
              </p>
            </div>
          </div>
          <span class="rounded-full bg-blue-50 px-3 py-1 text-[10px] font-black uppercase tracking-wider text-blue-800">
            Calcul automatique
          </span>
        </header>

        <div class="table-wrapper">
          <table class="w-full min-w-[760px] border-collapse text-xs">
            <thead>
              <tr class="bg-slate-50 text-[10px] font-black uppercase tracking-wider text-slate-500">
                <th class="border-r border-slate-100 p-3 text-left">pieces</th>
                <th v-for="point in pointKeys" :key="point" class="border-r border-slate-100 p-3 text-center">
                  {{ point }}
                </th>
                <th class="p-3 text-right text-blue-900">Résultat</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr>
                <th class="border-r border-slate-100 p-3 text-left font-black uppercase text-slate-600">
                  Poids brut (g)
                </th>
                <td v-for="point in pointKeys" :key="`brut-${point}`" class="border-r border-slate-100 p-3">
                  <input
                    :value="getPoidsBrutGrammes(point)"
                    type="number"
                    min="0"
                    step="1"
                    :aria-label="`Poids brut ${point} en grammes`"
                    placeholder="0"
                    class="w-full rounded-lg border border-slate-200 text-right font-mono font-bold"
                    @input="setPoidsBrutGrammes(point, $event)"
                  />
                </td>
                <td class="p-3 text-right font-bold text-slate-400">—</td>
              </tr>
              <tr>
                <th class="border-r border-slate-100 p-3 text-left font-black uppercase text-slate-600">
                  Poids net (g)
                </th>
                <td v-for="point in pointKeys" :key="`net-${point}`" class="border-r border-slate-100 p-3">
                  <input
                    :value="getPoidsNetGrammes(point)"
                    type="number"
                    min="0"
                    step="1"
                    :aria-label="`Poids net ${point} en grammes`"
                    placeholder="0"
                    class="w-full rounded-lg border border-slate-200 text-right font-mono font-bold"
                    @input="setPoidsNetGrammes(point, $event)"
                  />
                </td>
                <td class="bg-blue-50/70 p-3 text-right">
                  <span class="block text-[10px] font-black uppercase tracking-wider text-blue-700">Poids PHY moyen</span>
                  <span class="mt-1 block text-base font-black font-mono text-blue-950">
                    {{ formatNumber(poidsPhysiqueKg, 3) }} kg
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="grid gap-3 border-t border-slate-100 bg-slate-50/60 p-4 sm:grid-cols-3">
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">Mesures renseignées</span>
            <p class="mt-1 font-black font-mono text-slate-800">{{ validNetWeights.length }} / 5</p>
          </div>
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">
              Poids net converti en kg
            </span>
            <p class="mt-1 font-black font-mono text-slate-800">{{ formatNumber(totalPoidsNetKg, 3) }} kg</p>
          </div>
          <div>
            <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">Écart brut / net</span>
            <p class="mt-1 font-black font-mono text-slate-800">{{ formatNumber(ecartBrutNetKg, 3) }} kg</p>
          </div>
        </div>

        <footer class="flex flex-col-reverse gap-3 border-t border-slate-100 bg-slate-50/60 px-5 py-4 sm:flex-row sm:justify-end">
          <button
            type="button"
            class="inline-flex items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-xs font-bold text-slate-700 hover:bg-slate-50"
            @click="resetForm"
          >
            <RotateCcw class="h-4 w-4" />
            Réinitialiser
          </button>
          <button
            type="submit"
            class="inline-flex items-center justify-center gap-2 rounded-lg bg-blue-900 px-5 py-2.5 text-xs font-bold text-white hover:bg-blue-800"
          >
            <Plus class="h-4 w-4" />
            Enregistrer le contrôle
          </button>
        </footer>
      </section>
    </form>

    <section class="surface-card overflow-hidden">
      <header class="flex flex-col gap-2 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="flex items-center gap-2">
          <History class="h-4 w-4 text-blue-900" />
          <h2 class="text-sm font-black text-slate-800">Historique récent</h2>
        </div>
        <span class="text-[10px] font-black uppercase tracking-wider text-slate-400">
          {{ controles.length }} contrôle{{ controles.length > 1 ? 's' : '' }}
        </span>
      </header>

      <div v-if="controles.length" class="table-wrapper">
        <table class="w-full text-left">
          <thead>
            <tr class="bg-slate-50 text-[10px] font-black uppercase tracking-wider text-slate-500">
              <th class="p-3">Date</th>
              <th class="p-3">OF</th>
              <th class="p-3">Code / Désignation</th>
              <th class="p-3">Atelier</th>
              <th class="p-3 text-right">Mesures</th>
              <th class="p-3 text-right">Poids PHY</th>
              <th class="p-3 text-right">Action</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 text-xs">
            <tr v-for="(controle, index) in controles" :key="controle.id || `${controle.numeroOF}-${index}`">
              <td class="p-3 font-mono font-semibold text-slate-600">{{ controle.dateControle }}</td>
              <td class="p-3 font-mono font-black text-blue-900">{{ controle.numeroOF }}</td>
              <td class="p-3">
                <p class="font-bold text-slate-800">{{ controle.code }}</p>
                <p class="mt-0.5 text-[10px] font-semibold text-slate-400">{{ controle.designation || '—' }}</p>
              </td>
              <td class="p-3 font-semibold text-slate-600">{{ controle.atelier || '—' }}</td>
              <td class="p-3 text-right font-mono font-bold text-slate-700">{{ controle.totalPiecesControle || '—' }}</td>
              <td class="p-3 text-right font-mono font-black text-blue-900">
                {{ formatNumber(controle.poidsPhysiqueUnitaireKg, 3) }} kg
              </td>
              <td class="p-3 text-right">
                <button
                  type="button"
                  class="rounded-lg p-2 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                  title="Supprimer"
                  @click="deleteControle(controle, index)"
                >
                  <Trash2 class="h-4 w-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-else class="flex flex-col items-center justify-center px-6 py-12 text-center">
        <span class="rounded-full bg-slate-100 p-3 text-slate-400"><History class="h-6 w-6" /></span>
        <p class="mt-3 text-sm font-bold text-slate-700">Aucun contrôle enregistré</p>
        <p class="mt-1 text-xs font-semibold text-slate-400">Les contrôles enregistrés apparaîtront ici.</p>
      </div>
    </section>
  </div>
</template>
