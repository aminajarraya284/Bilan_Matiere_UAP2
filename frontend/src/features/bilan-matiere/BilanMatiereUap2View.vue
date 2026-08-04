<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { AlertTriangle, Layers3 } from 'lucide-vue-next';
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
const loadError = ref('');

const lignes = computed<BilanUap2ApiLigne[]>(() => bilan.value?.lignes ?? []);

const charger = async () => {
  isLoading.value = true;
  loadError.value = '';
  try {
    bilan.value = await bilanService.getBilanUap2Reel(periode.value);
  } catch (error) {
    loadError.value = error instanceof Error ? error.message : 'Chargement du Bilan UAP2 impossible.';
    bilan.value = null;
  } finally {
    isLoading.value = false;
  }
};

/** Le seuil s'apprécie en valeur absolue : un excédent alerte autant qu'un manquant. */
const horsSeuil = (ligne: BilanUap2ApiLigne): boolean =>
  ligne.tauxEcartPourcent !== null &&
  bilan.value !== null &&
  Math.abs(ligne.tauxEcartPourcent) > bilan.value.seuilEcartPourcent;

onMounted(charger);
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
          @change="charger"
          type="month"
          class="bg-white border border-slate-200 rounded-lg p-2 text-xs font-semibold"
          required
        />
      </FormField>
      <p v-if="bilan" class="pb-2 text-xs text-slate-500">
        Seuil d'alerte appliqué : {{ formatNumber(bilan.seuilEcartPourcent, 2) }} %
      </p>
    </div>

    <LoadingState v-if="isLoading" />

    <div
      v-else-if="loadError"
      class="rounded-xl border border-rose-100 bg-rose-50 p-4 text-xs font-semibold text-rose-700"
    >
      {{ loadError }}
    </div>

    <template v-else-if="bilan">
      <div class="grid grid-cols-1 gap-4 md:grid-cols-4">
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
                <th class="p-3 text-right">Rebuts SCI/EMR/TRN (pcs)</th>
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
                <td class="p-3 text-right font-mono text-slate-600">
                  {{ formatNumber(ligne.rebutSciPieces + ligne.rebutEmrPieces + ligne.rebutTrnPieces, 0) }}
                </td>
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
                <td colspan="12" class="p-6 text-center font-bold uppercase tracking-wider text-slate-400">
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
