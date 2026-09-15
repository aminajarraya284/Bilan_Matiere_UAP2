<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import axios from 'axios';
import { CheckCircle2, Flame, Loader2, Pencil, Plus, Trash2, X } from 'lucide-vue-next';
import { useAuthStore } from '@/features/auth/auth.store';
import { useAppDialog } from '@/composables/useAppDialog';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import FormField from '@/components/ui/FormField.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import PageHeader from '@/components/ui/PageHeader.vue';
import { dechetsService } from './dechets.service';
import type {
  DechetFonderiePayload,
  DechetFonderieResponse,
  DechetType,
  ResumeDechetsFonderieResponse
} from './dechets.types';
import type { TypeMatiereResponse } from './typesMatiere.types';
import { formatWeight, getCurrentPeriod, getIsoWeek, getLocalIsoDate } from './dechets.utils';

type FieldErrors = Partial<Record<keyof DechetFonderiePayload, string>>;
type ProblemDetails = { title?: string; detail?: string; errors?: Record<string, string[]> };

const authStore = useAuthStore();
const dialog = useAppDialog();
const dechets = ref<DechetFonderieResponse[]>([]);
const typesMatiere = ref<TypeMatiereResponse[]>([]);
const resume = ref<ResumeDechetsFonderieResponse | null>(null);
const total = ref(0);
const page = ref(1);
const pageSize = 20;
const isLoading = ref(true);
const isLoadingTypes = ref(true);
const isSubmitting = ref(false);
const deletingId = ref<string | null>(null);
const editingId = ref<string | null>(null);
const successMessage = ref('');
const typesError = ref('');
const dataError = ref('');
const fieldErrors = ref<FieldErrors>({});
let filterTimer: ReturnType<typeof setTimeout> | undefined;
let messageTimer: ReturnType<typeof setTimeout> | undefined;

const filters = ref({
  periode: getCurrentPeriod(),
  typeMatiereId: '',
  typeDechet: '' as '' | DechetType
});

const formDechet = ref({
  dateDeclaration: getLocalIsoDate(),
  typeMatiereId: '',
  typeDechet: 'Bavure' as DechetType,
  poidsKg: null as number | null,
  observation: ''
});

const typesDechetList: DechetType[] = ['Bavure', 'Poudre'];
const semaineIso = computed(() => getIsoWeek(formDechet.value.dateDeclaration));
const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize)));
const isReadOnly = computed(() => authStore.userRole !== 'Admin' && authStore.userRole !== 'ResponsableFonderie');
const hasNoActiveTypes = computed(() => !isLoadingTypes.value && !typesError.value && typesMatiere.value.length === 0);

const showSuccess = (message: string) => {
  if (messageTimer) clearTimeout(messageTimer);
  successMessage.value = message;
  messageTimer = setTimeout(() => (successMessage.value = ''), 4000);
};

const getApiErrorMessage = (error: unknown): string => {
  if (!axios.isAxiosError<ProblemDetails>(error)) {
    return error instanceof Error ? error.message : 'Une erreur inattendue est survenue.';
  }

  const problem = error.response?.data;
  return problem?.detail || problem?.title || 'La requête a échoué.';
};

const readApiError = (error: unknown) => {
  fieldErrors.value = {};
  if (axios.isAxiosError<ProblemDetails>(error)) {
    const problem = error.response?.data;
    Object.entries(problem?.errors ?? {}).forEach(([property, messages]) => {
      if (property in formDechet.value) {
        fieldErrors.value[property as keyof DechetFonderiePayload] = messages[0];
      }
    });
  }
  dataError.value = getApiErrorMessage(error);
};

const loadTypesMatiere = async () => {
  isLoadingTypes.value = true;
  typesError.value = '';
  try {
    typesMatiere.value = (await dechetsService.getTypesMatiere()).filter((item) => item.estActif);
    if (!typesMatiere.value.some((item) => item.id === formDechet.value.typeMatiereId)) {
      formDechet.value.typeMatiereId = typesMatiere.value[0]?.id ?? '';
    }
  } catch (error) {
    typesMatiere.value = [];
    formDechet.value.typeMatiereId = '';
    typesError.value = `Impossible de charger les types matière. ${getApiErrorMessage(error)}`;
  } finally {
    isLoadingTypes.value = false;
  }
};

const loadData = async () => {
  isLoading.value = true;
  dataError.value = '';
  try {
    const [listResponse, summaryResponse] = await Promise.all([
      dechetsService.getDechetsFonderie({
        periode: filters.value.periode,
        typeMatiereId: filters.value.typeMatiereId || undefined,
        typeDechet: filters.value.typeDechet || undefined,
        page: page.value,
        pageSize
      }),
      dechetsService.getResumeDechetsFonderie(filters.value.periode)
    ]);
    dechets.value = listResponse.items;
    total.value = listResponse.total;
    resume.value = summaryResponse;
  } catch (error) {
    dechets.value = [];
    total.value = 0;
    resume.value = null;
    readApiError(error);
  } finally {
    isLoading.value = false;
  }
};

const validateForm = (): boolean => {
  const errors: FieldErrors = {};
  if (!formDechet.value.dateDeclaration) errors.dateDeclaration = 'La date est obligatoire.';
  if (!formDechet.value.typeMatiereId) errors.typeMatiereId = 'Le type matière est obligatoire.';
  if (formDechet.value.poidsKg === null || formDechet.value.poidsKg <= 0) {
    errors.poidsKg = 'Le poids doit être strictement supérieur à zéro.';
  }
  if (formDechet.value.observation.length > 500) {
    errors.observation = 'L’observation ne peut pas dépasser 500 caractères.';
  }
  fieldErrors.value = errors;
  return Object.keys(errors).length === 0;
};

const resetEditableFields = () => {
  formDechet.value.poidsKg = null;
  formDechet.value.observation = '';
  editingId.value = null;
  fieldErrors.value = {};
};

const handleSubmit = async () => {
  if (isReadOnly.value || isSubmitting.value || !validateForm()) return;
  isSubmitting.value = true;
  dataError.value = '';
  const payload: DechetFonderiePayload = {
    dateDeclaration: formDechet.value.dateDeclaration,
    typeMatiereId: formDechet.value.typeMatiereId,
    typeDechet: formDechet.value.typeDechet,
    poidsKg: formDechet.value.poidsKg!,
    observation: formDechet.value.observation.trim() || null,
    // Cet ecran ne declare que des dechets Fonderie : l'origine est constante et
    // envoyee explicitement plutot que laissee au defaut du serveur.
    origine: 'Fonderie'
  };

  try {
    if (editingId.value) {
      await dechetsService.updateDechetFonderie(editingId.value, payload);
      showSuccess('Déclaration de déchet modifiée avec succès.');
    } else {
      await dechetsService.createDechetFonderie(payload);
      showSuccess('Déclaration de déchet enregistrée avec succès.');
    }
    resetEditableFields();
    await loadData();
  } catch (error) {
    readApiError(error);
  } finally {
    isSubmitting.value = false;
  }
};

const startEdit = (row: DechetFonderieResponse) => {
  editingId.value = row.id;
  formDechet.value = {
    dateDeclaration: row.dateDeclaration,
    typeMatiereId: row.typeMatiereId,
    typeDechet: row.typeDechet,
    poidsKg: row.poidsKg,
    observation: row.observation ?? ''
  };
  fieldErrors.value = {};
  window.scrollTo({ top: 0, behavior: 'smooth' });
};

const handleDelete = async (row: DechetFonderieResponse) => {
  if (deletingId.value) return;
  const confirmed = await dialog.confirm(
    `Supprimer logiquement la déclaration du ${row.dateDeclaration} (${formatWeight(row.poidsKg)} kg) ?`,
    { title: 'Supprimer la déclaration', tone: 'danger', confirmLabel: 'Supprimer' }
  );
  if (!confirmed) return;

  deletingId.value = row.id;
  try {
    await dechetsService.deleteDechetFonderie(row.id);
    showSuccess('Déclaration supprimée. Elle ne participe plus au résumé.');
    if (editingId.value === row.id) resetEditableFields();
    await loadData();
  } catch (error) {
    readApiError(error);
  } finally {
    deletingId.value = null;
  }
};

const changePage = async (nextPage: number) => {
  if (nextPage < 1 || nextPage > totalPages.value || nextPage === page.value) return;
  page.value = nextPage;
  await loadData();
};

watch(
  filters,
  () => {
    page.value = 1;
    if (filterTimer) clearTimeout(filterTimer);
    filterTimer = setTimeout(loadData, 250);
  },
  { deep: true }
);

onMounted(async () => {
  await loadTypesMatiere();
  await loadData();
});

onBeforeUnmount(() => {
  if (filterTimer) clearTimeout(filterTimer);
  if (messageTimer) clearTimeout(messageTimer);
});
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Déchets Fonderie"
      description="Déclarer et suivre les bavures et poudres de la fonderie par type matière."
    />

    <div
      v-if="successMessage"
      data-testid="success-message"
      class="flex items-center gap-3 rounded-xl border border-emerald-100 bg-emerald-50 p-4 text-xs font-semibold text-emerald-800"
    >
      <CheckCircle2 class="h-5 w-5 shrink-0 text-emerald-500" />
      <span>{{ successMessage }}</span>
    </div>
    <div
      v-if="typesError"
      data-testid="types-error"
      role="alert"
      class="flex items-center justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 p-4 text-xs font-semibold text-rose-700"
    >
      <span>{{ typesError }}</span>
      <button
        type="button"
        data-testid="retry-types"
        class="shrink-0 rounded-lg border border-rose-300 bg-white px-3 py-2 font-bold text-rose-700 hover:bg-rose-100"
        @click="loadTypesMatiere"
      >
        Réessayer
      </button>
    </div>
    <div
      v-else-if="hasNoActiveTypes"
      data-testid="types-empty"
      role="status"
      class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-xs font-semibold text-amber-800"
    >
      Aucun type matière actif disponible
    </div>
    <div
      v-if="dataError"
      data-testid="api-error"
      role="alert"
      class="rounded-xl border border-rose-200 bg-rose-50 p-4 text-xs font-semibold text-rose-700"
    >
      {{ dataError }}
    </div>

    <section v-if="resume" aria-label="Résumé des déchets" class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <article
        v-for="item in resume.totauxParTypeMatiere"
        :key="item.typeMatiereId"
        class="rounded-xl border border-slate-100 bg-white p-4 shadow-sm"
      >
        <p class="font-mono text-xs font-black text-blue-900">{{ item.typeMatiereCode }}</p>
        <p class="mt-2 text-xl font-black text-slate-800">{{ formatWeight(item.totalKg) }} kg</p>
        <p class="mt-1 text-[10px] font-semibold text-slate-500">
          Bavure {{ formatWeight(item.totalBavureKg) }} · Poudre {{ formatWeight(item.totalPoudreKg) }}
        </p>
      </article>
      <article class="rounded-xl border border-blue-100 bg-blue-50 p-4 shadow-sm">
        <p class="text-xs font-black uppercase tracking-wider text-blue-800">Total général</p>
        <p data-testid="summary-total" class="mt-2 text-xl font-black text-blue-950">
          {{ formatWeight(resume.totalGeneralKg) }} kg
        </p>
        <p class="mt-1 text-[10px] font-semibold text-blue-700">Période {{ resume.periode }}</p>
      </article>
    </section>

    <section v-if="!isReadOnly" class="space-y-6 rounded-xl border border-slate-100 bg-white p-6 shadow-sm">
      <div class="flex items-center justify-between gap-3 border-b border-slate-50 pb-3">
        <div class="flex items-center gap-2">
          <Flame class="h-4.5 w-4.5 text-blue-900" />
          <h2 class="text-xs font-black uppercase tracking-wider text-slate-700">
            {{ editingId ? 'Modifier le déchet' : 'Saisir un nouveau déchet' }}
          </h2>
        </div>
        <button
          v-if="editingId"
          type="button"
          class="inline-flex items-center gap-1 text-xs font-bold text-slate-500"
          @click="resetEditableFields"
        >
          <X class="h-4 w-4" /> Annuler la modification
        </button>
      </div>

      <form
        data-testid="waste-form"
        class="grid grid-cols-1 gap-4 rounded-xl border border-slate-100 bg-slate-50/50 p-4 sm:grid-cols-2 lg:grid-cols-4"
        @submit.prevent="handleSubmit"
      >
        <FormField id="d-date" label="Date déclaration" :error="fieldErrors.dateDeclaration" required>
          <input
            id="d-date"
            v-model="formDechet.dateDeclaration"
            data-testid="date-input"
            type="date"
            required
            class="w-full rounded-lg border border-slate-200 bg-white p-2 text-xs font-semibold"
          />
        </FormField>

        <FormField id="d-week" label="Semaine calculée" automatic>
          <input
            id="d-week"
            :value="semaineIso ? `S-${semaineIso}` : ''"
            data-testid="week-input"
            type="text"
            readonly
            class="w-full rounded-lg border border-slate-200 bg-slate-100 p-2 font-mono text-xs font-bold"
          />
        </FormField>

        <FormField id="d-material" label="Type matière" :error="fieldErrors.typeMatiereId" required>
          <select
            id="d-material"
            v-model="formDechet.typeMatiereId"
            data-testid="material-select"
            :disabled="isLoadingTypes"
            required
            class="w-full rounded-lg border border-slate-200 bg-white p-2 text-xs font-semibold"
          >
            <option disabled value="">Sélectionner</option>
            <option v-for="material in typesMatiere" :key="material.id" :value="material.id">
              {{ material.code }} — {{ material.libelle }}
            </option>
          </select>
        </FormField>

        <FormField id="d-weight" label="Poids déchet (kg)" :error="fieldErrors.poidsKg" required>
          <input
            id="d-weight"
            v-model.number="formDechet.poidsKg"
            data-testid="weight-input"
            type="number"
            min="0.001"
            step="0.001"
            required
            class="w-full rounded-lg border border-slate-200 bg-white p-2 font-mono text-xs font-bold"
            placeholder="0,000"
          />
        </FormField>

        <FormField id="d-kind" label="Type de déchet" :error="fieldErrors.typeDechet" required>
          <select
            id="d-kind"
            v-model="formDechet.typeDechet"
            data-testid="kind-select"
            required
            class="w-full rounded-lg border border-slate-200 bg-white p-2 text-xs font-semibold"
          >
            <option v-for="kind in typesDechetList" :key="kind" :value="kind">{{ kind }}</option>
          </select>
        </FormField>

        <FormField id="d-observation" label="Observation" :error="fieldErrors.observation">
          <textarea
            id="d-observation"
            v-model="formDechet.observation"
            data-testid="observation-input"
            maxlength="500"
            rows="2"
            class="w-full resize-none rounded-lg border border-slate-200 bg-white p-2 text-xs font-semibold"
            placeholder="Ex. bavures triées…"
          ></textarea>
        </FormField>

        <div class="flex items-end">
          <button
            data-testid="submit-waste"
            type="submit"
            :disabled="isSubmitting || isLoadingTypes || !typesMatiere.length"
            class="flex w-full items-center justify-center gap-1.5 rounded-lg bg-blue-900 px-4 py-2.5 text-xs font-bold uppercase tracking-wider text-white disabled:cursor-not-allowed disabled:opacity-60"
          >
            <Loader2 v-if="isSubmitting" class="h-4 w-4 animate-spin" />
            <Pencil v-else-if="editingId" class="h-4 w-4" />
            <Plus v-else class="h-4 w-4" />
            {{ editingId ? 'Enregistrer' : 'Déclarer le déchet' }}
          </button>
        </div>
      </form>
    </section>

    <section class="overflow-hidden rounded-xl border border-slate-100 bg-white shadow-sm">
      <div class="grid grid-cols-1 gap-3 border-b border-slate-100 bg-slate-50/30 p-4 sm:grid-cols-3">
        <FormField id="filter-period" label="Période">
          <input
            id="filter-period"
            v-model="filters.periode"
            data-testid="period-filter"
            type="month"
            class="w-full rounded-lg border border-slate-200 bg-white p-2 text-xs font-semibold"
          />
        </FormField>
        <FormField id="filter-material" label="Type matière">
          <select
            id="filter-material"
            v-model="filters.typeMatiereId"
            data-testid="material-filter"
            class="w-full rounded-lg border border-slate-200 bg-white p-2 text-xs font-semibold"
          >
            <option value="">Tous</option>
            <option v-for="material in typesMatiere" :key="material.id" :value="material.id">
              {{ material.code }}
            </option>
          </select>
        </FormField>
        <FormField id="filter-kind" label="Type de déchet">
          <select
            id="filter-kind"
            v-model="filters.typeDechet"
            data-testid="kind-filter"
            class="w-full rounded-lg border border-slate-200 bg-white p-2 text-xs font-semibold"
          >
            <option value="">Tous</option>
            <option v-for="kind in typesDechetList" :key="kind" :value="kind">{{ kind }}</option>
          </select>
        </FormField>
      </div>

      <div class="flex items-center justify-between border-b border-slate-50 px-6 py-4">
        <h2 class="text-xs font-black uppercase tracking-wider text-slate-700">Historique des déchets Fonderie</h2>
        <span data-testid="waste-total" class="font-mono text-[10px] font-bold text-slate-400">
          {{ total }} déclaration(s)
        </span>
      </div>

      <LoadingState v-if="isLoading" />
      <div v-else class="overflow-x-auto">
        <table data-testid="waste-table" class="w-full border-collapse text-left">
          <thead>
            <tr
              class="border-b border-slate-100 bg-slate-50/50 text-[10px] font-bold uppercase tracking-wider text-slate-500"
            >
              <th class="p-3">Date de saisie</th>
              <th class="p-3">Semaine</th>
              <th class="p-3">Type matière</th>
              <th class="p-3 text-right">Poids perdu kg</th>
              <th class="p-3">Type déchet</th>
              <th class="p-3">Observation</th>
              <th class="p-3">Déclarant</th>
              <th class="p-3">Statut</th>
              <th v-if="!isReadOnly" class="p-3 text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 text-xs font-medium">
            <tr v-for="row in dechets" :key="row.id" data-testid="waste-row" class="hover:bg-slate-50/40">
              <td class="p-3 font-mono text-slate-600">{{ row.dateDeclaration }}</td>
              <td class="p-3 font-mono text-slate-500">S-{{ row.semaineIso }}</td>
              <td class="p-3 font-mono font-bold text-blue-900">{{ row.typeMatiereCode }}</td>
              <td class="p-3 text-right font-mono font-black text-rose-600">{{ formatWeight(row.poidsKg) }} kg</td>
              <td class="p-3">
                <span
                  class="rounded border border-blue-100 bg-blue-50 px-2 py-1 text-[10px] font-extrabold uppercase text-blue-800"
                  >{{ row.typeDechet }}</span
                >
              </td>
              <td class="max-w-xs truncate p-3 text-[11px] text-slate-500" :title="row.observation ?? ''">
                {{ row.observation || '—' }}
              </td>
              <td class="p-3 text-[10px] font-bold uppercase tracking-wider text-slate-500">
                {{ row.declarant || 'Non renseigné' }}
              </td>
              <td class="p-3"><BadgeStatus :status="row.statut" /></td>
              <td v-if="!isReadOnly" class="p-3 text-right">
                <div class="inline-flex gap-1">
                  <button
                    type="button"
                    :aria-label="`Modifier ${row.id}`"
                    data-testid="edit-waste"
                    class="rounded p-1.5 text-slate-400 hover:bg-blue-50 hover:text-blue-700"
                    @click="startEdit(row)"
                  >
                    <Pencil class="h-3.5 w-3.5" />
                  </button>
                  <button
                    type="button"
                    :aria-label="`Supprimer ${row.id}`"
                    data-testid="delete-waste"
                    :disabled="deletingId === row.id"
                    class="rounded p-1.5 text-slate-400 hover:bg-rose-50 hover:text-rose-600 disabled:opacity-50"
                    @click="handleDelete(row)"
                  >
                    <Loader2 v-if="deletingId === row.id" class="h-3.5 w-3.5 animate-spin" /><Trash2
                      v-else
                      class="h-3.5 w-3.5"
                    />
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="!dechets.length">
              <td
                :colspan="isReadOnly ? 8 : 9"
                class="p-8 text-center text-xs font-bold uppercase tracking-wider text-slate-400"
              >
                Aucune déclaration pour ces filtres
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div
        v-if="totalPages > 1"
        class="flex items-center justify-end gap-3 border-t border-slate-100 px-4 py-3 text-xs font-semibold text-slate-600"
      >
        <button
          type="button"
          :disabled="page === 1"
          class="rounded border border-slate-200 px-3 py-1.5 disabled:opacity-40"
          @click="changePage(page - 1)"
        >
          Précédent
        </button>
        <span>Page {{ page }} / {{ totalPages }}</span>
        <button
          type="button"
          :disabled="page === totalPages"
          class="rounded border border-slate-200 px-3 py-1.5 disabled:opacity-40"
          @click="changePage(page + 1)"
        >
          Suivant
        </button>
      </div>
    </section>
  </div>
</template>
