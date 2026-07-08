<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { AlertTriangle, Scale, Trash2 } from 'lucide-vue-next';
import { useAuthStore } from '@/features/auth/auth.store';
import { alertesService } from './alertes.service';
import { AlertesWeightAlert, AlertesBilanAlert, AlertesSummary } from './alertes.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import { formatNumber } from '@/utils/formatters';
import { BUSINESS_RULES } from '@/config/businessRules';

const authStore = useAuthStore();
const isAdmin = ref(authStore.userRole === 'Admin');
const isLoading = ref(true);
const weightAlerts = ref<AlertesWeightAlert[]>([]);
const bilanAlerts = ref<AlertesBilanAlert[]>([]);
const summary = ref<AlertesSummary | null>(null);
const toleranceAlertePoids = ref(BUSINESS_RULES.seuilAlertePoidsKg);

const loadAlerts = async () => {
  isLoading.value = true;
  try {
    weightAlerts.value = await alertesService.getWeightAlerts(toleranceAlertePoids.value);
    bilanAlerts.value = await alertesService.getBilanAlerts();
    summary.value = await alertesService.getSummary(toleranceAlertePoids.value);
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  if (!isAdmin.value) return;
  loadAlerts();
});
</script>

<template>
  <div class="space-y-6">
    <PageHeader title="Alertes Administrateur" description="Alertes poids et écarts." />

    <div v-if="!isAdmin" class="bg-rose-50 border border-rose-100 rounded-xl p-6 text-rose-900 font-semibold text-sm">
      <div class="font-black">Accès refusé</div>
      <p class="mt-2 text-slate-700 text-[13px]">Cette page est réservée aux administrateurs.</p>
    </div>

    <div v-else>
      <div v-if="isLoading">
        <LoadingState />
      </div>

      <div v-else class="space-y-6">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div class="rounded-3xl border border-slate-100 bg-slate-950 text-white p-6 shadow-sm">
            <div class="flex items-center justify-between gap-4">
              <div>
                <h2 class="text-xs uppercase tracking-[0.28em] text-slate-400 font-bold">Alertes Poids</h2>
                <div class="mt-3 text-3xl font-extrabold">{{ summary?.totalWeightAlerts }}</div>
              </div>
              <AlertTriangle class="w-7 h-7 text-rose-400" />
            </div>
            <p class="mt-4 text-sm text-slate-300">
              Quantités U2A sans conformité entre poids déclaré et poids physique.
            </p>
          </div>
          <div class="rounded-3xl border border-slate-100 bg-slate-950 text-white p-6 shadow-sm">
            <div class="flex items-center justify-between gap-4">
              <div>
                <h2 class="text-xs uppercase tracking-[0.28em] text-slate-400 font-bold">Alertes Écart Bilan</h2>
                <div class="mt-3 text-3xl font-extrabold">{{ summary?.totalBilanAlerts }}</div>
              </div>
              <Scale class="w-7 h-7 text-emerald-400" />
            </div>
            <p class="mt-4 text-sm text-slate-300">Écarts bilan matière supérieurs au seuil d'alerte.</p>
          </div>
          <div class="rounded-3xl border border-slate-100 bg-slate-950 text-white p-6 shadow-sm">
            <div class="flex items-center justify-between gap-4">
              <div>
                <h2 class="text-xs uppercase tracking-[0.28em] text-slate-400 font-bold">Perte totale</h2>
                <div class="mt-3 text-3xl font-extrabold">{{ formatNumber(summary?.totalLostKg || 0, 0) }} kg</div>
              </div>
              <Trash2 class="w-7 h-7 text-slate-300" />
            </div>
            <p class="mt-4 text-sm text-slate-300">Quantité perdue en déchets ce mois.</p>
          </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <div class="rounded-3xl border border-slate-100 bg-white p-6 shadow-sm">
            <div class="flex items-center justify-between mb-5">
              <div>
                <h3 class="text-sm font-bold text-slate-900">Alertes Poids U2A</h3>
                <p class="text-xs text-slate-500 mt-1">Non-conformités entre poids réceptionné et poids physique.</p>
              </div>
              <div class="text-[10px] uppercase tracking-[0.24em] text-slate-400">
                Tolérance {{ toleranceAlertePoids }} kg
              </div>
            </div>

            <div
              v-if="!weightAlerts.length"
              class="rounded-3xl border border-slate-100 bg-slate-50 p-6 text-slate-500 text-sm text-center"
            >
              Aucune alerte poids détectée
            </div>

            <div v-else class="space-y-4">
              <div
                v-for="alert in weightAlerts"
                :key="alert.id"
                class="rounded-3xl border border-rose-100 bg-rose-50 p-4"
              >
                <div class="flex items-center justify-between gap-3">
                  <div>
                    <div class="text-sm font-bold text-slate-900">{{ alert.numOf }}</div>
                    <div class="text-[11px] text-slate-500">
                      {{ alert.designationArticle }} / {{ alert.codeTypeMatiere }}
                    </div>
                  </div>
                  <BadgeStatus status="AvecEcart" />
                </div>
                <div class="mt-3 grid grid-cols-2 gap-3 text-[12px] text-slate-600">
                  <div class="rounded-2xl bg-white p-3 border border-slate-100">
                    <div class="text-[10px] uppercase tracking-[0.24em] text-slate-400">Poids déclaré</div>
                    <div class="mt-2 text-xl font-extrabold text-slate-900">
                      {{ formatNumber(alert.poidsKg, 1) }} kg
                    </div>
                  </div>
                  <div class="rounded-2xl bg-white p-3 border border-slate-100">
                    <div class="text-[10px] uppercase tracking-[0.24em] text-slate-400">Poids physique</div>
                    <div class="mt-2 text-xl font-extrabold text-slate-900">
                      {{ formatNumber(alert.poidsPhysique, 1) }} kg
                    </div>
                  </div>
                </div>
                <div class="mt-3 text-sm text-rose-700 font-bold">
                  Différence : {{ formatNumber(alert.difference, 1) }} kg
                </div>
              </div>
            </div>
          </div>

          <div class="rounded-3xl border border-slate-100 bg-white p-6 shadow-sm">
            <div class="flex items-center justify-between mb-5">
              <div>
                <h3 class="text-sm font-bold text-slate-900">Alertes Écart Bilan</h3>
                <p class="text-xs text-slate-500 mt-1">Bilans matière dépassant le seuil de tolérance.</p>
              </div>
              <div class="text-[10px] uppercase tracking-[0.24em] text-slate-400">
                Seuil {{ BUSINESS_RULES.seuilEcartPourcentage }} %
              </div>
            </div>

            <div
              v-if="!bilanAlerts.length"
              class="rounded-3xl border border-slate-100 bg-slate-50 p-6 text-slate-500 text-sm text-center"
            >
              Aucune alerte bilan détectée
            </div>

            <div v-else class="space-y-4">
              <div
                v-for="alert in bilanAlerts"
                :key="alert.id"
                class="rounded-3xl border border-amber-100 bg-amber-50 p-4"
              >
                <div class="flex items-center justify-between gap-3">
                  <div>
                    <div class="text-sm font-bold text-slate-900">{{ alert.typeMatiere }}</div>
                    <div class="text-[11px] text-slate-500">{{ formatNumber(alert.tauxEcart, 2) }} %</div>
                  </div>
                  <BadgeStatus status="AvecEcart" />
                </div>
                <div class="mt-3 grid grid-cols-2 gap-3 text-[12px] text-slate-600">
                  <div class="rounded-2xl bg-white p-3 border border-slate-100">
                    <div class="text-[10px] uppercase tracking-[0.24em] text-slate-400">Input total</div>
                    <div class="mt-2 text-xl font-extrabold text-slate-900">
                      {{ formatNumber(alert.inputTotal, 0) }} kg
                    </div>
                  </div>
                  <div class="rounded-2xl bg-white p-3 border border-slate-100">
                    <div class="text-[10px] uppercase tracking-[0.24em] text-slate-400">Output total</div>
                    <div class="mt-2 text-xl font-extrabold text-slate-900">
                      {{ formatNumber(alert.outputTotal, 0) }} kg
                    </div>
                  </div>
                </div>
                <div class="mt-3 text-sm text-slate-700">Justification : {{ alert.justification }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
