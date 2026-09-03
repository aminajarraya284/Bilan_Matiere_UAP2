<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { AlertTriangle, Layers3, RefreshCw } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import FormField from '@/components/ui/FormField.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import { bilanService } from './bilan.service';
import type { BilanUap2ApiLigne, BilanUap2ApiResponse } from '@/types/api.types';
import { formatNumber } from '@/utils/formatters';
import { BUSINESS_RULES } from '@/config/businessRules';

const periode = ref(BUSINESS_RULES.periodeParDefaut.slice(0, 7));
const bilan = ref<BilanUap2ApiResponse | null>(null);
const isLoading = ref(true);
const isLoadingPeriodes = ref(true);
const loadError = ref('');
const periodesError = ref('');

const lignes = computed<BilanUap2ApiLigne[]>(() => bilan.value?.lignes ?? []);

const charger = async (periodeACharger = periode.value) => {
  isLoading.value = true;
  loadError.value = '';
  try {
    bilan.value = await bilanService.getBilanUap2Reel(periodeACharger);
  } catch {
    loadError.value = 'Impossible de charger le bilan UAP2.';
    bilan.value = null;
  } finally {
    isLoading.value = false;
  }
};

const initialiser = async () => {
  try {
    const periodesDisponibles = await bilanService.getPeriodesUap2Disponibles();
    if (periodesDisponibles.length > 0) {
      periode.value = periodesDisponibles[0];
      await charger(periode.value);
    }
  } catch {
    periodesError.value = 'Impossible de déterminer la dernière période disponible.';
  } finally {
    isLoadingPeriodes.value = false;
    if (!bilan.value) isLoading.value = false;
  }
};

/** Le seuil s'apprécie en valeur absolue : un excédent alerte autant qu'un manquant. */
const horsSeuil = (ligne: BilanUap2ApiLigne): boolean =>
  ligne.tauxEcartPourcent !== null &&
  bilan.value !== null &&
  Math.abs(ligne.tauxEcartPourcent) > bilan.value.seuilEcartPourcent;

onMounted(initialiser);
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Bilan UAP2 par OF"
      description="Sortie matière en kilogrammes et écart en pièces, par ordre de fabrication."
    />

    <div class="rounded-xl border border-slate-200 bg-white p-4 flex flex-wrap items-end gap-4 shadow-sm">
      <FormField id="uap2-periode" label="Période" required>
        <input
          v-model="periode"
          @change="charger(periode)"
          type="month"
          class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold"
          required
        />
      </FormField>
      <p v-if="bilan" class="pb-2 text-xs text-slate-500">
        Seuil d'alerte appliqué : {{ formatNumber(bilan.seuilEcartPourcent, 2) }} %
      </p>
      <button
        type="button"
        class="mb-0.5 inline-flex items-center gap-2 rounded-lg bg-blue-900 px-4 py-2 text-xs font-bold text-white hover:bg-blue-800 disabled:cursor-wait disabled:opacity-60"
        :disabled="isLoading"
        @click="charger(periode)"
      >
        <RefreshCw class="h-4 w-4" :class="{ 'animate-spin': isLoading }" />
        Actualiser
      </button>
    </div>

    <LoadingState v-if="isLoadingPeriodes || isLoading" />

    <div v-else-if="periodesError" class="rounded-xl border border-rose-100 bg-rose-50 p-4 text-xs font-semibold text-rose-700">
      {{ periodesError }}
    </div>

    <div v-else-if="bilan && lignes.length === 0" class="rounded-xl border border-slate-200 bg-white p-6 text-center text-sm font-semibold text-slate-500 shadow-sm">
      Aucune donnée UAP2 pour la période sélectionnée.
    </div>

    <div
      v-else-if="loadError"
      class="rounded-xl border border-rose-100 bg-rose-50 p-4 text-xs font-semibold text-rose-700"
    >
      {{ loadError }}
    </div>

    <div
      v-else-if="!bilan"
      class="rounded-xl border border-slate-200 bg-white p-6 text-center text-sm font-semibold text-slate-500 shadow-sm"
    >
      Aucune donnée UAP2 disponible.
    </div>

    <template v-else-if="bilan">
      <div class="grid grid-cols-1 gap-4 md:grid-cols-3 xl:grid-cols-6">
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">OF suivis</h3>
          <p class="mt-2 text-2xl font-black text-slate-900">{{ bilan.nombreLignes }}</p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">Total CTRL</h3>
          <p class="mt-2 text-2xl font-black font-mono text-slate-900">
            {{ formatNumber(bilan.totalQuantiteCtrlPieces, 0) }}
            <span class="text-sm font-bold text-slate-400">pièces</span>
          </p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">Écart UAP2 global</h3>
          <p class="mt-2 text-2xl font-black font-mono text-slate-900">
            {{ formatNumber(bilan.totalEcartUap2Pieces, 0) }}
            <span class="text-sm font-bold text-slate-400">pièces</span>
          </p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">Sortie MP</h3>
          <p class="mt-2 text-2xl font-black font-mono text-slate-900">
            <span v-if="bilan.totalSortieMpKg !== null">
              {{ formatNumber(bilan.totalSortieMpKg, 0) }} <span class="text-sm font-bold text-slate-400">kg</span>
            </span>
            <span v-else class="text-base font-bold text-slate-400">Indisponible</span>
          </p>
          <p v-if="bilan.totalSortieMpKg === null" class="mt-1 text-xs text-slate-500">
            Au moins un poids physique n'est pas résolu.
          </p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">Lignes hors seuil</h3>
          <p
            class="mt-2 text-2xl font-black"
            :class="bilan.nombreLignesHorsSeuil > 0 ? 'text-rose-600' : 'text-slate-900'"
          >
            {{ bilan.nombreLignesHorsSeuil }}
          </p>
        </div>
        <div class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">Anomalies</h3>
          <p class="mt-2 text-2xl font-black" :class="bilan.nombreAnomalies > 0 ? 'text-amber-600' : 'text-slate-900'">
            {{ bilan.nombreAnomalies }}
          </p>
          <p class="mt-1 text-xs text-slate-500">{{ bilan.nombreLignesDonneesManquantes }} ligne(s) incomplète(s)</p>
        </div>
      </div>

      <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
        <div class="flex items-center gap-2 border-b border-slate-100 pb-3 text-slate-700">
          <Layers3 class="h-4 w-4" />
          <h3 class="text-xs font-black uppercase tracking-wider">Détail par ordre de fabrication</h3>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full border-collapse text-left">
            <thead>
              <tr
                class="border-b border-slate-100 bg-slate-50/50 text-[10px] font-bold uppercase tracking-wider text-slate-500"
              >
                <th class="p-3">OF</th>
                <th class="p-3">Article</th>
                <th class="p-3 text-right">CTRL (pcs)</th>
                <th class="p-3 text-right">Rebut SCI (pcs)</th>
                <th class="p-3 text-right">Rebut EMR (pcs)</th>
                <th class="p-3 text-right">Rebut TRN (pcs)</th>
                <th class="p-3 text-right">Sortie MP (pcs)</th>
                <th class="p-3 text-right">Poids phys. (kg/pc)</th>
                <th class="p-3 text-right text-blue-900">Sortie MP (kg)</th>
                <th class="p-3 text-right">Entrée OF (pcs)</th>
                <th class="p-3 text-right">Rebut U2 (pcs)</th>
                <th class="p-3 text-right text-rose-900">Écart (pcs)</th>
                <th class="p-3 text-right">Taux</th>
                <th class="p-3">Statut</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="ligne in lignes" :key="`${ligne.numeroOF}|${ligne.codeArticle}`">
                <td class="p-3 font-mono font-bold text-slate-800">{{ ligne.numeroOF }}</td>
                <td class="p-3 font-mono text-slate-600">{{ ligne.codeArticle }}</td>
                <td class="p-3 text-right font-black font-mono">{{ formatNumber(ligne.quantiteCtrlPieces, 0) }}</td>
                <td class="p-3 text-right font-mono text-slate-600">{{ formatNumber(ligne.rebutSciPieces, 0) }}</td>
                <td class="p-3 text-right font-mono text-slate-600">{{ formatNumber(ligne.rebutEmrPieces, 0) }}</td>
                <td class="p-3 text-right font-mono text-slate-600">{{ formatNumber(ligne.rebutTrnPieces, 0) }}</td>
                <td class="p-3 text-right font-semibold font-mono">
                  {{ formatNumber(ligne.quantiteSortieMpPieces, 0) }}
                </td>
                <td class="p-3 text-right font-mono text-slate-500">
                  <span v-if="ligne.poidsPhysiqueUnitaireKg !== null">
                    {{ formatNumber(ligne.poidsPhysiqueUnitaireKg, 3) }}
                  </span>
                  <span v-else class="text-slate-400">—</span>
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-900">
                  <span v-if="ligne.sortieMpKg !== null">{{ formatNumber(ligne.sortieMpKg, 1) }}</span>
                  <span v-else class="text-slate-400">—</span>
                </td>
                <td class="p-3 text-right font-mono">{{ formatNumber(ligne.entreeOfPieces, 0) }}</td>
                <td class="p-3 text-right font-mono">{{ formatNumber(ligne.rebutUsinage2Pieces, 0) }}</td>
                <td
                  class="p-3 text-right font-black font-mono"
                  :class="horsSeuil(ligne) ? 'text-rose-600' : 'text-slate-700'"
                >
                  {{ formatNumber(ligne.ecartUap2Pieces, 0) }}
                </td>
                <td class="p-3 text-right font-bold font-mono" :class="horsSeuil(ligne) ? 'text-rose-600' : ''">
                  <span v-if="ligne.tauxEcartPourcent !== null">
                    {{ formatNumber(ligne.tauxEcartPourcent, 2) }} %
                  </span>
                  <span v-else class="text-slate-400">—</span>
                </td>
                <td class="p-3">
                  <div class="flex flex-col gap-1">
                    <BadgeStatus :status="ligne.statut" />
                    <span
                      v-for="anomalie in ligne.anomalies"
                      :key="anomalie"
                      class="inline-flex items-center gap-1 rounded-full border border-amber-100 bg-amber-50 px-2 py-0.5 text-[10px] font-bold text-amber-700"
                    >
                      <AlertTriangle class="h-3 w-3" />
                      {{ anomalie }}
                    </span>
                  </div>
                </td>
              </tr>
              <tr v-if="lignes.length === 0">
                <td colspan="14" class="p-6 text-center font-bold uppercase tracking-wider text-slate-400">
                  Aucune donnée pour cette période
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <p class="text-xs text-slate-500">
          L'écart est exprimé <strong>en pièces</strong> : aucune conversion n'y intervient. Seule la sortie MP est
          convertie en kilogrammes, via le poids physique du Contrôle poids Fonderie.
        </p>
      </div>
    </template>
  </div>
</template>
