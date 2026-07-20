<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useAuthStore } from '@/features/auth/auth.store';
import { Trash2, Plus, CheckCircle2, Flame } from 'lucide-vue-next';
import { dechetsService } from './dechets.service';
import { DechetMouvement, DechetType } from './dechets.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import FormField from '@/components/ui/FormField.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { formatNumber } from '@/utils/formatters';
import { useAppDialog } from '@/composables/useAppDialog';
import { BUSINESS_RULES } from '@/config/businessRules';

const authStore = useAuthStore();
const dialog = useAppDialog();
const dechets = ref<DechetMouvement[]>([]);
const isLoading = ref(true);
const successMessage = ref('');

// Check permissions
const isReadOnly = computed(() => authStore.userRole !== 'Admin' && authStore.userRole !== 'ResponsableFonderie');
const canUseComments = computed(() => authStore.userRole === 'Admin');

// Form state
const formDechet = ref({
  dateDeclaration: BUSINESS_RULES.dateFinParDefaut,
  semaine: BUSINESS_RULES.semaineDeclarationParDefaut,
  codeTypeMatiere: 'M3010L0',
  quantiteKg: 0,
  unite: 'KG' as const,
  typeDechet: 'Bavure' as DechetType,
  commentaire: ''
});

const typesDechetList: DechetType[] = ['Bavure', 'Poudre'];
const alloysList = ['M3010L0', 'M3000L0', 'M3060L0'];

const loadDechets = async () => {
  isLoading.value = true;
  try {
    dechets.value = await dechetsService.getDechets();
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const handleAddDechet = async () => {
  if (isReadOnly.value) return;
  if (!formDechet.value.dateDeclaration || formDechet.value.quantiteKg <= 0) {
    await dialog.alert('Veuillez renseigner une date valide et un poids supérieur à 0 kg.', {
      title: 'Saisie incomplète',
      tone: 'warning'
    });
    return;
  }

  try {
    const payload = {
      dateDeclaration: formDechet.value.dateDeclaration,
      semaine: formDechet.value.semaine,
      codeTypeMatiere: formDechet.value.codeTypeMatiere,
      quantiteKg: formDechet.value.quantiteKg,
      unite: 'KG' as const,
      typeDechet: formDechet.value.typeDechet,
      commentaire: canUseComments.value ? formDechet.value.commentaire || undefined : undefined
    };

    await dechetsService.createDechet(payload);
    formDechet.value.quantiteKg = 0;
    formDechet.value.commentaire = '';
    successMessage.value = 'Déclaration de déchet enregistrée avec succès !';
    await loadDechets();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const handleValiderDechet = async (id: number) => {
  try {
    await dechetsService.updateDechet(id, { statut: 'Valide' });
    successMessage.value = 'Déclaration de déchet validée !';
    await loadDechets();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const handleDeleteDechet = async (id: number) => {
  if (
    await dialog.confirm('Voulez-vous supprimer cette déclaration de déchet ?', {
      title: 'Supprimer la déclaration',
      tone: 'danger',
      confirmLabel: 'Supprimer'
    })
  ) {
    try {
      await dechetsService.deleteDechet(id);
      successMessage.value = 'Déclaration supprimée.';
      await loadDechets();
      setTimeout(() => (successMessage.value = ''), 4000);
    } catch (err) {
      console.error(err);
    }
  }
};

onMounted(() => {
  loadDechets();
});
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <PageHeader
      title="Déchets Fonderie"
      description="Déclarer et suivre les bavures, poudres et autres déchets de la fonderie."
    />

    <!-- Notifications -->
    <div
      v-if="successMessage"
      class="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-emerald-800 text-xs font-semibold flex items-center gap-3"
    >
      <CheckCircle2 class="w-5 h-5 text-emerald-500 shrink-0" />
      <span>{{ successMessage }}</span>
    </div>

    <!-- Saisie form only for Admin & Responsable Fonderie -->
    <div v-if="!isReadOnly" class="bg-white rounded-xl border border-slate-100 shadow-sm p-6 space-y-6">
      <div class="flex items-center gap-2 border-b border-slate-50 pb-3">
        <Flame class="w-4.5 h-4.5 text-blue-900" />
        <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">Saisie d'un Nouveau Déchet de Coulée</h3>
      </div>

      <form
        @submit.prevent="handleAddDechet"
        class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 bg-slate-50/50 border border-slate-100 p-4 rounded-xl"
      >
        <FormField id="d-date" label="Date déclaration" hint="Jour de constat du déchet." required>
          <input
            type="date"
            v-model="formDechet.dateDeclaration"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white"
            required
          />
        </FormField>

        <FormField id="d-sem" label="Semaine" hint="Numéro entre 1 et 53." required>
          <input
            type="number"
            v-model="formDechet.semaine"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono"
            min="1"
            max="53"
            placeholder="26"
            required
          />
        </FormField>

        <FormField id="d-mat" label="Type Matière" hint="Matière concernée." required>
          <select
            v-model="formDechet.codeTypeMatiere"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white cursor-pointer"
            required
          >
            <option v-for="alloy in alloysList" :key="alloy" :value="alloy">{{ alloy }}</option>
          </select>
        </FormField>

        <FormField id="d-qte" label="Poids déchet (kg)" hint="Poids réel perdu." required>
          <input
            type="number"
            v-model="formDechet.quantiteKg"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-bold w-full focus:bg-white font-mono"
            min="1"
            step="0.5"
            placeholder="0.00"
            required
          />
        </FormField>

        <FormField id="d-type-dechet" label="Type de déchet" hint="Choisir Bavure ou Poudre." required>
          <select
            v-model="formDechet.typeDechet"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white cursor-pointer"
            required
          >
            <option v-for="td in typesDechetList" :key="td" :value="td">{{ td }}</option>
          </select>
        </FormField>

        <FormField v-if="canUseComments" id="d-comment" label="Observation">
          <input
            type="text"
            v-model="formDechet.commentaire"
            class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold w-full focus:bg-white"
            placeholder="Ex. bavure coulée, poudre récupérée..."
          />
        </FormField>

        <div class="flex items-end">
          <button
            type="submit"
            class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-lg flex items-center justify-center gap-1.5 cursor-pointer active:scale-95 transition-all"
          >
            <Plus class="w-4 h-4" />
            Déclarer Déchet
          </button>
        </div>
      </form>
    </div>

    <!-- Declarations Table -->
    <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col">
      <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
        <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider">
          Registre Historique des Déchets Fonderie
        </h3>
        <span class="text-[10px] font-bold text-slate-400 font-mono">Déclarations : {{ dechets.length }}</span>
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
              <th class="p-3">Date de Saisie</th>
              <th class="p-3">Semaine</th>
              <th class="p-3">Alliage</th>
              <th class="p-3 text-right">Poids Perdu (kg)</th>
              <th class="p-3">Type Déchet</th>
              <th v-if="canUseComments" class="p-3">Commentaires</th>
              <th class="p-3">Déclarant</th>
              <th class="p-3">Statut</th>
              <th class="p-3 text-right" v-if="!isReadOnly">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 text-xs font-medium">
            <tr v-for="row in dechets" :key="row.id" class="hover:bg-slate-50/30 transition-colors">
              <td class="p-3 font-mono text-slate-600">{{ row.dateDeclaration }}</td>
              <td class="p-3 font-mono text-slate-500">S-{{ row.semaine }}</td>
              <td class="p-3 font-bold font-mono text-blue-900">{{ row.codeTypeMatiere }}</td>
              <td class="p-3 text-right font-black font-mono text-rose-600">
                {{ formatNumber(row.quantiteKg, 1) }} kg
              </td>
              <td class="p-3">
                <span
                  class="px-2 py-1 rounded text-[10px] font-extrabold bg-blue-50 text-blue-800 border border-blue-100 uppercase tracking-wider"
                  >{{ row.typeDechet || 'Non défini' }}</span
                >
              </td>
              <td
                v-if="canUseComments"
                class="p-3 text-slate-500 text-[11px] leading-relaxed max-w-xs truncate"
                :title="row.commentaire"
              >
                {{ row.commentaire || 'Aucun détail' }}
              </td>
              <td class="p-3 text-slate-500 text-[10px] font-bold uppercase tracking-wider">{{ row.utilisateur }}</td>
              <td class="p-3">
                <BadgeStatus :status="row.statut" />
              </td>
              <td class="p-3 text-right" v-if="!isReadOnly">
                <div class="inline-flex gap-2">
                  <button
                    v-if="row.statut === 'Brouillon'"
                    @click="handleValiderDechet(row.id)"
                    class="px-2 py-0.5 bg-emerald-50 border border-emerald-100 text-emerald-700 hover:bg-emerald-100 rounded text-[10px] font-extrabold cursor-pointer"
                  >
                    Valider
                  </button>
                  <button
                    @click="handleDeleteDechet(row.id)"
                    class="p-1 hover:bg-rose-50 rounded text-slate-400 hover:text-rose-600 cursor-pointer"
                  >
                    <Trash2 class="w-3.5 h-3.5" />
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="!dechets.length">
              <td
                :colspan="canUseComments ? 10 : 9"
                class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider"
              >
                Aucune déclaration enregistrée
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
