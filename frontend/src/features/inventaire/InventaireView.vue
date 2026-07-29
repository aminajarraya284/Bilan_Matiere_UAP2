<script setup lang="ts">
import { computed, ref, onMounted } from 'vue';
import { useAuthStore } from '@/features/auth/auth.store';
import { Plus, Trash2, Layers, Calculator, Flame } from 'lucide-vue-next';
import { inventaireService } from './inventaire.service';
import {
  InventaireConteneur,
  InventaireCategorie,
  InventaireFour,
  InventaireTotalParMatiere
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
const isLoading = ref(true);
const canUseComments = computed(() => authStore.userRole === 'Admin');

const formP1 = ref({
  dateInventaire: BUSINESS_RULES.dateFinParDefaut,
  numeroConteneur: '',
  ofDestination: '',
  codeTypeMatiere: 'M3010L0',
  quantiteReelle: 0,
  unite: 'KG' as const,
  observation: ''
});

const formP2 = ref({
  categorie: 'REBUT',
  codeTypeMatiere: 'M3010L0',
  quantiteReelle: 0,
  unite: 'KG' as const,
  commentaire: ''
});

const formP3 = ref({
  dateInventaire: BUSINESS_RULES.dateFinParDefaut,
  mois: Number(BUSINESS_RULES.dateFinParDefaut.split('-')[1]),
  annee: Number(BUSINESS_RULES.dateFinParDefaut.split('-')[0]),
  four: 'Four CBP 01' as InventaireFour['four'],
  codeTypeMatiere: 'M3010L0',
  quantite: 0,
  unite: 'KG' as InventaireFour['unite'],
  observation: '',
  statut: 'Brouillon' as InventaireFour['statut']
});

const categoriesList = ['REBUT', 'MP LINGOT'];
const alloysList = ['M3010L0', 'M3000L0', 'M3060L0'];
const foursList: InventaireFour['four'][] = ['Four CBP 01', 'Four CBP 02', 'Four CDQ', 'Autre'];
const typeMatiereIds: Record<string, number> = {
  M3010L0: 1,
  M3000L0: 2,
  M3060L0: 3
};

const loadAllData = async () => {
  isLoading.value = true;
  try {
    const [cRes, catRes, fourRes, tRes] = await Promise.all([
      inventaireService.getConteneurs(),
      inventaireService.getCategories(),
      inventaireService.getFours(),
      inventaireService.getTotauxConsolides()
    ]);
    conteneurs.value = cRes;
    categories.value = catRes;
    fours.value = fourRes;
    totaux.value = tRes;
  } catch (error) {
    console.error('Failed to load inventory', error);
  } finally {
    isLoading.value = false;
  }
};

const totalGeneralRapport = computed(() => totaux.value.reduce((sum, item) => sum + item.totalInventaireRapport, 0));
const totalGeneralBilan = computed(() => totaux.value.reduce((sum, item) => sum + item.totalUtiliseBilan, 0));
const totalGeneralFour = computed(() => totaux.value.reduce((sum, item) => sum + item.totalPartie3, 0));

const handleAddP1 = async () => {
  if (!formP1.value.dateInventaire || !formP1.value.numeroConteneur || !formP1.value.ofDestination.trim()) {
    await dialog.alert('Veuillez remplir correctement tous les champs obligatoires.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  await inventaireService.createConteneur({
    dateInventaire: formP1.value.dateInventaire,
    mois: parseInt(formP1.value.dateInventaire.split('-')[1]),
    annee: parseInt(formP1.value.dateInventaire.split('-')[0]),
    numeroConteneur: formP1.value.numeroConteneur,
    ofDestination: formP1.value.ofDestination.trim(),
    codeArticle: '',
    typeMatiereId: typeMatiereIds[formP1.value.codeTypeMatiere] || 0,
    codeTypeMatiere: formP1.value.codeTypeMatiere,
    quantiteReelle: formP1.value.quantiteReelle,
    unite: formP1.value.unite,
    observation: canUseComments.value ? formP1.value.observation || undefined : undefined
  });

  formP1.value.numeroConteneur = '';
  formP1.value.ofDestination = '';
  formP1.value.quantiteReelle = 0;
  formP1.value.observation = '';
  await loadAllData();
};

const handleAddP2 = async () => {
  if (!formP2.value.categorie) {
    await dialog.alert('Veuillez remplir correctement tous les champs obligatoires.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  await inventaireService.createCategorie({
    mois: 6,
    annee: 2026,
    categorie: formP2.value.categorie,
    categorieInventaire: 'CategorieBilan',
    typeMatiereId: typeMatiereIds[formP2.value.codeTypeMatiere] || 0,
    codeTypeMatiere: formP2.value.codeTypeMatiere,
    quantiteReelle: formP2.value.quantiteReelle,
    unite: formP2.value.unite,
    commentaire: canUseComments.value ? formP2.value.commentaire || undefined : undefined,
    inclusDansBilanFonderie: true,
    visibleDansRapportInventaire: true
  });

  formP2.value.quantiteReelle = 0;
  formP2.value.commentaire = '';
  await loadAllData();
};

const handleAddP3 = async () => {
  if (!formP3.value.dateInventaire || !formP3.value.four) {
    await dialog.alert('Veuillez remplir correctement tous les champs obligatoires.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  await inventaireService.createFour({
    dateInventaire: formP3.value.dateInventaire,
    mois: formP3.value.mois,
    annee: formP3.value.annee,
    four: formP3.value.four,
    codeTypeMatiere: formP3.value.codeTypeMatiere,
    quantite: formP3.value.quantite,
    unite: formP3.value.unite,
    observation: canUseComments.value ? formP3.value.observation || undefined : undefined,
    statut: formP3.value.statut
  });

  formP3.value.quantite = 0;
  formP3.value.observation = '';
  formP3.value.statut = 'Brouillon';
  await loadAllData();
};

const handleValiderP1 = async (id: number) => {
  await inventaireService.validerConteneur(id);
  await loadAllData();
};

const handleDeleteP1 = async (id: number) => {
  if (
    await dialog.confirm('Voulez-vous supprimer cette ligne de conteneur ?', {
      title: 'Supprimer la ligne',
      tone: 'danger',
      confirmLabel: 'Supprimer'
    })
  ) {
    await inventaireService.deleteConteneur(id);
    await loadAllData();
  }
};

const handleValiderP2 = async (id: number) => {
  await inventaireService.validerCategorie(id);
  await loadAllData();
};

const handleDeleteP2 = async (id: number) => {
  if (
    await dialog.confirm('Voulez-vous supprimer cette ligne par catégorie ?', {
      title: 'Supprimer la ligne',
      tone: 'danger',
      confirmLabel: 'Supprimer'
    })
  ) {
    await inventaireService.deleteCategorie(id);
    await loadAllData();
  }
};

const handleValiderP3 = async (id: number) => {
  await inventaireService.validerFour(id);
  await loadAllData();
};

const handleDeleteP3 = async (id: number) => {
  if (
    await dialog.confirm('Voulez-vous supprimer cette ligne Four ?', {
      title: 'Supprimer la ligne',
      tone: 'danger',
      confirmLabel: 'Supprimer'
    })
  ) {
    await inventaireService.deleteFour(id);
    await loadAllData();
  }
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

    <div v-if="isLoading">
      <LoadingState />
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
          <FormField id="p1-of" label="OF" required>
            <input
              v-model="formP1.ofDestination"
              type="text"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
              placeholder="OF18100563"
              required
            />
          </FormField>
          <FormField id="p1-mat" label="Type matière" required>
            <select
              v-model="formP1.codeTypeMatiere"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option v-for="mat in alloysList" :key="mat" :value="mat">{{ mat }}</option>
            </select>
          </FormField>
          <FormField id="p1-qte" label="Poids réel (kg)" required>
            <input
              v-model="formP1.quantiteReelle"
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
                <th class="p-3 text-right">Quantité</th>
                <th class="p-3">Unité</th>
                <th v-if="canUseComments" class="p-3">Observation</th>
                <th class="p-3">Statut</th>
                <th class="p-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="item in conteneurs" :key="item.id">
                <td class="p-3 font-mono text-[11px] text-slate-600">{{ item.dateInventaire }}</td>
                <td class="p-3 font-bold text-slate-800 font-mono">{{ item.numeroConteneur }}</td>
                <td class="p-3 font-mono text-slate-600">{{ item.ofDestination || '-' }}</td>
                <td class="p-3 font-mono font-bold text-blue-900">{{ item.codeTypeMatiere }}</td>
                <td class="p-3 text-right font-black font-mono text-slate-800">
                  {{ formatNumber(item.quantiteReelle, 0) }}
                </td>
                <td class="p-3 font-bold font-mono text-slate-500 text-[10px]">{{ item.unite }}</td>
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
                      @click="handleDeleteP1(item.id)"
                      class="p-1 hover:bg-rose-50 rounded text-slate-400 hover:text-rose-600 cursor-pointer"
                    >
                      <Trash2 class="w-3.5 h-3.5" />
                    </button>
                  </div>
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
              v-model="formP2.codeTypeMatiere"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option v-for="mat in alloysList" :key="mat" :value="mat">{{ mat }}</option>
            </select>
          </FormField>
          <FormField id="p2-qte" label="Poids réel (kg)" required>
            <input
              v-model="formP2.quantiteReelle"
              type="number"
              min="0"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full font-mono"
              required
            />
          </FormField>
          <FormField v-if="canUseComments" id="p2-comment" label="Commentaire">
            <input
              v-model="formP2.commentaire"
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
                <th class="p-3 text-right">Quantité réelle</th>
                <th class="p-3">Unité</th>
                <th v-if="canUseComments" class="p-3">Commentaire</th>
                <th class="p-3">Statut</th>
                <th class="p-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="item in categories" :key="item.id">
                <td class="p-3 font-bold text-slate-800">{{ item.categorie }}</td>
                <td class="p-3 font-mono font-bold text-blue-900">{{ item.codeTypeMatiere }}</td>
                <td class="p-3 text-right font-bold font-mono">{{ formatNumber(item.quantiteReelle, 0) }}</td>
                <td class="p-3 text-slate-400 font-bold font-mono">{{ item.unite }}</td>
                <td v-if="canUseComments" class="p-3 text-slate-500 text-[11px] font-semibold">
                  {{ item.commentaire || '-' }}
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
                      @click="handleDeleteP2(item.id)"
                      class="p-1 hover:bg-rose-50 rounded text-slate-400 hover:text-rose-600 cursor-pointer"
                    >
                      <Trash2 class="w-3.5 h-3.5" />
                    </button>
                  </div>
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
          <FormField id="p3-mois" label="Mois" required>
            <input
              v-model="formP3.mois"
              type="number"
              min="1"
              max="12"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
              required
            />
          </FormField>
          <FormField id="p3-annee" label="Année" required>
            <input
              v-model="formP3.annee"
              type="number"
              min="2020"
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
              v-model="formP3.codeTypeMatiere"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option v-for="mat in alloysList" :key="mat" :value="mat">{{ mat }}</option>
            </select>
          </FormField>
          <FormField id="p3-qte" label="Quantité" required>
            <input
              v-model="formP3.quantite"
              type="number"
              min="0"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full font-mono"
              required
            />
          </FormField>
          <FormField id="p3-unite" label="Unité" required>
            <select
              v-model="formP3.unite"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option value="KG">KG</option>
              <option value="UN">UN</option>
            </select>
          </FormField>
          <FormField id="p3-statut" label="Statut" required>
            <select
              v-model="formP3.statut"
              class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full cursor-pointer"
            >
              <option value="Brouillon">Brouillon</option>
              <option value="Valide">Valide</option>
              <option value="Annule">Annule</option>
            </select>
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
                <th class="p-3">Mois</th>
                <th class="p-3">Année</th>
                <th class="p-3">Four</th>
                <th class="p-3">Type matière</th>
                <th class="p-3 text-right">Quantité</th>
                <th class="p-3">Unité</th>
                <th class="p-3">Observation</th>
                <th class="p-3">Statut</th>
                <th class="p-3">Inclus bilan fonderie</th>
                <th class="p-3">Visible rapport inventaire</th>
                <th class="p-3">Badges</th>
                <th class="p-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="item in fours" :key="item.id">
                <td class="p-3 font-mono text-[11px] text-slate-600">{{ item.dateInventaire }}</td>
                <td class="p-3 font-mono">{{ item.mois }}</td>
                <td class="p-3 font-mono">{{ item.annee }}</td>
                <td class="p-3 font-bold text-slate-800">{{ item.four }}</td>
                <td class="p-3 font-mono font-bold text-blue-900">{{ item.codeTypeMatiere }}</td>
                <td class="p-3 text-right font-bold font-mono">{{ formatNumber(item.quantite, 0) }}</td>
                <td class="p-3 font-mono text-slate-500">{{ item.unite }}</td>
                <td class="p-3 text-slate-500">{{ item.observation || '-' }}</td>
                <td class="p-3"><BadgeStatus :status="item.statut" /></td>
                <td class="p-3"><span class="text-rose-700 font-black">Non</span></td>
                <td class="p-3"><span class="text-emerald-700 font-black">Oui</span></td>
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
                      @click="handleDeleteP3(item.id)"
                      class="p-1 hover:bg-rose-50 rounded text-slate-400 hover:text-rose-600 cursor-pointer"
                    >
                      <Trash2 class="w-3.5 h-3.5" />
                    </button>
                  </div>
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
                <th class="p-3 text-right">Partie 1</th>
                <th class="p-3 text-right">Partie 2</th>
                <th class="p-3 text-right text-[var(--color-attention-strong)]">Partie 3 Four</th>
                <th class="p-3 text-right">Total rapport inventaire</th>
                <th class="p-3 text-right text-blue-900">Total utilisé dans bilan matière</th>
                <th class="p-3">Statut</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="tot in totaux" :key="tot.codeTypeMatiere">
                <td class="p-3 font-bold text-slate-800 font-mono">{{ tot.codeTypeMatiere }}</td>
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
                <td class="p-3"><BadgeStatus :status="tot.statut" /></td>
              </tr>
              <tr class="bg-slate-50/50 font-black">
                <td class="p-3 uppercase text-slate-600">Total général</td>
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
                <td class="p-3"><BadgeStatus status="Valide" /></td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>
