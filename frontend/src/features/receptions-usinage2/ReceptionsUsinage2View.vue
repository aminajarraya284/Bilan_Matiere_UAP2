<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useAuthStore } from '@/features/auth/auth.store';
import { Trash2, Plus, CheckCircle2, ArrowLeftRight, AlertCircle, Eye } from 'lucide-vue-next';
import { receptionsService } from './receptions.service';
import {
  ControleNonReception,
  ReceptionUsinage2Mouvement,
  VerificationControleNonReceptionPayload
} from './receptions.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import FormField from '@/components/ui/FormField.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { formatNumber } from '@/utils/formatters';
import { useAppDialog } from '@/composables/useAppDialog';
import { BUSINESS_RULES } from '@/config/businessRules';

const authStore = useAuthStore();
const dialog = useAppDialog();
const receptions = ref<ReceptionUsinage2Mouvement[]>([]);
const controleNonReception = ref<ControleNonReception[]>([]);
const isLoading = ref(true);
const isLoadingControle = ref(false);
const successMessage = ref('');
const highlightedReceptionId = ref<number | null>(null);
const selectedControle = ref<ControleNonReception | null>(null);
const verificationForm = ref<VerificationControleNonReceptionPayload>({
  statutControle: 'AVerifier',
  commentaireControle: '',
  justification: ''
});

// Access checks
const isAdmin = computed(() => authStore.userRole === 'Admin');
const canAdd = computed(() => authStore.userRole === 'Admin' || authStore.userRole === 'ResponsableUsinage2');

// Form state
const formRec = ref({
  numeroOF: '',
  dateReception: BUSINESS_RULES.dateFinParDefaut,
  origine: 'FONDERIE' as 'FONDERIE' | 'AUTRE',
  poidsTotalReceptionneKg: 0,
  nombrePiecesReceptionnees: 0,
  codeArticle: '',
  designationArticle: '',
  atelierOrigine: '',
  codeTypeMatiere: '',
  quantiteLienUtilisee: 0,
  uniteQuantiteLien: 'KG',
  poidsTheoriqueUnitaireKg: 0
});

const loadReceptionDefaults = async () => {
  if (!formRec.value.numeroOF.trim()) return;
  try {
    const defaults = await receptionsService.getReceptionDefaults(formRec.value.numeroOF.trim().toUpperCase());
    formRec.value = { ...formRec.value, ...defaults };
  } catch {
    formRec.value.codeArticle = '';
    formRec.value.designationArticle = '';
    formRec.value.atelierOrigine = '';
    formRec.value.codeTypeMatiere = '';
    formRec.value.quantiteLienUtilisee = 0;
    formRec.value.poidsTheoriqueUnitaireKg = 0;
  }
};

const loadReceptions = async () => {
  isLoading.value = true;
  try {
    receptions.value = await receptionsService.getReceptions();
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const loadControleNonReception = async () => {
  if (!isAdmin.value) return;
  isLoadingControle.value = true;
  try {
    controleNonReception.value = await receptionsService.getControleNonReception();
  } catch (err) {
    console.error(err);
  } finally {
    isLoadingControle.value = false;
  }
};

const controleTotals = computed(() => {
  const totals = controleNonReception.value.reduce(
    (acc, row) => {
      acc.quantiteEmrX3 += row.quantiteEmrX3;
      acc.quantiteReceptionneeU2Application += row.quantiteReceptionneeU2Application;
      acc.quantiteNonReceptionnee += row.quantiteNonReceptionnee;
      acc.nombrePieces += row.nombrePieces;
      return acc;
    },
    {
      quantiteEmrX3: 0,
      quantiteReceptionneeU2Application: 0,
      quantiteNonReceptionnee: 0,
      nombrePieces: 0
    }
  );

  return {
    ...totals,
    ecartParPiece: totals.nombrePieces > 0 ? totals.quantiteNonReceptionnee / totals.nombrePieces : 0
  };
});

const controleSummaryCards = computed(() => [
  {
    label: 'Total quantité EMR issue de X3',
    value: `${formatNumber(controleTotals.value.quantiteEmrX3, 2)} kg`
  },
  {
    label: 'Total quantité réceptionnée U2 application',
    value: `${formatNumber(controleTotals.value.quantiteReceptionneeU2Application, 2)} kg`
  },
  {
    label: 'Quantité non réceptionnée',
    value: `${formatNumber(controleTotals.value.quantiteNonReceptionnee, 2)} kg`
  },
  {
    label: 'Nombre de pièces',
    value: `${formatNumber(controleTotals.value.nombrePieces, 0)}`
  },
  {
    label: 'Écart par pièce',
    value: `${formatNumber(controleTotals.value.ecartParPiece, 4)} kg`
  }
]);

const controleAlert = computed(() => {
  const diff = controleTotals.value.quantiteNonReceptionnee;
  if (diff > 0) {
    return {
      className: 'bg-amber-50 border-amber-200 text-amber-900',
      text: 'Décalage détecté : une quantité issue de l’état EMR X3 n’est pas encore réceptionnée dans l’application Usinage 2.'
    };
  } else if (diff < 0) {
    return {
      className: 'bg-rose-50 border-rose-200 text-rose-800',
      text: 'Anomalie détectée : la quantité réceptionnée dans l’application Usinage 2 est supérieure à la quantité EMR issue de X3.'
    };
  } else {
    return {
      className: 'bg-emerald-50 border-emerald-200 text-emerald-800',
      text: 'Aucun décalage détecté entre l’état EMR X3 et les réceptions Usinage 2.'
    };
  }
});

const getControleStatusMeta = (row: ControleNonReception) => {
  switch (row.statutControle) {
    case 'OK':
      return { label: 'OK', className: 'bg-emerald-50 border-emerald-100 text-emerald-700', dot: 'bg-emerald-500' };
    case 'AVerifier':
      return { label: 'À vérifier', className: 'bg-amber-50 border-amber-100 text-amber-700', dot: 'bg-amber-500' };
    case 'Anomalie':
      return { label: 'Anomalie', className: 'bg-rose-50 border-rose-100 text-rose-700', dot: 'bg-rose-500' };
    case 'Justifie':
      return { label: 'Justifié', className: 'bg-blue-50 border-blue-100 text-blue-700', dot: 'bg-blue-500' };
    case 'Regularise':
      return {
        label: 'Régularisé',
        className: 'bg-emerald-50 border-emerald-100 text-emerald-800',
        dot: 'bg-emerald-600'
      };
    default:
      return {
        label: row.statutControle,
        className: 'bg-slate-50 border-slate-200 text-slate-700',
        dot: 'bg-slate-500'
      };
  }
};

const openControleVerification = (row: ControleNonReception) => {
  selectedControle.value = row;
  verificationForm.value = {
    statutControle: row.statutControle,
    commentaireControle: row.observation || '',
    justification: row.justification || ''
  };
};

const closeControleVerification = () => {
  selectedControle.value = null;
};

const submitControleVerification = async () => {
  if (!selectedControle.value) return;

  try {
    await receptionsService.verifierControleNonReception(selectedControle.value.id, {
      statutControle: verificationForm.value.statutControle,
      commentaireControle: verificationForm.value.commentaireControle,
      justification: verificationForm.value.justification
    });
    successMessage.value = 'Justification du contrôle enregistrée.';
    selectedControle.value = null;
    await loadControleNonReception();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const handleAddReception = async () => {
  if (
    !formRec.value.numeroOF ||
    !formRec.value.codeArticle ||
    formRec.value.poidsTotalReceptionneKg <= 0 ||
    formRec.value.nombrePiecesReceptionnees <= 0
  ) {
    await dialog.alert('Veuillez remplir correctement tous les champs obligatoires.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  try {
    const payload = {
      numeroOF: formRec.value.numeroOF.trim().toUpperCase(),
      dateReception: formRec.value.dateReception,
      origine: formRec.value.origine,
      poidsTotalReceptionneKg: formRec.value.poidsTotalReceptionneKg,
      nombrePiecesReceptionnees: formRec.value.nombrePiecesReceptionnees
    };

    const createdReception = await receptionsService.createReception(payload);
    formRec.value.numeroOF = '';
    formRec.value.poidsTotalReceptionneKg = 0;
    formRec.value.nombrePiecesReceptionnees = 0;
    await loadReceptionDefaults();
    successMessage.value = 'Réception Usinage 2 enregistrée avec succès !';
    await loadReceptions();
    highlightedReceptionId.value = createdReception.id;
    setTimeout(() => (highlightedReceptionId.value = null), 700);
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const handleDelete = async (id: number) => {
  if (
    await dialog.confirm('Voulez-vous supprimer cette ligne de réception ?', {
      title: 'Supprimer la réception',
      tone: 'danger',
      confirmLabel: 'Supprimer'
    })
  ) {
    try {
      await receptionsService.deleteReception(id);
      successMessage.value = 'Réception supprimée.';
      await loadReceptions();
      setTimeout(() => (successMessage.value = ''), 4000);
    } catch (err) {
      console.error(err);
    }
  }
};

onMounted(() => {
  loadReceptions();
  loadControleNonReception();
  handleArticleChange(); // Initialize labels
});
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <PageHeader
      title="Réceptions Usinage 2"
      description="Saisir, contrôler et suivre les réceptions U2 issues de la fonderie."
    />

    <!-- Notifications -->
    <Transition name="toast-motion">
      <div
        v-if="successMessage"
        class="motion-alert bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-emerald-800 text-xs font-semibold flex items-center gap-3"
        role="status"
        aria-live="polite"
      >
        <CheckCircle2 class="w-5 h-5 text-emerald-500 shrink-0" />
        <span>{{ successMessage }}</span>
      </div>
    </Transition>

    <!-- Rule notification -->
    <div class="semantic-attention border rounded-xl p-3 flex items-center gap-3 text-xs">
      <AlertCircle class="w-5 h-5 shrink-0 mt-0.5" />
      <p class="font-bold">
        Bilan matière : seules les lignes <strong>Origine = FONDERIE</strong> sont prises en compte.
      </p>
    </div>

    <!-- Admin-only non reception control -->
    <div v-if="isAdmin" class="bg-white rounded-xl border border-slate-100 shadow-sm p-5 space-y-5">
      <div class="flex flex-col xl:flex-row xl:items-start justify-between gap-4 border-b border-slate-100 pb-4">
        <div class="max-w-4xl">
          <h3 class="text-sm font-black text-slate-800 uppercase tracking-wider flex items-center gap-2">
            <AlertCircle class="w-4 h-4 text-blue-900" />
            Alerte quantité non réceptionnée
          </h3>
        </div>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-3 lg:grid-cols-5 gap-3">
        <div
          v-for="card in controleSummaryCards"
          :key="card.label"
          class="rounded-xl border border-slate-100 bg-slate-50/60 p-4 space-y-1"
        >
          <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">{{ card.label }}</div>
          <div class="text-base font-black text-slate-800 font-mono">{{ card.value }}</div>
        </div>
      </div>

      <div
        v-if="controleAlert"
        class="rounded-xl border p-4 text-xs font-bold leading-relaxed flex items-start gap-3"
        :class="controleAlert.className"
      >
        <AlertCircle class="w-5 h-5 shrink-0 mt-0.5" />
        <span>{{ controleAlert.text }}</span>
      </div>

      <div v-if="isLoadingControle">
        <LoadingState />
      </div>

      <div v-else class="overflow-x-auto rounded-xl border border-slate-100">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr
              class="bg-slate-50/80 border-b border-slate-100 text-[10px] text-slate-500 font-black uppercase tracking-wider"
            >
              <th class="p-3">Article</th>
              <th class="p-3">Type matière</th>
              <th class="p-3 text-right">Quantité EMR issue de X3</th>
              <th class="p-3 text-right">Quantité réceptionnée U2 application</th>
              <th class="p-3 text-right">Quantité non réceptionnée</th>
              <th class="p-3 text-right">Nombre de pièces</th>
              <th class="p-3 text-right">Écart par pièce</th>
              <th class="p-3">Statut contrôle</th>
              <th class="p-3">Observation</th>
              <th class="p-3 text-right">Action</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 text-xs font-medium">
            <tr v-for="row in controleNonReception" :key="`controle-nr-${row.id}`" class="hover:bg-slate-50/40">
              <td class="p-3">
                <div class="font-bold text-slate-800">{{ row.codeArticle }}</div>
                <div class="text-[10px] font-semibold text-slate-400">{{ row.designationArticle || '—' }}</div>
              </td>
              <td class="p-3 font-black font-mono text-blue-900">{{ row.codeTypeMatiere }}</td>
              <td class="p-3 text-right font-bold font-mono text-slate-700">
                {{ formatNumber(row.quantiteEmrX3, 2) }}
              </td>
              <td class="p-3 text-right font-bold font-mono text-slate-700">
                {{ formatNumber(row.quantiteReceptionneeU2Application, 2) }}
              </td>
              <td
                class="p-3 text-right font-black font-mono"
                :class="row.quantiteNonReceptionnee !== 0 ? 'text-amber-700' : 'text-emerald-700'"
              >
                {{ formatNumber(row.quantiteNonReceptionnee, 2) }}
              </td>
              <td class="p-3 text-right font-bold font-mono text-slate-700">{{ formatNumber(row.nombrePieces, 0) }}</td>
              <td class="p-3 text-right font-bold font-mono text-slate-700">
                {{ formatNumber(row.ecartParPiece, 4) }}
              </td>
              <td class="p-3">
                <span
                  class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[10px] font-bold border whitespace-nowrap"
                  :class="getControleStatusMeta(row).className"
                >
                  <span class="w-1.5 h-1.5 rounded-full" :class="getControleStatusMeta(row).dot"></span>
                  {{ getControleStatusMeta(row).label }}
                </span>
              </td>
              <td class="p-3 max-w-[220px] text-slate-600">
                {{ row.observation || '—' }}
              </td>
              <td class="p-3 text-right">
                <button
                  v-if="isAdmin"
                  @click="openControleVerification(row)"
                  class="inline-flex items-center justify-center gap-1.5 bg-blue-900 hover:bg-blue-800 text-white font-bold text-[10px] uppercase tracking-wider py-2 px-3 rounded-lg cursor-pointer active:scale-95 transition-all"
                >
                  <Eye class="w-3.5 h-3.5" />
                  Vérifier
                </button>
              </td>
            </tr>
            <tr v-if="!controleNonReception.length">
              <td colspan="10" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
                Aucun contrôle non réceptionné disponible
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Saisie form only for Admin & Responsable Usinage 2 -->
    <div v-if="canAdd" class="bg-white rounded-xl border border-slate-100 shadow-sm p-6 space-y-6">
      <div class="flex items-center gap-2 border-b border-slate-50 pb-3">
        <ArrowLeftRight class="w-4.5 h-4.5 text-blue-900" />
        <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">Saisir un Nouveau Bon de Réception</h3>
      </div>

      <form
        @submit.prevent="handleAddReception"
        class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 bg-slate-50/50 border border-slate-100 p-4 rounded-xl"
      >
        <FormField id="r-of" label="N° OF" hint="Ordre de fabrication." required>
          <input
            type="text"
            v-model="formRec.numeroOF"
            @change="loadReceptionDefaults"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono"
            placeholder="OF18100563"
            required
          />
        </FormField>

        <FormField id="r-date" label="Date réception" hint="Jour d'entrée U2." required>
          <input
            type="date"
            v-model="formRec.dateReception"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white"
            required
          />
        </FormField>

        <FormField id="r-art" label="Article" hint="Déterminé depuis l’OF sélectionné." required automatic>
          <input
            type="text"
            v-model="formRec.codeArticle"
            readonly
            class="bg-slate-100 border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
          />
        </FormField>

        <FormField
          id="r-designation"
          label="Désignation"
          hint="Remplie depuis la référence article."
          required
          automatic
        >
          <input
            type="text"
            v-model="formRec.designationArticle"
            readonly
            class="bg-slate-100 border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white"
            placeholder="Désignation complète"
            required
          />
        </FormField>

        <FormField id="r-atelier-auto" label="Atelier d’origine" hint="Rempli depuis la gamme." required automatic>
          <input
            type="text"
            v-model="formRec.atelierOrigine"
            readonly
            class="bg-slate-100 border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
          />
        </FormField>

        <FormField id="r-poidsphys" label="Poids physique (kg)" hint="Poids total saisi à la réception." required>
          <input
            type="number"
            v-model="formRec.poidsTotalReceptionneKg"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono"
            min="1"
            placeholder="0.00"
            required
          />
        </FormField>

        <FormField id="r-pcs" label="Nombre de pièces" hint="Quantité reçue en pièces." required>
          <input
            type="number"
            v-model="formRec.nombrePiecesReceptionnees"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono"
            min="1"
            placeholder="0"
            required
          />
        </FormField>

        <FormField id="r-matiere-auto" label="Type matière" hint="Rempli depuis la nomenclature." required automatic>
          <input
            type="text"
            v-model="formRec.codeTypeMatiere"
            readonly
            class="bg-slate-100 border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
          />
        </FormField>

        <FormField
          id="r-lien-auto"
          label="Quantité de lien théorique"
          hint="Remplie depuis la nomenclature."
          required
          automatic
        >
          <input
            type="text"
            :value="`${formRec.quantiteLienUtilisee} ${formRec.uniteQuantiteLien}`"
            readonly
            class="bg-slate-100 border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
          />
        </FormField>

        <div class="flex items-end">
          <button
            type="submit"
            class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-lg flex items-center justify-center gap-1.5 cursor-pointer active:scale-95 transition-all"
          >
            <Plus class="w-4 h-4" />
            Déclarer Réception
          </button>
        </div>
      </form>
    </div>

    <!-- Receptions Table -->
    <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col">
      <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
        <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">Mouvements de Réception Déclarés</h3>
        <span class="text-[10px] font-bold text-slate-400 font-mono">Total : {{ receptions.length }} bons</span>
      </div>

      <div v-if="isLoading">
        <LoadingState />
      </div>

      <div v-else class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr
              class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
            >
              <th class="p-3">OF</th>
              <th class="p-3">Date Réception</th>
              <th class="p-3">Type Matière</th>
              <th class="p-3">Article Concerné</th>
              <th class="p-3 text-right">Poids Déclaré (kg)</th>
              <th class="p-3 text-right">Poids Physique (kg)</th>
              <th class="p-3 text-right">Nb Pièces</th>
              <th class="p-3 text-center">Atelier Origine</th>
              <th class="p-3">Mois</th>
              <th class="p-3">Source</th>
              <th class="p-3 text-right" v-if="authStore.userRole === 'Admin'">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 text-xs font-medium">
            <tr
              v-for="row in receptions"
              :key="row.id"
              class="hover:bg-slate-50/30 transition-colors"
              :class="{
                'bg-amber-50/10': row.atelierOrigine !== 'FONDERIE',
                'motion-row-added': row.id === highlightedReceptionId
              }"
            >
              <td class="p-3 font-bold text-slate-800 font-mono">{{ row.numeroOF }}</td>
              <td class="p-3 font-mono text-slate-600">{{ row.dateReception }}</td>
              <td class="p-3 font-bold font-mono text-blue-900">{{ row.codeTypeMatiere }}</td>
              <td class="p-3">
                <span class="font-bold text-slate-800">{{ row.codeArticle }}</span>
                <p class="text-[10px] text-slate-400 font-semibold mt-0.5">{{ row.designationArticle }}</p>
              </td>
              <td class="p-3 text-right font-bold font-mono text-slate-700">{{ formatNumber(row.poidsKg, 1) }}</td>
              <td class="p-3 text-right font-bold font-mono text-blue-900">{{ formatNumber(row.poidsPhysique, 1) }}</td>
              <td class="p-3 text-right font-mono">{{ formatNumber(row.nombrePieces, 0) }} pcs</td>
              <td class="p-3 text-center">
                <span
                  class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider"
                  :class="
                    row.atelierOrigine === 'FONDERIE'
                      ? 'bg-emerald-50 text-emerald-700 border border-emerald-100'
                      : 'bg-slate-100 text-slate-500 border border-slate-200'
                  "
                >
                  {{ row.atelierOrigine }}
                </span>
              </td>
              <td class="p-3 font-mono text-slate-400 text-[11px]">{{ row.mois }}</td>
              <td class="p-3">
                <span class="text-[10px] font-bold text-slate-500">{{ row.source }}</span>
              </td>
              <td class="p-3 text-right" v-if="authStore.userRole === 'Admin'">
                <button
                  @click="handleDelete(row.id)"
                  class="p-1 hover:bg-rose-50 rounded text-slate-400 hover:text-rose-600 cursor-pointer"
                >
                  <Trash2 class="w-3.5 h-3.5" />
                </button>
              </td>
            </tr>
            <tr v-if="!receptions.length">
              <td colspan="11" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
                Aucune réception enregistrée
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- verification modal -->
    <Transition name="dialog-motion">
      <div
        v-if="selectedControle"
        class="fixed inset-0 z-[90] flex items-center justify-center bg-slate-950/45 px-4 py-6"
        role="dialog"
        aria-modal="true"
        @click.self="closeControleVerification"
      >
        <div
          class="app-dialog-panel w-full max-w-4xl max-h-[90vh] overflow-y-auto rounded-xl bg-white shadow-2xl border border-slate-100"
        >
          <div class="flex items-start justify-between gap-4 border-b border-slate-100 p-5">
            <div>
              <h3 class="text-sm font-black text-slate-800 uppercase tracking-wider">Vérifier le contrôle</h3>
              <p class="mt-1 text-xs font-semibold text-slate-500">
                Les données X3 et les quantités calculées sont affichées en lecture seule.
              </p>
            </div>
            <button
              @click="closeControleVerification"
              class="rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-bold text-slate-600 hover:bg-slate-50 cursor-pointer"
            >
              Fermer
            </button>
          </div>

          <div class="p-5 space-y-5">
            <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-3">
              <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">Article</div>
                <div class="mt-1 text-xs font-black text-slate-800">{{ selectedControle.codeArticle }}</div>
                <div class="mt-0.5 text-[10px] font-semibold text-slate-500">
                  {{ selectedControle.designationArticle || '—' }}
                </div>
              </div>
              <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">Type matière</div>
                <div class="mt-1 text-xs font-black font-mono text-blue-900">
                  {{ selectedControle.codeTypeMatiere }}
                </div>
              </div>
              <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">
                  Quantité EMR issue de X3
                </div>
                <div class="mt-1 text-xs font-black font-mono text-slate-800">
                  {{ formatNumber(selectedControle.quantiteEmrX3, 2) }}
                </div>
              </div>
              <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">
                  Quantité réceptionnée U2 application
                </div>
                <div class="mt-1 text-xs font-black font-mono text-slate-800">
                  {{ formatNumber(selectedControle.quantiteReceptionneeU2Application, 2) }}
                </div>
              </div>
              <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">
                  Quantité non réceptionnée
                </div>
                <div class="mt-1 text-xs font-black font-mono text-amber-700">
                  {{ formatNumber(selectedControle.quantiteNonReceptionnee, 2) }}
                </div>
              </div>
              <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">Nombre de pièces</div>
                <div class="mt-1 text-xs font-black font-mono text-slate-800">
                  {{ formatNumber(selectedControle.nombrePieces, 0) }}
                </div>
              </div>
              <div class="rounded-xl border border-slate-100 bg-slate-50 p-3">
                <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">Écart par pièce</div>
                <div class="mt-1 text-xs font-black font-mono text-slate-800">
                  {{ formatNumber(selectedControle.ecartParPiece, 4) }}
                </div>
              </div>
            </div>

            <div class="rounded-xl border border-slate-100 bg-slate-50/60 p-4">
              <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">Statut actuel</div>
              <div class="mt-2">
                <span
                  class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[10px] font-bold border"
                  :class="getControleStatusMeta(selectedControle).className"
                >
                  <span class="w-1.5 h-1.5 rounded-full" :class="getControleStatusMeta(selectedControle).dot"></span>
                  {{ getControleStatusMeta(selectedControle).label }}
                </span>
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <FormField id="controle-statut" label="Statut de vérification" hint="Champ modifiable Admin." required>
                <select
                  v-model="verificationForm.statutControle"
                  class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white cursor-pointer"
                >
                  <option value="OK">OK</option>
                  <option value="AVerifier">À vérifier</option>
                  <option value="Justifie">Justifié</option>
                  <option value="Regularise">Régularisé</option>
                  <option value="Anomalie">Anomalie</option>
                </select>
              </FormField>

              <FormField id="controle-commentaire" label="Commentaire (Observation)" hint="Commentaire de contrôle.">
                <textarea
                  v-model="verificationForm.commentaireControle"
                  rows="3"
                  class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white"
                  placeholder="Commentaire de contrôle (Observation)"
                ></textarea>
              </FormField>
            </div>

            <FormField id="controle-justification" label="Justification" hint="Justification Admin de l'écart.">
              <textarea
                v-model="verificationForm.justification"
                rows="4"
                class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white"
                placeholder="Ex. lot en attente, réception non saisie, retard transfert, anomalie poids..."
              ></textarea>
            </FormField>

            <div class="flex flex-col sm:flex-row justify-end gap-3 border-t border-slate-100 pt-4">
              <button
                @click="closeControleVerification"
                class="px-4 py-2 border border-slate-200 text-slate-600 rounded-lg text-xs font-bold hover:bg-slate-50 cursor-pointer"
              >
                Annuler
              </button>
              <button
                @click="submitControleVerification"
                class="inline-flex items-center justify-center gap-1.5 px-4 py-2 bg-blue-900 hover:bg-blue-800 text-white rounded-lg text-xs font-bold cursor-pointer"
              >
                <CheckCircle2 class="w-3.5 h-3.5" />
                Enregistrer la justification
              </button>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>
