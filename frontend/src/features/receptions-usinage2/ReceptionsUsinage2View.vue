<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useAuthStore } from '@/features/auth/auth.store';
import { Trash2, Plus, CheckCircle2, ArrowLeftRight, AlertCircle } from 'lucide-vue-next';
import { receptionsService } from './receptions.service';
import { bilanUsinage2Service } from './bilanUsinage2.service';
import type { ReceptionUsinage2Mouvement } from './receptions.types';
import type { BilanUsinage2 } from './bilanUsinage2.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import FormField from '@/components/ui/FormField.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { formatNumber } from '@/utils/formatters';
import { useAppDialog } from '@/composables/useAppDialog';
import { BUSINESS_RULES } from '@/config/businessRules';

const authStore = useAuthStore();
const dialog = useAppDialog();
const receptions = ref<ReceptionUsinage2Mouvement[]>([]);
const isLoading = ref(true);
const successMessage = ref('');
const highlightedReceptionId = ref<number | null>(null);

// Bilan Usinage 2 (en pieces, calcule server-side) : remplace l'ancien panneau de controle
// non-reception qui reposait sur des quantites en kg incompatibles avec la formule confirmee.
const bilanPeriode = ref(new Date().toISOString().slice(0, 7));
const bilanUsinage2 = ref<BilanUsinage2 | null>(null);
const isLoadingBilan = ref(false);
const bilanIndisponible = ref(false);

// Access checks
const isAdmin = computed(() => authStore.userRole === 'Admin');
const canAdd = computed(() => authStore.userRole === 'Admin' || authStore.userRole === 'ResponsableUsinage2');

// Le workflow Brouillon -> Valide/Annule n'existe que cote API reelle : les receptions mock
// sont creees directement en statut Valide (voir receptions.service.ts), pas de bouton necessaire.
const isReceptionsRealApi = computed(() => bilanUsinage2Service.isAvailable());

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
  poidsTheoriqueUnitaireKg: 0,
  poidsPhysiqueUnitaireKg: null as number | null,
  statutArticle: '',
  statutAtelier: '',
  statutTypeMatiere: '',
  statutPoidsPhysique: ''
});

const chargementContexte = ref(false);

/** Affiche une valeur unitaire en kg/piece, ou un tiret si elle est indisponible. */
const formatUnitaire = (valeur: number | null | undefined): string =>
  typeof valeur === 'number' && valeur > 0 ? `${valeur} kg/pièce` : '—';

/**
 * Motifs empechant la declaration. Le bouton reste desactive tant que la liste
 * n'est pas vide : aucune reception n'est enregistree avec des champs automatiques
 * non resolus.
 */
const blocagesReception = computed(() => {
  const motifs: string[] = [];
  const f = formRec.value;
  if (!f.numeroOF.trim()) motifs.push('N° OF absent');
  if (!f.dateReception) motifs.push('Date de réception absente');
  if (!f.codeArticle) motifs.push('Article introuvable');
  if (!f.atelierOrigine) motifs.push("Atelier d'origine introuvable");
  if (!f.codeTypeMatiere) motifs.push('Type matière introuvable');
  if (Number(f.nombrePiecesReceptionnees) <= 0) motifs.push('Quantité réceptionnée doit être positive');
  if (Number(f.poidsTotalReceptionneKg) < 0) motifs.push('Poids réceptionné total négatif');
  if (!f.poidsPhysiqueUnitaireKg) motifs.push('Poids physique unitaire manquant');
  if (!f.poidsTheoriqueUnitaireKg) motifs.push('Poids théorique introuvable');
  if (chargementContexte.value) motifs.push('Données automatiques en cours de chargement');
  return motifs;
});

/**
 * La reception est rattachee au mois de sa date, pas a la periode affichee.
 * On previent sans jamais corriger la date a la place de l'utilisateur.
 */
const avertissementPeriode = computed(() => {
  const dateReception = formRec.value.dateReception;
  if (!dateReception) return '';
  const periodeSaisie = dateReception.slice(0, 7);
  if (periodeSaisie === bilanPeriode.value) return '';
  const libelle = new Date(`${periodeSaisie}-01T00:00:00`).toLocaleDateString('fr-FR', {
    month: 'long',
    year: 'numeric'
  });
  return `La date de réception ne correspond pas à la période affichée. Cette réception sera comptabilisée dans le bilan de ${libelle}.`;
});

const loadReceptionDefaults = async () => {
  if (!formRec.value.numeroOF.trim()) return;
  chargementContexte.value = true;
  try {
    const defaults = await receptionsService.getReceptionDefaults(
      formRec.value.numeroOF.trim().toUpperCase(),
      formRec.value.dateReception
    );
    formRec.value = { ...formRec.value, ...defaults };
  } catch {
    // Contexte non resolu : on vide les champs automatiques plutot que de
    // conserver des valeurs periemes d'un OF precedent.
    formRec.value.codeArticle = '';
    formRec.value.designationArticle = '';
    formRec.value.atelierOrigine = '';
    formRec.value.codeTypeMatiere = '';
    formRec.value.quantiteLienUtilisee = 0;
    formRec.value.poidsTheoriqueUnitaireKg = 0;
    formRec.value.poidsPhysiqueUnitaireKg = null;
  } finally {
    chargementContexte.value = false;
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

const loadBilanUsinage2 = async () => {
  if (!isAdmin.value) return;
  if (!bilanUsinage2Service.isAvailable()) {
    bilanIndisponible.value = true;
    return;
  }
  isLoadingBilan.value = true;
  bilanIndisponible.value = false;
  try {
    bilanUsinage2.value = await bilanUsinage2Service.getBilan(bilanPeriode.value);
  } catch (err) {
    console.error(err);
    bilanUsinage2.value = null;
  } finally {
    isLoadingBilan.value = false;
  }
};

const bilanSummaryCards = computed(() => [
  {
    // Meme libelle et meme unite que l'ecran Bilan matiere, pour que les deux
    // affichent la meme valeur sur une meme periode.
    label: 'Quantité totale réceptionnée U2 (pièces)',
    value: formatNumber(bilanUsinage2.value?.quantiteTotaleReceptionneePieces ?? 0, 0)
  },
  {
    label: 'Lignes du bilan',
    value: `${bilanUsinage2.value?.nombreLignes ?? 0}`
  },
  {
    label: 'Lignes données manquantes',
    value: `${bilanUsinage2.value?.nombreLignesDonneesManquantes ?? 0}`
  }
]);

const getBilanStatutMeta = (statut: string) => {
  switch (statut) {
    case 'OK':
      return { label: 'OK', className: 'bg-emerald-50 border-emerald-100 text-emerald-700', dot: 'bg-emerald-500' };
    case 'AVerifier':
      return { label: 'À vérifier', className: 'bg-amber-50 border-amber-100 text-amber-700', dot: 'bg-amber-500' };
    case 'Anomalie':
      return { label: 'Anomalie', className: 'bg-rose-50 border-rose-100 text-rose-700', dot: 'bg-rose-500' };
    default:
      return {
        label: 'Données manquantes',
        className: 'bg-slate-50 border-slate-200 text-slate-700',
        dot: 'bg-slate-500'
      };
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

const handleValider = async (row: ReceptionUsinage2Mouvement) => {
  if (!row.apiId) return;
  try {
    await receptionsService.validerReception(row.apiId);
    successMessage.value = 'Réception validée.';
    await loadReceptions();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
    await dialog.alert(
      err instanceof Error ? err.message : 'La validation a échoué (poids physique manquant ?).',
      { title: 'Validation impossible', tone: 'danger' }
    );
  }
};

const handleAnnuler = async (row: ReceptionUsinage2Mouvement) => {
  if (!row.apiId) return;
  const confirmed = await dialog.confirm(`Annuler la réception ${row.numeroOF} ?`, {
    title: 'Annuler la réception',
    tone: 'danger',
    confirmLabel: 'Annuler la réception'
  });
  if (!confirmed) return;

  try {
    await receptionsService.annulerReception(row.apiId);
    successMessage.value = 'Réception annulée.';
    await loadReceptions();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

onMounted(() => {
  loadReceptions();
  loadBilanUsinage2();
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

    <!-- Admin-only : Bilan Usinage 2 en pieces (calcule server-side) -->
    <div v-if="isAdmin" class="bg-white rounded-xl border border-slate-100 shadow-sm p-5 space-y-5">
      <div class="flex flex-col xl:flex-row xl:items-center justify-between gap-4 border-b border-slate-100 pb-4">
        <div class="max-w-4xl">
          <h3 class="text-sm font-black text-slate-800 uppercase tracking-wider flex items-center gap-2">
            <AlertCircle class="w-4 h-4 text-blue-900" />
            Bilan Usinage 2 (pièces)
          </h3>
        </div>
        <div class="flex items-center gap-2">
          <input
            type="month"
            v-model="bilanPeriode"
            @change="loadBilanUsinage2"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold focus:bg-white"
          />
        </div>
      </div>

      <div v-if="bilanIndisponible" class="rounded-xl border border-slate-200 bg-slate-50 p-4 text-xs font-bold text-slate-500">
        Le Bilan Usinage 2 n'est disponible qu'avec une source API réelle.
      </div>

      <template v-else>
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div
            v-for="card in bilanSummaryCards"
            :key="card.label"
            class="rounded-xl border border-slate-100 bg-slate-50/60 p-4 space-y-1"
          >
            <div class="text-[10px] font-black uppercase tracking-wider text-slate-400">{{ card.label }}</div>
            <div class="text-base font-black text-slate-800 font-mono">{{ card.value }}</div>
          </div>
        </div>

        <div v-if="isLoadingBilan">
          <LoadingState />
        </div>

        <div v-else class="overflow-x-auto rounded-xl border border-slate-100">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr
                class="bg-slate-50/80 border-b border-slate-100 text-[10px] text-slate-500 font-black uppercase tracking-wider"
              >
                <th class="p-3">OF</th>
                <th class="p-3">Article</th>
                <th class="p-3">Type matière</th>
                <th class="p-3 text-right">CTRL EMR (pièces)</th>
                <th class="p-3 text-right">Rebut EMR (pièces)</th>
                <th class="p-3 text-right">Nombre de pièces</th>
                <th class="p-3 text-right">Quantité reçue U2 (pièces)</th>
                <th class="p-3 text-right">Quantité non réceptionnée (pièces)</th>
                <th class="p-3 text-right">Rebut U2 (pièces)</th>
                <th class="p-3 text-right">Entrée OF (pièces)</th>
                <th class="p-3 text-right">Bilan U2 (pièces)</th>
                <th class="p-3">Statut</th>
                <th class="p-3">Anomalies</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr
                v-for="ligne in bilanUsinage2?.lignes ?? []"
                :key="`bilan-${ligne.numeroOF}-${ligne.codeArticle}`"
                class="hover:bg-slate-50/40"
              >
                <td class="p-3 font-bold font-mono text-slate-800">{{ ligne.numeroOF }}</td>
                <td class="p-3 font-semibold text-slate-700">{{ ligne.codeArticle }}</td>
                <td class="p-3 font-black font-mono text-blue-900">{{ ligne.typeMatiere ?? '—' }}</td>
                <td class="p-3 text-right font-bold font-mono text-slate-700">
                  {{ ligne.quantiteCtrlEmrPieces === null ? '—' : formatNumber(ligne.quantiteCtrlEmrPieces, 0) }}
                </td>
                <td class="p-3 text-right font-bold font-mono text-slate-400">
                  {{ ligne.rebutEmrPieces === null ? '—' : formatNumber(ligne.rebutEmrPieces, 0) }}
                </td>
                <td class="p-3 text-right font-bold font-mono text-slate-700">
                  {{ ligne.nombreDePieces === null ? '—' : formatNumber(ligne.nombreDePieces, 0) }}
                </td>
                <td class="p-3 text-right font-bold font-mono text-blue-700">
                  {{ formatNumber(ligne.quantiteReceptionneePieces, 0) }}
                </td>
                <td class="p-3 text-right font-bold font-mono text-slate-400">
                  {{
                    ligne.quantiteNonReceptionneePieces === null
                      ? '—'
                      : formatNumber(ligne.quantiteNonReceptionneePieces, 0)
                  }}
                </td>
                <td class="p-3 text-right font-bold font-mono text-slate-500">
                  {{ formatNumber(ligne.rebutUsinage2Pieces, 0) }}
                </td>
                <td class="p-3 text-right font-bold font-mono text-slate-500">
                  {{ ligne.entreeOfPieces === null ? '—' : formatNumber(ligne.entreeOfPieces, 0) }}
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-900">
                  {{ ligne.bilanUsinage2Pieces === null ? '—' : formatNumber(ligne.bilanUsinage2Pieces, 0) }}
                </td>
                <td class="p-3">
                  <span
                    class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[10px] font-bold border whitespace-nowrap"
                    :class="getBilanStatutMeta(ligne.statutReception).className"
                  >
                    <span class="w-1.5 h-1.5 rounded-full" :class="getBilanStatutMeta(ligne.statutReception).dot"></span>
                    {{ getBilanStatutMeta(ligne.statutReception).label }}
                  </span>
                </td>
                <td class="p-3 max-w-[280px] text-slate-500 text-[11px]">
                  {{ ligne.anomalies.join(' • ') || '—' }}
                </td>
              </tr>
              <tr v-if="!bilanUsinage2?.lignes?.length">
                <td colspan="13" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
                  Aucune réception validée pour cette période
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
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

        <FormField
          id="r-poids-total"
          label="Poids réceptionné total (kg)"
          hint="Poids total mesuré pour cette réception."
          required
        >
          <input
            type="number"
            v-model="formRec.poidsTotalReceptionneKg"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono"
            min="0"
            step="0.001"
            placeholder="0.00"
            required
          />
        </FormField>

        <FormField
          id="r-quantite-recue"
          label="Quantité réceptionnée (pièces)"
          hint="Nombre de pièces reçues dans ce bon de réception."
          required
        >
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
          id="r-poids-physique-unitaire"
          label="Poids physique unitaire (kg/pièce)"
          hint="Valeur automatique provenant du Contrôle poids Fonderie validé."
          required
          automatic
        >
          <input
            type="text"
            :value="formatUnitaire(formRec.poidsPhysiqueUnitaireKg)"
            readonly
            class="bg-slate-100 border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
          />
        </FormField>

        <FormField
          id="r-poids-theorique-unitaire"
          label="Poids théorique unitaire (kg/pièce)"
          hint="Valeur automatique issue de la nomenclature."
          required
          automatic
        >
          <input
            type="text"
            :value="formatUnitaire(formRec.poidsTheoriqueUnitaireKg)"
            readonly
            class="bg-slate-100 border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full"
          />
        </FormField>

        <div
          v-if="avertissementPeriode"
          class="sm:col-span-2 lg:col-span-3 rounded-lg border border-amber-200 bg-amber-50 p-3 text-[11px] font-semibold text-amber-900"
        >
          {{ avertissementPeriode }}
        </div>

        <div class="flex flex-col justify-end gap-2">
          <button
            type="submit"
            :disabled="blocagesReception.length > 0"
            class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-lg flex items-center justify-center gap-1.5 cursor-pointer active:scale-95 transition-all disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Plus class="w-4 h-4" />
            Déclarer Réception
          </button>
          <ul v-if="blocagesReception.length" class="space-y-0.5">
            <li
              v-for="motif in blocagesReception"
              :key="motif"
              class="text-[10px] font-semibold text-rose-700"
            >
              {{ motif }}
            </li>
          </ul>
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
              <th class="p-3">Statut</th>
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
              <td class="p-3 text-right font-bold font-mono text-slate-700">
                {{ formatNumber(row.poidsTotalReceptionneKg, 1) }}
              </td>
              <td class="p-3 text-right font-bold font-mono text-blue-900">
                {{ formatNumber(row.poidsPhysiqueUnitaireKg, 1) }}
              </td>
              <td class="p-3 text-right font-mono">{{ formatNumber(row.nombrePiecesReceptionnees, 0) }} pcs</td>
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
                <span
                  class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider"
                  :class="{
                    'bg-slate-100 text-slate-600': row.statut === 'Brouillon',
                    'bg-emerald-50 text-emerald-700 border border-emerald-100': row.statut === 'Valide',
                    'bg-rose-50 text-rose-600 border border-rose-100': row.statut === 'Annule' || row.statut === 'Cloture'
                  }"
                >
                  {{ row.statut }}
                </span>
              </td>
              <td class="p-3 text-right" v-if="authStore.userRole === 'Admin'">
                <div v-if="isReceptionsRealApi" class="flex justify-end gap-1">
                  <button
                    v-if="row.statut === 'Brouillon'"
                    @click="handleValider(row)"
                    class="px-2 py-1 rounded text-[10px] font-bold uppercase bg-emerald-50 hover:bg-emerald-100 text-emerald-700 cursor-pointer"
                  >
                    Valider
                  </button>
                  <button
                    v-if="row.statut !== 'Annule'"
                    @click="handleAnnuler(row)"
                    class="px-2 py-1 rounded text-[10px] font-bold uppercase bg-rose-50 hover:bg-rose-100 text-rose-600 cursor-pointer"
                  >
                    Annuler
                  </button>
                </div>
                <button
                  v-else
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

  </div>
</template>
