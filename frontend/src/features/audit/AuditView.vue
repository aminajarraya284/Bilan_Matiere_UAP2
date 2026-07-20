<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { History, Search, User } from 'lucide-vue-next';
import { auditService } from './audit.service';
import { AuditLogEntry } from './audit.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import LoadingState from '@/components/ui/LoadingState.vue';

const logs = ref<AuditLogEntry[]>([]);
const isLoading = ref(true);
const searchQuery = ref('');

const loadAuditLogs = async () => {
  isLoading.value = true;
  try {
    logs.value = await auditService.getLogs();
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const filteredLogs = computed(() => {
  if (!searchQuery.value) return logs.value;
  const query = searchQuery.value.toLowerCase();
  return logs.value.filter(
    (log) =>
      log.utilisateur.toLowerCase().includes(query) ||
      log.action.toLowerCase().includes(query) ||
      log.module.toLowerCase().includes(query) ||
      log.detail.toLowerCase().includes(query)
  );
});

onMounted(() => {
  loadAuditLogs();
});
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <PageHeader
      title="Journal d’audit"
      description="Consulter la traçabilité des actions et des opérations réalisées dans l’application."
    />

    <!-- Filter/Search Bar -->
    <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm flex items-center gap-3">
      <Search class="w-4 h-4 text-slate-400 shrink-0" />
      <input
        v-model="searchQuery"
        type="text"
        placeholder="Rechercher par utilisateur, action, module..."
        class="w-full bg-slate-50 border border-slate-100 rounded-lg p-2 text-xs font-semibold focus:bg-white"
      />
    </div>

    <!-- Audit logs table -->
    <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col">
      <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
        <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
          <History class="w-4 h-4 text-blue-900" />
          Mouvements du Journal d'Audit
        </h3>
        <span class="text-[10px] text-slate-400 font-bold font-mono">Total : {{ filteredLogs.length }} traces</span>
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
              <th class="p-3">Horodatage</th>
              <th class="p-3">Utilisateur</th>
              <th class="p-3">Rôle</th>
              <th class="p-3">Action Réalisée</th>
              <th class="p-3">Module</th>
              <th class="p-3">Détail des Modifications</th>
              <th class="p-3">Statut</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 text-xs font-medium">
            <tr v-for="log in filteredLogs" :key="log.id" class="hover:bg-slate-50/30 transition-colors">
              <td class="p-3 font-mono text-slate-500 text-[11px]">{{ log.dateHeure }}</td>
              <td class="p-3">
                <div class="flex items-center gap-2">
                  <User class="w-3.5 h-3.5 text-slate-400" />
                  <span class="font-bold text-slate-800">{{ log.utilisateur }}</span>
                </div>
              </td>
              <td class="p-3 font-semibold text-slate-500 text-[10px] uppercase tracking-wide">{{ log.role }}</td>
              <td class="p-3 font-bold text-blue-900">{{ log.action }}</td>
              <td class="p-3">
                <span
                  class="px-2 py-0.5 rounded text-[10px] font-bold bg-slate-100 text-slate-700 border border-slate-200"
                >
                  {{ log.module }}
                </span>
              </td>
              <td class="p-3 text-slate-600 text-[11px] leading-relaxed max-w-sm truncate" :title="log.detail">
                {{ log.detail }}
              </td>
              <td class="p-3">
                <span
                  class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[10px] font-bold"
                  :class="{
                    'bg-emerald-50 text-emerald-700 border border-emerald-100': log.statut === 'Succès',
                    'semantic-attention border': log.statut === 'Attention',
                    'bg-rose-50 text-rose-700 border border-rose-100': log.statut === 'Échec'
                  }"
                >
                  <span
                    class="w-1.5 h-1.5 rounded-full"
                    :class="{
                      'bg-emerald-500': log.statut === 'Succès',
                      'bg-[var(--color-attention)]': log.statut === 'Attention',
                      'bg-rose-500': log.statut === 'Échec'
                    }"
                  ></span>
                  {{ log.statut }}
                </span>
              </td>
            </tr>
            <tr v-if="!filteredLogs.length">
              <td colspan="7" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
                Aucune trace d'audit ne correspond à votre recherche
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
