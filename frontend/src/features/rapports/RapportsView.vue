<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { FileDown, Eye, CheckCircle2, Calendar, FileSpreadsheet } from 'lucide-vue-next';
import { rapportsService } from './rapports.service';
import { RapportsListType } from './rapports.types';
import { reportExporter } from './exporter';
import PageHeader from '@/components/ui/PageHeader.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import DataSourceBadge from '@/components/ui/DataSourceBadge.vue';
import { BUSINESS_RULES } from '@/config/businessRules';

const rapports = ref<RapportsListType[]>([]);
const isLoading = ref(true);
const successMessage = ref('');

// Filters
const filterMois = ref(BUSINESS_RULES.periodeParDefaut);
const filterDateDebut = ref(BUSINESS_RULES.dateDebutParDefaut);
const filterDateFin = ref(BUSINESS_RULES.dateFinParDefaut);
const filterModule = ref('TOUS');

const getMonthRange = (mois: string) => {
  const [year, month] = mois.split('-').map(Number);
  const lastDay = new Date(year, month, 0).getDate();
  return {
    dateDebut: `${mois}-01`,
    dateFin: `${mois}-${String(lastDay).padStart(2, '0')}`
  };
};

const syncMonthRange = async () => {
  const { dateDebut, dateFin } = getMonthRange(filterMois.value);
  filterDateDebut.value = dateDebut;
  filterDateFin.value = dateFin;
  await loadRapports();
};

const loadRapports = async () => {
  isLoading.value = true;
  try {
    rapports.value = await rapportsService.getRapports(filterMois.value, filterDateDebut.value, filterDateFin.value);
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const filteredRapports = computed(() => {
  if (filterModule.value === 'TOUS') return rapports.value;
  return rapports.value.filter((rapport) => rapport.module === filterModule.value);
});

const handleExport = (id: number, format: 'PDF' | 'Excel', name: string) => {
  let success = false;
  if (format === 'Excel') {
    success = reportExporter.exportExcel(id, name);
  } else {
    success = reportExporter.exportPDF(id, name);
  }

  if (success) {
    successMessage.value = `Le fichier "${name}" au format ${format} a été généré et téléchargé avec succès !`;
  } else {
    successMessage.value = `Erreur lors de la génération du rapport "${name}".`;
  }
  setTimeout(() => (successMessage.value = ''), 5000);
};

const handleApercu = (id: number, name: string) => {
  // Opening the preview by triggering the text/PDF generation
  const success = reportExporter.exportPDF(id, name);
  if (success) {
    successMessage.value = `Génération de l'aperçu textuel pour "${name}" réussie. Téléchargement lancé !`;
  } else {
    successMessage.value = `Erreur lors de la génération de l'aperçu pour "${name}".`;
  }
  setTimeout(() => (successMessage.value = ''), 5000);
};

onMounted(() => {
  loadRapports();
});
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <PageHeader title="Rapports" description="Consulter et exporter les rapports d’exploitation du bilan matière." />
    <DataSourceBadge vertical="rapports" />

    <!-- Notifications -->
    <div
      v-if="successMessage"
      class="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-emerald-800 text-xs font-semibold flex items-center gap-3"
    >
      <CheckCircle2 class="w-5 h-5 text-emerald-500 shrink-0" />
      <span>{{ successMessage }}</span>
    </div>

    <!-- Filters Row -->
    <div
      class="bg-white rounded-xl border border-slate-100 p-5 shadow-sm flex flex-col sm:flex-row justify-between items-center gap-4"
    >
      <div class="flex flex-wrap items-center gap-4 w-full sm:w-auto">
        <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
          <Calendar class="w-4 h-4 text-slate-400 shrink-0" />
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Mois</span>
          <input
            type="month"
            v-model="filterMois"
            class="bg-transparent border-none text-xs font-bold focus:outline-none cursor-pointer"
            @change="syncMonthRange"
          />
        </div>

        <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
          <Calendar class="w-4 h-4 text-slate-400 shrink-0" />
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Date Debut</span>
          <input
            type="date"
            v-model="filterDateDebut"
            class="bg-transparent border-none text-xs font-bold focus:outline-none cursor-pointer text-slate-700"
            @change="
              () => {
                filterMois = filterDateDebut.slice(0, 7);
                loadRapports();
              }
            "
          />
        </div>

        <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
          <Calendar class="w-4 h-4 text-slate-400 shrink-0" />
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Date Fin</span>
          <input
            type="date"
            v-model="filterDateFin"
            class="bg-transparent border-none text-xs font-bold focus:outline-none cursor-pointer text-slate-700"
            @change="loadRapports"
          />
        </div>

        <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
          <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Module</span>
          <select
            v-model="filterModule"
            class="bg-transparent border-none text-xs font-bold cursor-pointer focus:outline-none"
          >
            <option value="TOUS">TOUS LES MODULES</option>
            <option value="Bilan">Bilan matière</option>
            <option value="Déchets">Déchets fonderie</option>
            <option value="Réceptions">Réceptions Usinage 2</option>
            <option value="Sage X3">Synchronisation sorties X3</option>
            <option value="Inventaire">Inventaire Fonderie</option>
            <option value="Alertes">Alertes</option>
          </select>
        </div>
      </div>
    </div>

    <!-- Reports list -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <div v-if="isLoading" class="col-span-2">
        <LoadingState />
      </div>

      <template v-else>
        <div
          v-for="rep in filteredRapports"
          :key="rep.id"
          class="bg-white rounded-xl border border-slate-100 p-6 shadow-sm hover:shadow-md transition-shadow flex flex-col justify-between space-y-4"
        >
          <div class="space-y-2">
            <div class="flex justify-between items-start">
              <span
                class="px-2.5 py-0.5 rounded-full text-[9px] font-black uppercase tracking-wider bg-slate-100 text-slate-600"
              >
                {{ rep.module }}
              </span>
              <span class="text-[10px] text-slate-400 font-mono font-bold">Généré le: {{ rep.dateGeneration }}</span>
            </div>

            <h4 class="text-sm font-black text-slate-800 leading-tight">{{ rep.titre }}</h4>
            <p class="text-xs text-slate-500 font-semibold leading-relaxed">{{ rep.description }}</p>
          </div>

          <div class="pt-4 border-t border-slate-50 flex items-center justify-between gap-3">
            <button
              @click="handleApercu(rep.id, rep.titre)"
              class="flex items-center gap-1 text-xs font-bold text-slate-600 hover:text-blue-900 cursor-pointer"
            >
              <Eye class="w-4 h-4" />
              Aperçu en ligne
            </button>

            <div class="flex gap-2">
              <button
                v-if="rep.formatDisponible.includes('PDF')"
                @click="handleExport(rep.id, 'PDF', rep.titre)"
                class="flex items-center gap-1.5 px-3 py-1.5 bg-rose-50 border border-rose-100 text-rose-700 hover:bg-rose-100 rounded-lg text-[10px] font-bold uppercase tracking-wider cursor-pointer transition-colors"
              >
                <FileDown class="w-3.5 h-3.5" />
                PDF
              </button>
              <button
                v-if="rep.formatDisponible.includes('Excel')"
                @click="handleExport(rep.id, 'Excel', rep.titre)"
                class="flex items-center gap-1.5 px-3 py-1.5 bg-blue-50 border border-blue-100 text-blue-800 hover:bg-blue-100 rounded-lg text-[10px] font-bold uppercase tracking-wider cursor-pointer transition-colors"
              >
                <FileSpreadsheet class="w-3.5 h-3.5" />
                EXCEL
              </button>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>
