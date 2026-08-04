<script setup lang="ts">
import { computed, ref, onMounted } from 'vue';
import { useAuthStore } from '@/features/auth/auth.store';
import { Plus, Ban, Layers, Calculator, Flame } from 'lucide-vue-next';
import { inventaireService } from './inventaire.service';
import {
  InventaireConteneur,
  InventaireCategorie,
  InventaireFour,
  InventaireTotalParMatiere,
  InventaireTypeMatiere
} from './inventaire.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import FormField from '@/components/ui/FormField.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import DataSourceBadge from '@/components/ui/DataSourceBadge.vue';
import { formatNumber } from '@/utils/formatters';
import { useAppDialog } from '@/composables/useAppDialog';
import { BUSINESS_RULES } from '@/config/businessRules';

const authStore = useAuthStore();
const dialog = useAppDialog();

const conteneurs = ref<InventaireConteneur[]>([]);
const categories = ref<InventaireCategorie[]>([]);
const fours = ref<InventaireFour[]>([]);
const totaux = ref<InventaireTotalParMatiere[]>([]);
const typesMatiere = ref<InventaireTypeMatiere[]>([]);
const isLoading = ref(true);
const loadError = ref('');
const canUseComments = computed(() => authStore.userRole === 'Admin');

// L'API d'inventaire est portee par periode (AAAA-MM) : toute la page est cadree
// sur cette valeur, y compris les creations.
const periode = ref(BUSINESS_RULES.dateFinParDefaut.slice(0, 7));

const formP1 = ref({
  dateInventaire: BUSINESS_RULES.dateFinParDefaut,
  numeroConteneur: '',
  numeroOF: '',
  typeMatiereId: '',
  quantiteKg: 0,
  observation: ''
});

const formP2 = ref({
  categorie: 'REBUT',
  typeMatiereId: '',
  quantiteKg: 0,
  observation: ''
});

const formP3 = ref({
  dateInventaire: BUSINESS_RULES.dateFinParDefaut,
  four: 'Four CBP 01',
  typeMatiereId: '',
  quantiteKg: 0,
  observation: ''
});

const categoriesList = ['REBUT', 'MP LINGOT'];
const foursList = ['Four CBP 01', 'Four CBP 02', 'Four CDQ', 'Autre'];

/** Aligne les listes deroulantes sur les types matiere actifs renvoyes par l'API. */
const syncTypeMatiereSelections = () => {
  const defaultId = typesMatiere.value[0]?.id ?? '';
  const known = new Set(typesMatiere.value.map((type) => type.id));
  for (const form of [formP1, formP2, formP3]) {
    if (!known.has(form.value.typeMatiereId)) form.value.typeMatiereId = defaultId;
  }
};

const loadAllData = async () => {
  isLoading.value = true;
  loadError.value = '';
  try {
    const data = await inventaireService.getPeriode(periode.value);
    conteneurs.value = data.conteneurs;
    categories.value = data.categories;
    fours.value = data.fours;
    totaux.value = data.totaux;
    typesMatiere.value = data.typesMatiere;
    syncTypeMatiereSelections();
  } catch (error) {
    loadError.value = error instanceof Error ? error.message : "Chargement de l'inventaire impossible.";
    conteneurs.value = [];
    categories.value = [];
    fours.value = [];
    totaux.value = [];
  } finally {
    isLoading.value = false;
  }
};

/** La date saisie doit rester dans la periode affichee : le backend la refuserait sinon. */
const isDansPeriode = (date: string): boolean => date.startsWith(periode.value);

const submitOrAlert = async (action: () => Promise<unknown>): Promise<void> => {
  try {
    await action();
    await loadAllData();
  } catch (error) {
    await dialog.alert(error instanceof Error ? error.message : 'Enregistrement impossible.', {
      title: 'Enregistrement refusé',
      tone: 'danger'
    });
  }
};

const totalGeneralRapport = computed(() => totaux.value.reduce((sum, item) => sum + item.totalInventaireRapport, 0));
const totalGeneralBilan = computed(() => totaux.value.reduce((sum, item) => sum + item.totalUtiliseBilan, 0));
const totalGeneralFour = computed(() => totaux.value.reduce((sum, item) => sum + item.totalPartie3, 0));
const totalGeneralStockDebut = computed(() =>
  totaux.value.reduce((sum, item) => sum + (item.stockDebutKg ?? 0), 0)
);
const totalGeneralStockFin = computed(() => totaux.value.reduce((sum, item) => sum + item.stockFinKg, 0));

const handleAddP1 = async () => {
  if (!formP1.value.dateInventaire || !formP1.value.numeroConteneur.trim() || !formP1.value.typeMatiereId) {
    await dialog.alert('Veuillez remplir correctement tous les champs obligatoires.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  if (!isDansPeriode(formP1.value.dateInventaire)) {
    await dialog.alert(`La date doit appartenir à la période ${periode.value}.`, {
      title: 'Date hors période',
      tone: 'warning'
    });
    return;
  }

  await submitOrAlert(async () => {
    await inventaireService.createConteneur({
      dateInventaire: formP1.value.dateInventaire,
      numeroConteneur: formP1.value.numeroConteneur.trim(),
      numeroOF: formP1.value.numeroOF.trim() || undefined,
      typeMatiereId: formP1.value.typeMatiereId,
      quantiteKg: Number(formP1.value.quantiteKg),
      observation: canUseComments.value ? formP1.value.observation || undefined : undefined
    });

    formP1.value.numeroConteneur = '';
    formP1.value.numeroOF = '';
    formP1.value.quantiteKg = 0;
    formP1.value.observation = '';
  });
};

const handleAddP2 = async () => {
  if (!formP2.value.categorie || !formP2.value.typeMatiereId) {
    await dialog.alert('Veuillez remplir correctement tous les champs obligatoires.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  await submitOrAlert(async () => {
    await inventaireService.createCategorie({
      periode: periode.value,
      categorie: formP2.value.categorie,
      typeMatiereId: formP2.value.typeMatiereId,
      quantiteKg: Number(formP2.value.quantiteKg),
      observation: canUseComments.value ? formP2.value.observation || undefined : undefined
    });

    formP2.value.quantiteKg = 0;
    formP2.value.observation = '';
  });
};

const handleAddP3 = async () => {
  if (!formP3.value.dateInventaire || !formP3.value.four || !formP3.value.typeMatiereId) {
    await dialog.alert('Veuillez remplir correctement tous les champs obligatoires.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  if (!isDansPeriode(formP3.value.dateInventaire)) {
    await dialog.alert(`La date doit appartenir à la période ${periode.value}.`, {
      title: 'Date hors période',
      tone: 'warning'
    });
    return;
  }

  await submitOrAlert(async () => {
    await inventaireService.createFour({
      dateInventaire: formP3.value.dateInventaire,
      four: formP3.value.four,
      typeMatiereId: formP3.value.typeMatiereId,
      quantiteKg: Number(formP3.value.quantiteKg),
      observation: canUseComments.value ? formP3.value.observation || undefined : undefined
    });

    formP3.value.quantiteKg = 0;
    formP3.value.observation = '';
  });
};

const handleValiderP1 = (id: string) => submitOrAlert(() => inventaireService.validerConteneur(id));
const handleValiderP2 = (id: string) => submitOrAlert(() => inventaireService.validerCategorie(id));
const handleValiderP3 = (id: string) => submitOrAlert(() => inventaireService.validerFour(id));

/**
 * L'API ne supprime pas : elle annule (RM-INV-006). Une ligne annulee sort du
 * stock mais reste consultable, ce qui preserve la tracabilite de la saisie.
 */
const confirmerAnnulation = async (message: string): Promise<boolean> =>
  dialog.confirm(message, {
    title: 'Annuler la ligne',
    tone: 'danger',
    confirmLabel: 'Annuler la ligne'
  });

const handleAnnulerP1 = async (id: string) => {
  if (await confirmerAnnulation('Cette ligne sortira du stock mais restera consultable. Confirmer ?')) {
    await submitOrAlert(() => inventaireService.annulerConteneur(id));
  }
};

const handleAnnulerP2 = async (id: string) => {
  if (await confirmerAnnulation('Cette ligne sortira du stock mais restera consultable. Confirmer ?')) {
    await submitOrAlert(() => inventaireService.annulerCategorie(id));
  }
};

const handleAnnulerP3 = async (id: string) => {
  if (await confirmerAnnulation('Cette ligne sortira du stock mais restera consultable. Confirmer ?')) {
    await submitOrAlert(() => inventaireService.annulerFour(id));
  }
};

/** Le changement de periode recadre la page et les dates de saisie par defaut. */
const handleChangePeriode = async () => {
  const premierJour = `${periode.value}-01`;
  formP1.value.dateInventaire = premierJour;
  formP3.value.dateInventaire = premierJour;
  await loadAllData();
};

onMounted(() => {
  loadAllData();
});
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Inventaire Fonderie"
      description="Saisir et contrôler les stocks physiques de fin de mois de la fonderie."
    />
    <DataSourceBadge vertical="inventaire" />

    <div
      class="bg-white rounded-xl border border-slate-100 shadow-sm p-4 flex flex-wrap items-end gap-4"
    >
      <FormField id="periode" label="Période d'inventaire" required>
        <input
          v-model="periode"
          @change="handleChangePeriode"
          type="month"
          class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold"
          required
        />
      </FormField>
      <p class="text-xs text-slate-500 pb-2">
        Les saisies et les totaux affichés portent sur cette période.
      </p>
    </div>

    <div v-if="isLoading">
      <LoadingState />
    </div>

    <div
      v-else-if="loadError"
      class="bg-rose-50 border border-rose-100 rounded-xl p-4 text-xs font-semibold text-rose-700"
    >
      {{ loadError }}
    </div>

    <div v-else class="space-y-8">
      <div class="bg-white rounded-xl border border-slate-100 shadow-sm p-6 space-y-6">
        <div class="flex items-center gap-2 border-b border-slate-50 pb-3">
          <Layers class="w-4.5 h-4.5 text-blue-900" />
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">
            Partie 1 - Inventaire par conteneur / OF
          </h3>
        </div>

        <form
          @submit.prevent="handleAddP1"
          class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 p-4 bg-slate-50/50 border border-slate-100 rounded-xl"
        >
          <FormField id="p1-date" label="Date inventaire" required>
            <input
              v-model="formP1.dateInventaire"
              type="date"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
              required
            />
          </FormField>
          <FormField id="p1-cont" label="N° conteneur" required>
            <input
              v-model="formP1.numeroConteneur"
              type="text"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
              placeholder="CONT-000"
              required
            />
          </FormField>
          <FormField id="p1-of" label="OF">
            <input
              v-model="formP1.numeroOF"
              type="text"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
              placeholder="OF18100563"
            />
          </FormField>
          <FormField id="p1-mat" label="Type matière" required>
            <select
              v-model="formP1.typeMatiereId"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option v-for="mat in typesMatiere" :key="mat.id" :value="mat.id">{{ mat.code }}</option>
            </select>
          </FormField>
          <FormField id="p1-qte" label="Poids réel (kg)" required>
            <input
              v-model="formP1.quantiteKg"
              type="number"
              min="0"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full font-mono"
              required
            />
          </FormField>
          <FormField v-if="canUseComments" id="p1-obs" label="Observation">
            <input
              v-model="formP1.observation"
              type="text"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
              placeholder="Rien à signaler"
            />
          </FormField>
          <div class="flex items-end">
            <button
              type="submit"
              class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-lg flex items-center justify-center gap-1.5 cursor-pointer"
            >
              <Plus class="w-4 h-4" />
              Ajouter ligne
            </button>
          </div>
        </form>

        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr
                class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
              >
                <th class="p-3">Date</th>
                <th class="p-3">N° conteneur</th>
                <th class="p-3">OF</th>
                <th class="p-3">Type matière</th>
                <th class="p-3 text-right">Quantité (kg)</th>
                <th v-if="canUseComments" class="p-3">Observation</th>
                <th class="p-3">Statut</th>
                <th class="p-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="item in conteneurs" :key="item.id">
                <td class="p-3 font-mono text-[11px] text-slate-600">{{ item.dateInventaire }}</td>
                <td class="p-3 font-bold text-slate-800 font-mono">{{ item.numeroConteneur }}</td>
                <td class="p-3 font-mono text-slate-600">{{ item.numeroOF || '-' }}</td>
                <td class="p-3 font-mono font-bold text-blue-900">{{ item.codeTypeMatiere }}</td>
                <td class="p-3 text-right font-black font-mono text-slate-800">
                  {{ formatNumber(item.quantiteKg, 0) }}
                </td>
                <td v-if="canUseComments" class="p-3 text-slate-400 text-[11px] font-semibold">
                  {{ item.observation || '-' }}
                </td>
                <td class="p-3"><BadgeStatus :status="item.statut" /></td>
                <td class="p-3 text-right">
                  <div class="inline-flex gap-2">
                    <button
                      v-if="item.statut === 'Brouillon'"
                      @click="handleValiderP1(item.id)"
                      class="px-2 py-1 bg-emerald-50 border border-emerald-100 rounded text-emerald-700 text-[10px] font-bold cursor-pointer"
                    >
                      Valider
                    </button>
                    <button
                      v-if="item.statut !== 'Annule'"
                      @click="handleAnnulerP1(item.id)"
                      title="Annuler la ligne"
                      class="p-1 hover:bg-rose-50 rounded text-slate-400 hover:text-rose-600 cursor-pointer"
                    >
                      <Ban class="w-3.5 h-3.5" />
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="conteneurs.length === 0">
                <td :colspan="canUseComments ? 8 : 7" class="p-4 text-center text-slate-400 font-semibold">
                  Aucune ligne pour cette période.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="bg-white rounded-xl border border-slate-100 shadow-sm p-6 space-y-6">
        <div class="flex items-center gap-2 border-b border-slate-50 pb-3">
          <Layers class="w-4.5 h-4.5 text-blue-900" />
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">
            Partie 2 - Inventaire par catégorie inclus dans bilan fonderie
          </h3>
        </div>

        <form
          @submit.prevent="handleAddP2"
          class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-5 gap-4 p-4 bg-slate-50/50 border border-slate-100 rounded-xl"
        >
          <FormField id="p2-cat" label="Catégorie" required>
            <select
              v-model="formP2.categorie"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option v-for="cat in categoriesList" :key="cat" :value="cat">{{ cat }}</option>
            </select>
          </FormField>
          <FormField id="p2-mat" label="Type matière" required>
            <select
              v-model="formP2.typeMatiereId"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option v-for="mat in typesMatiere" :key="mat.id" :value="mat.id">{{ mat.code }}</option>
            </select>
          </FormField>
          <FormField id="p2-qte" label="Poids réel (kg)" required>
            <input
              v-model="formP2.quantiteKg"
              type="number"
              min="0"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full font-mono"
              required
            />
          </FormField>
          <FormField v-if="canUseComments" id="p2-comment" label="Observation">
            <input
              v-model="formP2.observation"
              type="text"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
              placeholder="Note d'observation"
            />
          </FormField>
          <div class="flex items-end">
            <button
              type="submit"
              class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-lg flex items-center justify-center gap-1.5 cursor-pointer"
            >
              <Plus class="w-4 h-4" />
              Ajouter ligne
            </button>
          </div>
        </form>

        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr
                class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
              >
                <th class="p-3">Catégorie</th>
                <th class="p-3">Type matière</th>
                <th class="p-3 text-right">Quantité réelle (kg)</th>
                <th v-if="canUseComments" class="p-3">Observation</th>
                <th class="p-3">Statut</th>
                <th class="p-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="item in categories" :key="item.id">
                <td class="p-3 font-bold text-slate-800">{{ item.categorie }}</td>
                <td class="p-3 font-mono font-bold text-blue-900">{{ item.codeTypeMatiere }}</td>
                <td class="p-3 text-right font-bold font-mono">{{ formatNumber(item.quantiteKg, 0) }}</td>
                <td v-if="canUseComments" class="p-3 text-slate-500 text-[11px] font-semibold">
                  {{ item.observation || '-' }}
                </td>
                <td class="p-3"><BadgeStatus :status="item.statut" /></td>
                <td class="p-3 text-right">
                  <div class="inline-flex gap-2">
                    <button
                      v-if="item.statut === 'Brouillon'"
                      @click="handleValiderP2(item.id)"
                      class="px-2 py-1 bg-emerald-50 border border-emerald-100 rounded text-emerald-700 text-[10px] font-bold cursor-pointer"
                    >
                      Valider
                    </button>
                    <button
                      v-if="item.statut !== 'Annule'"
                      @click="handleAnnulerP2(item.id)"
                      title="Annuler la ligne"
                      class="p-1 hover:bg-rose-50 rounded text-slate-400 hover:text-rose-600 cursor-pointer"
                    >
                      <Ban class="w-3.5 h-3.5" />
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="categories.length === 0">
                <td :colspan="canUseComments ? 6 : 5" class="p-4 text-center text-slate-400 font-semibold">
                  Aucune ligne pour cette période.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="bg-white rounded-xl border border-slate-100 shadow-sm p-6 space-y-6">
        <div class="flex items-center gap-2 border-b border-slate-50 pb-3">
          <Flame class="w-4.5 h-4.5 text-[var(--color-attention)]" />
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">Partie 3 - Catégorie Four</h3>
        </div>

        <form
          @submit.prevent="handleAddP3"
          class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-5 gap-4 p-4 bg-slate-50/50 border border-slate-100 rounded-xl"
        >
          <FormField id="p3-date" label="Date inventaire" required>
            <input
              v-model="formP3.dateInventaire"
              type="date"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
              required
            />
          </FormField>
          <FormField id="p3-four" label="Four" required>
            <select
              v-model="formP3.four"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option v-for="item in foursList" :key="item" :value="item">{{ item }}</option>
            </select>
          </FormField>
          <FormField id="p3-mat" label="Type matière" required>
            <select
              v-model="formP3.typeMatiereId"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option v-for="mat in typesMatiere" :key="mat.id" :value="mat.id">{{ mat.code }}</option>
            </select>
          </FormField>
          <FormField id="p3-qte" label="Quantité (kg)" required>
            <input
              v-model="formP3.quantiteKg"
              type="number"
              min="0"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full font-mono"
              required
            />
          </FormField>
          <FormField v-if="canUseComments" id="p3-observation" label="Observation">
            <input
              v-model="formP3.observation"
              type="text"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
              placeholder="Observation four"
            />
          </FormField>
          <div class="flex items-end">
            <button
              type="submit"
              class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-lg flex items-center justify-center gap-1.5 cursor-pointer"
            >
              <Plus class="w-4 h-4" />
              Ajouter ligne four
            </button>
          </div>
        </form>

        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr
                class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
              >
                <th class="p-3">Date inventaire</th>
                <th class="p-3">Four</th>
                <th class="p-3">Type matière</th>
                <th class="p-3 text-right">Quantité (kg)</th>
                <th class="p-3">Observation</th>
                <th class="p-3">Statut</th>
                <th class="p-3">Badges</th>
                <th class="p-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="item in fours" :key="item.id">
                <td class="p-3 font-mono text-[11px] text-slate-600">{{ item.dateInventaire }}</td>
                <td class="p-3 font-bold text-slate-800">{{ item.four }}</td>
                <td class="p-3 font-mono font-bold text-blue-900">{{ item.codeTypeMatiere }}</td>
                <td class="p-3 text-right font-bold font-mono">{{ formatNumber(item.quantiteKg, 0) }}</td>
                <td class="p-3 text-slate-500">{{ item.observation || '-' }}</td>
                <td class="p-3"><BadgeStatus :status="item.statut" /></td>
                <td class="p-3">
                  <div class="flex flex-wrap gap-1.5">
                    <span
                      class="px-2 py-0.5 rounded-full bg-rose-50 border border-rose-100 text-rose-700 text-[10px] font-bold"
                      >Hors bilan fonderie</span
                    >
                    <span
                      class="px-2 py-0.5 rounded-full bg-emerald-50 border border-emerald-100 text-emerald-700 text-[10px] font-bold"
                      >Visible rapport inventaire</span
                    >
                  </div>
                </td>
                <td class="p-3 text-right">
                  <div class="inline-flex gap-2">
                    <button
                      v-if="item.statut === 'Brouillon'"
                      @click="handleValiderP3(item.id)"
                      class="px-2 py-1 bg-emerald-50 border border-emerald-100 rounded text-emerald-700 text-[10px] font-bold cursor-pointer"
                    >
                      Valider
                    </button>
                    <button
                      v-if="item.statut !== 'Annule'"
                      @click="handleAnnulerP3(item.id)"
                      title="Annuler la ligne"
                      class="p-1 hover:bg-rose-50 rounded text-slate-400 hover:text-rose-600 cursor-pointer"
                    >
                      <Ban class="w-3.5 h-3.5" />
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="fours.length === 0">
                <td colspan="8" class="p-4 text-center text-slate-400 font-semibold">
                  Aucune ligne pour cette période.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="bg-white rounded-xl border border-slate-100 shadow-sm p-6 space-y-4">
        <div class="flex items-center gap-2 border-b border-slate-50 pb-3">
          <Calculator class="w-4.5 h-4.5 text-blue-900" />
          <div>
            <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">
              Synthèse inventaire, rapport et bilan
            </h3>
            <p class="text-xs text-slate-500 mt-1">
              Le rapport d'inventaire inclut la partie Four. Le bilan matière fonderie ne retient que les lignes
              incluses dans le bilan.
            </p>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr
                class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
              >
                <th class="p-3">Type matière</th>
                <th class="p-3 text-right">Stock début</th>
                <th class="p-3 text-right">Partie 1</th>
                <th class="p-3 text-right">Partie 2</th>
                <th class="p-3 text-right text-[var(--color-attention-strong)]">Partie 3 Four</th>
                <th class="p-3 text-right">Total rapport inventaire</th>
                <th class="p-3 text-right text-blue-900">Total utilisé dans bilan matière</th>
                <th class="p-3 text-right">Stock fin</th>
                <th class="p-3">Anomalies</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="tot in totaux" :key="tot.codeTypeMatiere">
                <td class="p-3 font-bold text-slate-800 font-mono">{{ tot.codeTypeMatiere }}</td>
                <td class="p-3 text-right font-semibold font-mono">
                  <span v-if="tot.stockDebutKg !== null">{{ formatNumber(tot.stockDebutKg, 0) }} kg</span>
                  <span v-else class="text-slate-400">—</span>
                </td>
                <td class="p-3 text-right font-semibold font-mono">{{ formatNumber(tot.totalPartie1, 0) }} kg</td>
                <td class="p-3 text-right font-semibold font-mono">{{ formatNumber(tot.totalPartie2, 0) }} kg</td>
                <td class="p-3 text-right font-semibold font-mono text-[var(--color-attention-strong)]">
                  {{ formatNumber(tot.totalPartie3, 0) }} kg
                </td>
                <td class="p-3 text-right font-semibold font-mono">
                  {{ formatNumber(tot.totalInventaireRapport, 0) }} kg
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-900">
                  {{ formatNumber(tot.totalUtiliseBilan, 0) }} kg
                </td>
                <td class="p-3 text-right font-semibold font-mono">{{ formatNumber(tot.stockFinKg, 0) }} kg</td>
                <td class="p-3">
                  <div v-if="tot.anomalies.length" class="flex flex-wrap gap-1.5">
                    <span
                      v-for="anomalie in tot.anomalies"
                      :key="anomalie"
                      class="px-2 py-0.5 rounded-full bg-amber-50 border border-amber-100 text-amber-700 text-[10px] font-bold"
                      >{{ anomalie }}</span
                    >
                  </div>
                  <span v-else class="text-slate-400">—</span>
                </td>
              </tr>
              <tr class="bg-slate-50/50 font-black">
                <td class="p-3 uppercase text-slate-600">Total général</td>
                <td class="p-3 text-right font-mono">{{ formatNumber(totalGeneralStockDebut, 0) }} kg</td>
                <td class="p-3 text-right font-mono">
                  {{
                    formatNumber(
                      totaux.reduce((sum, t) => sum + t.totalPartie1, 0),
                      0
                    )
                  }}
                  kg
                </td>
                <td class="p-3 text-right font-mono">
                  {{
                    formatNumber(
                      totaux.reduce((sum, t) => sum + t.totalPartie2, 0),
                      0
                    )
                  }}
                  kg
                </td>
                <td class="p-3 text-right font-mono text-[var(--color-attention-strong)]">
                  {{ formatNumber(totalGeneralFour, 0) }} kg
                </td>
                <td class="p-3 text-right font-mono">{{ formatNumber(totalGeneralRapport, 0) }} kg</td>
                <td class="p-3 text-right font-mono text-blue-900">{{ formatNumber(totalGeneralBilan, 0) }} kg</td>
                <td class="p-3 text-right font-mono">{{ formatNumber(totalGeneralStockFin, 0) }} kg</td>
                <td class="p-3"></td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>
