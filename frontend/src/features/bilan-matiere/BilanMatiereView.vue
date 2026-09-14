<script setup lang="ts">
import type { Component } from 'vue';
import { ref, onMounted, computed } from 'vue';
import {
  Scale,
  CheckCircle2,
  Calendar,
  RefreshCw,
  Printer,
  Wrench,
  BarChart3,
} from 'lucide-vue-next';
import { RouterLink, useRoute } from 'vue-router';
import { useAuthStore } from '@/features/auth/auth.store';
import { bilanService } from './bilan.service';
import { rebutsUsinage2Service } from './rebutsUsinage2.service';
import type { RebutUsinage2 } from './rebutsUsinage2.types';
import { BilanMatiereReport, BilanUsinage2Api } from './bilan.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import BadgeStatus from '@/components/ui/BadgeStatus.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import DataSourceBadge from '@/components/ui/DataSourceBadge.vue';
import { formatNumber } from '@/utils/formatters';
import { useAppDialog } from '@/composables/useAppDialog';
import { BUSINESS_RULES } from '@/config/businessRules';
import type { DataVertical } from '@/config/dataSources';
import BilanMatiereUap2View from './BilanMatiereUap2View.vue';
import { archivageService } from '@/features/archivage/archivage.service';
import type { ArchiveDetail } from '@/features/archivage/archivage.types';

const authStore = useAuthStore();
const route = useRoute();
const dialog = useAppDialog();
const isAdmin = computed(() => authStore.userRole === 'Admin');

// Tab state
type TabId = 'fonderie' | 'usinage2' | 'uap2';
const canOpenTab = (tab: TabId): boolean => {
  if (authStore.roles.includes('Admin')) return true;
  if (tab === 'fonderie') return authStore.roles.includes('ResponsableFonderie');
  return authStore.roles.includes('ResponsableUsinage2');
};
const requestedTab = typeof route.query.tab === 'string' ? route.query.tab : '';
const defaultTab: TabId = authStore.roles.includes('ResponsableUsinage2') ? 'usinage2' : 'fonderie';
const activeTab = ref<TabId>(
  (requestedTab === 'fonderie' || requestedTab === 'usinage2' || requestedTab === 'uap2') && canOpenTab(requestedTab)
    ? requestedTab
    : defaultTab
);
const activeBilanVertical = computed<DataVertical>(() =>
  activeTab.value === 'fonderie' ? 'bilanFonderie' : activeTab.value === 'uap2' ? 'bilanUap2' : 'bilanU2'
);

const allTabs: { id: TabId; label: string; icon: Component }[] = [
  { id: 'fonderie', label: 'Bilan Fonderie', icon: Scale },
  { id: 'usinage2', label: 'Bilan Usinage 2', icon: Wrench },
  { id: 'uap2', label: 'Bilan UAP2 Total', icon: BarChart3 }
];
const tabs = computed(() => allTabs.filter((tab) => canOpenTab(tab.id)));

// Fonderie state
const reports = ref<BilanMatiereReport[]>([]);
const isLoading = ref(true);
const fonderieError = ref('');
let fonderieRequest = 0;
const successMessage = ref('');
const archiveError = ref('');
const isArchiving = ref(false);
const lastArchive = ref<ArchiveDetail | null>(null);
const archiveMois = ref<string>(BUSINESS_RULES.periodeParDefaut);

const enregistrerPeriode = async () => {
  if (!isAdmin.value || isArchiving.value) return;
  const periode = archiveMois.value;
  const confirme = await dialog.confirm(
    `Enregistrer définitivement la période ${periode} ? Les résultats seront figés et consultables en lecture seule.`,
    { title: 'Enregistrer la période', tone: 'warning', confirmLabel: 'Enregistrer' }
  );
  if (!confirme) return;
  isArchiving.value = true;
  archiveError.value = '';
  try {
    lastArchive.value = await archivageService.enregistrer(periode);
    successMessage.value = `Période ${periode} enregistrée avec succès.`;
  } catch (error) {
    const status = (error as { response?: { status?: number } })?.response?.status;
    archiveError.value = status === 409
      ? `La période ${periode} est déjà enregistrée.`
      : `Impossible d'enregistrer la période ${periode}.`;
  } finally {
    isArchiving.value = false;
  }
};

// Usinage state
const bilanUsinage = ref<BilanUsinage2Api | null>(null);
const bilansUsinage = computed(() => bilanUsinage.value?.lignes ?? []);
const isLoadingUsinage = ref(true);
const usinageError = ref('');
// Les totaux viennent du backend et couvrent la PERIODE ENTIERE. Les additionner depuis
// `bilansUsinage` ne sommerait que la page affichee (50 lignes sur plusieurs centaines) :
// un rebut saisi sur un OF absent de la page 1 resterait invisible dans les KPI.
const totalRebutUsinage2Pieces = computed(() => bilanUsinage.value?.totalRebutUsinage2Pieces ?? 0);
const totalEntreeOfPieces = computed(() => bilanUsinage.value?.totalEntreeOfPieces ?? 0);
/** Null tant qu'une ligne de la periode n'est pas calculable : affiche « — », jamais 0. */
const totalBilanUsinage2Pieces = computed(() => bilanUsinage.value?.totalBilanUsinage2Pieces ?? null);
const nombreAnomaliesUsinage2 = computed(() => bilanUsinage.value?.nombreAnomalies ?? 0);

const getBilanUsinageStatus = (ligne: BilanUsinage2Api['lignes'][number]): string => {
  if (ligne.bilanUsinage2Pieces === null) return 'DonneesManquantes';
  return ligne.anomalies.length > 0 ? 'Anomalie' : 'OK';
};

/**
 * Etat vide explicite : plutot qu'un simple zero, on nomme la dependance manquante.
 * Les anomalies remontees par le backend permettent de distinguer les causes.
 */
const usinageEmptyMessage = computed(() => {
  if (!bilanUsinage.value) return 'Aucun bilan Usinage 2 chargé pour cette période.';
  const anomalies = bilanUsinage.value.lignes.flatMap((ligne) => ligne.anomalies);
  if (anomalies.includes('AUCUNE_RECEPTION_U2_VALIDEE')) {
    return 'Aucune réception U2 validée pour cette période.';
  }
  if (anomalies.length > 0) {
    return 'Les données nécessaires au calcul sont incomplètes.';
  }
  return 'Aucune donnée ne correspond à la période sélectionnée.';
});

// Filters
const filterMois = ref<string>(BUSINESS_RULES.periodeParDefaut);
const filterDateDebut = ref<string>(BUSINESS_RULES.dateDebutParDefaut);
const filterDateFin = ref<string>(BUSINESS_RULES.dateFinParDefaut);
const filterAlliage = ref<string>('TOUS');

// ===== FONDERIE =====
const loadBilans = async () => {
  const request = ++fonderieRequest;
  reports.value = [];
  fonderieError.value = '';
  const dateDebut = filterDateDebut.value;
  const dateFin = filterDateFin.value;
  if (!dateDebut || !dateFin || dateDebut > dateFin) {
    fonderieError.value = 'Renseignez les deux dates, avec une date de début antérieure ou égale à la date de fin.';
    isLoading.value = false;
    return;
  }
  isLoading.value = true;
  try {
    const result = await bilanService.getBilans(filterMois.value, dateDebut, dateFin);
    if (request === fonderieRequest) reports.value = result;
  } catch {
    if (request === fonderieRequest) {
      fonderieError.value = "Le bilan Fonderie n'a pas pu être chargé. Vérifiez les dates et la disponibilité de l'API.";
    }
  } finally {
    if (request === fonderieRequest) isLoading.value = false;
  }
};

const handlePrint = () => {
  window.print();
};

const filteredReports = computed(() => {
  if (filterAlliage.value === 'TOUS') return reports.value;
  return reports.value.filter((r) => r.typeMatiere === filterAlliage.value);
});

const availableMatieres = computed(() => {
  return Array.from(new Set(reports.value.map((report) => report.typeMatiere))).sort();
});

/** Un ecart inconnu n'est pas un ecart acceptable : il ne doit pas s'afficher en neutre. */
const depasseSeuilEcartKg = (valeur: number | null): boolean =>
  valeur !== null && valeur > BUSINESS_RULES.seuilEcartKgHighlight;

const depasseSeuilEcartPourcent = (valeur: number | null): boolean =>
  valeur !== null && valeur > BUSINESS_RULES.seuilEcartPourcentage;

/**
 * Une seule ligne dont la composante est inconnue rend le total inconnu : afficher une
 * somme partielle la ferait passer pour exhaustive (RM-COM-003).
 */
const sommeOuNull = (selecteur: (report: BilanMatiereReport) => number | null): number | null =>
  filteredReports.value.some((report) => selecteur(report) === null)
    ? null
    : filteredReports.value.reduce((total, report) => total + (selecteur(report) ?? 0), 0);

const totalReport = computed(() => {
  const inputTotal = sommeOuNull((report) => report.inputTotal);
  const ecartKg = sommeOuNull((report) => report.ecartKg);

  return {
    encoursDebut: sommeOuNull((report) => report.encoursDebut),
    sortiesX3: sommeOuNull((report) => report.sortiesX3),
    inputTotal,
    encoursFinReel: sommeOuNull((report) => report.encoursFinReel),
    dechetsFonderie: filteredReports.value.reduce((total, report) => total + report.dechetsFonderie, 0),
    receptionsUsinage2: sommeOuNull((report) => report.receptionsUsinage2),
    poidsNonReceptionne: filteredReports.value.reduce(
      (total, report) => total + (report.poidsNonReceptionne || 0),
      0
    ),
    nombrePiecesNonReceptionnees: filteredReports.value.reduce(
      (total, report) => total + (report.nombrePiecesNonReceptionnees || 0),
      0
    ),
    outputTotal: sommeOuNull((report) => report.outputTotal),
    ecartKg,
    tauxEcart: ecartKg === null || inputTotal === null || inputTotal === 0 ? null : (ecartKg / inputTotal) * 100
  };
});

// ===== USINAGE 2 =====
const loadBilansUsinage = async () => {
  isLoadingUsinage.value = true;
  usinageError.value = '';
  try {
    bilanUsinage.value = await bilanService.getBilansUsinage(
      filterMois.value,
      filterDateDebut.value,
      filterDateFin.value
    );
  } catch (err) {
    console.error(err);
    bilanUsinage.value = null;
    usinageError.value = "Le Bilan Usinage 2 n'a pas pu être chargé depuis l'API. Aucun résultat de secours n'est affiché.";
  } finally {
    isLoadingUsinage.value = false;
  }
};

// ===== REBUTS USINAGE 2 (saisie en PIECES) =====
// Le rebut saisi ici alimente le terme « Rebut U2 » de la formule
// Bilan U2 = Réception U2 - Rebut U2 - Entrée OF signée.
// Le calcul reste integralement backend : seuls les rebuts VALIDES sont pris en compte,
// et le bilan est rechargé apres chaque action plutot que recalculé dans la vue.
const rebuts = ref<RebutUsinage2[]>([]);
const rebutsLoading = ref(false);
const rebutsError = ref('');
const rebutSuccess = ref('');
const rebutActionEnCours = ref(false);
const rebutEnEdition = ref<string | null>(null);
const rebutForm = ref({
  dateDeclaration: `${filterMois.value}-01`,
  numeroOF: '',
  codeArticle: '',
  quantiteRebutPieces: null as number | null,
  motif: '',
  observation: ''
});

const rebutsDisponibles = computed(() => rebutsUsinage2Service.isAvailable());

/** Champs bloquants de la saisie : OF, article et une quantite strictement positive. */
const blocagesRebut = computed(() => {
  const motifs: string[] = [];
  if (!rebutForm.value.numeroOF.trim()) motifs.push("Numéro d'OF obligatoire");
  if (!rebutForm.value.codeArticle.trim()) motifs.push('Code article obligatoire');
  if (!rebutForm.value.dateDeclaration) motifs.push('Date de déclaration obligatoire');
  const quantite = rebutForm.value.quantiteRebutPieces;
  if (quantite === null || quantite <= 0) motifs.push('Quantité rebutée strictement positive (pièces)');
  return motifs;
});

const messageErreurRebut = (error: unknown, secours: string): string => {
  const probleme = (error as { response?: { data?: { detail?: string; title?: string; errors?: Record<string, string[]> } } })
    ?.response?.data;
  const validation = probleme?.errors ? Object.values(probleme.errors).flat().join(' ') : '';
  return validation || probleme?.detail || probleme?.title || (error instanceof Error ? error.message : secours);
};

const loadRebuts = async () => {
  if (!isAdmin.value || !rebutsDisponibles.value) return;
  const [annee, mois] = filterMois.value.split('-').map(Number);
  rebutsLoading.value = true;
  rebutsError.value = '';
  try {
    rebuts.value = (await rebutsUsinage2Service.getRebuts({ annee, mois, page: 1, pageSize: 50 })).items;
  } catch (error) {
    rebuts.value = [];
    rebutsError.value = messageErreurRebut(error, "Les rebuts U2 n'ont pas pu être chargés.");
  } finally {
    rebutsLoading.value = false;
  }
};

const reinitialiserRebutForm = () => {
  rebutEnEdition.value = null;
  rebutForm.value = {
    dateDeclaration: `${filterMois.value}-01`,
    numeroOF: '',
    codeArticle: '',
    quantiteRebutPieces: null,
    motif: '',
    observation: ''
  };
};

/** Recharge la liste ET le bilan : le total Rebut U2 doit refleter l'action immediatement. */
const rafraichirApresRebut = async (message: string) => {
  await Promise.all([loadRebuts(), loadBilansUsinage()]);
  rebutSuccess.value = message;
  window.setTimeout(() => (rebutSuccess.value = ''), 4000);
};

const enregistrerRebut = async () => {
  if (blocagesRebut.value.length > 0) return;
  rebutActionEnCours.value = true;
  rebutsError.value = '';
  try {
    const payload = {
      dateDeclaration: rebutForm.value.dateDeclaration,
      numeroOF: rebutForm.value.numeroOF.trim().toUpperCase(),
      codeArticle: rebutForm.value.codeArticle.trim().toUpperCase(),
      quantiteRebutPieces: rebutForm.value.quantiteRebutPieces as number,
      motif: rebutForm.value.motif.trim() || null,
      observation: rebutForm.value.observation.trim() || null
    };
    if (rebutEnEdition.value) {
      await rebutsUsinage2Service.updateRebut(rebutEnEdition.value, payload);
      await rafraichirApresRebut('Rebut U2 modifié. Bilan U2 actualisé.');
    } else {
      await rebutsUsinage2Service.createRebut(payload);
      await rafraichirApresRebut('Rebut U2 créé en brouillon. Bilan U2 actualisé.');
    }
    reinitialiserRebutForm();
  } catch (error) {
    rebutsError.value = messageErreurRebut(error, "L'enregistrement du rebut U2 a échoué.");
  } finally {
    rebutActionEnCours.value = false;
  }
};

const modifierRebut = (rebut: RebutUsinage2) => {
  rebutEnEdition.value = rebut.id;
  rebutForm.value = {
    dateDeclaration: rebut.dateDeclaration,
    numeroOF: rebut.numeroOF,
    codeArticle: rebut.codeArticle,
    quantiteRebutPieces: rebut.quantiteRebutPieces,
    motif: rebut.motif ?? '',
    observation: rebut.observation ?? ''
  };
};

const validerRebut = async (rebut: RebutUsinage2) => {
  const confirme = await dialog.confirm(
    `Valider le rebut de ${formatNumber(rebut.quantiteRebutPieces, 0)} pièce(s) sur ${rebut.numeroOF} ?`,
    { title: 'Valider le rebut U2', tone: 'warning', confirmLabel: 'Valider' }
  );
  if (!confirme) return;
  rebutActionEnCours.value = true;
  try {
    await rebutsUsinage2Service.validerRebut(rebut.id);
    await rafraichirApresRebut('Rebut U2 validé. Il entre désormais dans le Bilan U2.');
  } catch (error) {
    rebutsError.value = messageErreurRebut(error, 'La validation du rebut a échoué.');
  } finally {
    rebutActionEnCours.value = false;
  }
};

const annulerRebut = async (rebut: RebutUsinage2) => {
  const confirme = await dialog.confirm(`Annuler le rebut sur ${rebut.numeroOF} ?`, {
    title: 'Annuler le rebut U2',
    tone: 'danger',
    confirmLabel: 'Annuler le rebut'
  });
  if (!confirme) return;
  rebutActionEnCours.value = true;
  try {
    await rebutsUsinage2Service.annulerRebut(rebut.id);
    await rafraichirApresRebut('Rebut U2 annulé. Il sort du Bilan U2 mais reste dans l’historique.');
  } catch (error) {
    rebutsError.value = messageErreurRebut(error, "L'annulation du rebut a échoué.");
  } finally {
    rebutActionEnCours.value = false;
  }
};

// Une composante inconnue reste « — » : formatNumber renverrait « 0,00 » et ferait
// passer un stock indisponible pour un stock nul (RM-COM-003).
const formatNullable = (value: number | null, decimals = 0): string =>
  value === null ? '—' : formatNumber(value, decimals);

// ===== TAB SWITCH =====
const switchTab = (tab: TabId) => {
  if (!canOpenTab(tab)) return;
  activeTab.value = tab;
  if (tab === 'usinage2' && bilansUsinage.value.length === 0) loadBilansUsinage();
  // La saisie des rebuts vit dans cet onglet : sa liste doit suivre la meme periode.
  if (tab === 'usinage2') loadRebuts();
};

// ===== INIT =====
onMounted(() => {
  if (activeTab.value === 'fonderie') loadBilans();
  if (activeTab.value === 'usinage2') {
    loadBilansUsinage();
    loadRebuts();
  }
});
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <PageHeader
      title="Bilan matière"
      description="Consulter les bilans et les écarts de production de l’UAP2."
    />
    <DataSourceBadge :vertical="activeBilanVertical" />

    <section v-if="isAdmin" class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-blue-200 bg-blue-50 p-4">
      <div>
        <p class="text-sm font-black text-blue-950">Clôture mensuelle</p>
        <p class="mt-1 text-xs text-blue-700">Le backend recalculera et figera les trois bilans du mois entier, indépendamment des dates de consultation.</p>
      </div>
      <label class="text-xs font-bold text-blue-950">
        Mois à archiver
        <input v-model="archiveMois" type="month" required aria-label="Mois à archiver" :disabled="isArchiving" class="ml-2 rounded border border-blue-200 p-2" />
      </label>
      <button class="rounded-lg bg-blue-900 px-4 py-2 text-xs font-bold text-white disabled:opacity-50" :disabled="isArchiving" @click="enregistrerPeriode">
        {{ isArchiving ? 'Enregistrement…' : 'Enregistrer la période' }}
      </button>
    </section>

    <div v-if="archiveError" class="rounded-xl border border-amber-200 bg-amber-50 p-4 text-xs font-semibold text-amber-800" role="alert">{{ archiveError }}</div>
    <div v-if="lastArchive" class="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-xs text-emerald-900">
      <strong>Enregistrée le {{ new Date(lastArchive.metadata.dateEnregistrementUtc).toLocaleString('fr-FR') }}</strong>
      <span> par {{ lastArchive.metadata.enregistrePar }} · {{ lastArchive.metadata.statutGlobal }}</span>
      <RouterLink :to="`/historique-periodes/${lastArchive.metadata.periode}`" class="ml-3 font-bold underline">Voir l’archive</RouterLink>
    </div>

    <!-- Success notice -->
    <div
      v-if="successMessage"
      class="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-emerald-800 text-xs font-semibold flex items-center gap-3"
    >
      <CheckCircle2 class="w-5 h-5 text-emerald-500 shrink-0" />
      <span>{{ successMessage }}</span>
    </div>

    <!-- Tab Bar -->
    <div class="app-tabs">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        @click="switchTab(tab.id)"
        class="app-tab flex-1"
        :class="activeTab === tab.id ? 'app-tab--active' : ''"
      >
        <component :is="tab.icon" class="w-4 h-4" />
        {{ tab.label }}
      </button>
    </div>

    <!-- ===================== ONGLET FONDERIE ===================== -->
    <template v-if="activeTab === 'fonderie'">
      <!-- Filters & Calculations Controls -->
      <div
        class="bg-white rounded-xl border border-slate-100 p-5 shadow-sm flex flex-col md:flex-row justify-between items-center gap-4"
      >
        <div class="flex flex-wrap items-center gap-4 w-full md:w-auto">
          <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
            <Calendar class="w-4 h-4 text-slate-400 shrink-0" />
            <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Date Début</span>
            <input
              type="date"
              aria-label="Date début"
              required
              v-model="filterDateDebut"
              class="bg-transparent border-none text-xs font-bold focus:outline-none cursor-pointer text-slate-700"
              @change="loadBilans"
            />
          </div>

          <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
            <Calendar class="w-4 h-4 text-slate-400 shrink-0" />
            <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Date Fin</span>
            <input
              type="date"
              aria-label="Date fin"
              required
              v-model="filterDateFin"
              class="bg-transparent border-none text-xs font-bold focus:outline-none cursor-pointer text-slate-700"
              @change="loadBilans"
            />
          </div>

          <div class="flex items-center gap-2 bg-slate-50 border border-slate-100 p-2 rounded-lg">
            <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Filtrer Type Matière</span>
            <select
              v-model="filterAlliage"
              class="bg-transparent border-none text-xs font-bold cursor-pointer focus:outline-none"
            >
              <option value="TOUS">TOUS LES TYPES MATIÈRE</option>
              <option v-for="matiere in availableMatieres" :key="matiere" :value="matiere">{{ matiere }}</option>
            </select>
          </div>
        </div>

        <div class="flex flex-wrap gap-2 items-center justify-end w-full md:w-auto">
          <button
            @click="handlePrint"
            class="bg-slate-700 hover:bg-slate-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-5 rounded-xl flex items-center gap-2 cursor-pointer active:scale-95 transition-all"
          >
            <Printer class="w-4 h-4" />
            Imprimer
          </button>
          <button
            @click="loadBilans"
            :disabled="isLoading"
            class="bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-6 rounded-xl flex items-center gap-1.5 cursor-pointer disabled:opacity-50 active:scale-95 transition-all w-full md:w-auto justify-center"
          >
            <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': isLoading }" />
            {{ isLoading ? 'Actualisation...' : 'Actualiser le bilan' }}
          </button>
        </div>
      </div>

      <!-- Main Reports Table -->
      <p class="text-xs text-slate-600">
        Flux filtrés sur les deux dates incluses. Les stocks sont des états mensuels :
        une borne en cours de mois reste indisponible (—), sans extrapolation.
        « Complet » décrit les données, pas une validation métier.
      </p>
      <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col">
        <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
            <Scale class="w-4 h-4 text-blue-900" />
            Résultats Consolidés Bilan Fonderie
          </h3>
          <span class="text-[10px] text-slate-400 font-bold font-mono"
            >Période : Du {{ filterDateDebut }} au {{ filterDateFin }}</span
          >
        </div>

        <div v-if="isLoading">
          <LoadingState />
        </div>

        <div v-else-if="fonderieError" role="alert" class="p-4 text-sm text-rose-800 bg-rose-50">
          <p>{{ fonderieError }}</p>
          <button type="button" class="mt-2 font-bold underline" @click="loadBilans">Réessayer</button>
        </div>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr
                class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
              >
                <th class="p-3">Type Matière</th>
                <th class="p-3 text-right">Encours Début (kg)</th>
                <th class="p-3 text-right">Sorties X3 (kg)</th>
                <th class="p-3 text-right text-blue-900 bg-blue-50/10">INPUT Total (kg)</th>
                <th class="p-3 text-right">Encours Fin Réel (kg)</th>
                <th class="p-3 text-right">Déchets Fonderie (kg)</th>
                <th class="p-3 text-right">Réceptions U2 (kg)</th>
                <th class="p-3 text-right text-blue-900 bg-blue-50/20">OUTPUT Total (kg)</th>
                <th class="p-3 text-right text-rose-900">Écart (kg)</th>
                <th class="p-3 text-right">Taux Écart</th>
                <th class="p-3">Statut</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr v-for="report in filteredReports" :key="report.bilanKey" class="hover:bg-slate-50/30 transition-colors">
                <td class="p-3 font-bold text-slate-800 font-mono">
                  {{ report.typeMatiere }}
                  <ul v-if="report.anomalies?.length" class="mt-1 text-[10px] font-normal text-amber-800">
                    <li v-for="anomalie in report.anomalies" :key="anomalie">{{ anomalie }}</li>
                  </ul>
                </td>
                <td class="p-3 text-right font-semibold font-mono">{{ formatNullable(report.encoursDebut) }}</td>
                <td class="p-3 text-right font-semibold font-mono text-slate-600">
                  {{ formatNullable(report.sortiesX3) }}
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-900 bg-blue-50/10">
                  {{ formatNullable(report.inputTotal) }}
                </td>
                <td class="p-3 text-right font-semibold font-mono">{{ formatNullable(report.encoursFinReel) }}</td>
                <td class="p-3 text-right font-semibold font-mono text-rose-600">
                  {{ formatNullable(report.dechetsFonderie) }}
                </td>
                <td class="p-3 text-right font-semibold font-mono text-blue-700">
                  {{ formatNullable(report.receptionsUsinage2) }}
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-900 bg-blue-50/20">
                  {{ formatNullable(report.outputTotal) }}
                </td>
                <td
                  class="p-3 text-right font-black font-mono"
                  :class="depasseSeuilEcartKg(report.ecartKg) ? 'text-rose-600' : 'text-slate-700'"
                >
                  <template v-if="report.ecartKg === null">—</template>
                  <template v-else>{{ formatNumber(report.ecartKg, 0) }} kg</template>
                </td>
                <td
                  class="p-3 text-right font-bold font-mono"
                  :class="depasseSeuilEcartPourcent(report.tauxEcart) ? 'text-rose-600' : 'text-slate-600'"
                >
                  <template v-if="report.tauxEcart === null">—</template>
                  <template v-else>{{ formatNumber(report.tauxEcart, 2) }} %</template>
                </td>
                <td class="p-3">
                  <BadgeStatus :status="report.statut" />
                </td>
              </tr>
              <tr v-if="!filteredReports.length">
                <td colspan="11" class="p-6 text-center text-slate-400 font-bold uppercase tracking-wider">
                  Aucun bilan matière disponible pour cette sélection
                </td>
              </tr>
            </tbody>
            <tfoot>
              <tr
                class="bg-slate-100 border-t border-slate-200 text-[10px] text-slate-600 font-bold uppercase tracking-wider"
              >
                <td class="p-3">Total</td>
                <td class="p-3 text-right font-black font-mono">{{ formatNullable(totalReport.encoursDebut) }}</td>
                <td class="p-3 text-right font-black font-mono">{{ formatNullable(totalReport.sortiesX3) }}</td>
                <td class="p-3 text-right font-black font-mono text-blue-900 bg-blue-50/10">
                  {{ formatNullable(totalReport.inputTotal) }}
                </td>
                <td class="p-3 text-right font-black font-mono">{{ formatNullable(totalReport.encoursFinReel) }}</td>
                <td class="p-3 text-right font-black font-mono text-rose-600">
                  {{ formatNullable(totalReport.dechetsFonderie) }}
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-700">
                  {{ formatNullable(totalReport.receptionsUsinage2) }}
                </td>
                <td class="p-3 text-right font-black font-mono text-blue-900 bg-blue-50/20">
                  {{ formatNullable(totalReport.outputTotal) }}
                </td>
                <td
                  class="p-3 text-right font-black font-mono"
                  :class="
                    depasseSeuilEcartKg(totalReport.ecartKg) ? 'text-rose-600' : 'text-slate-700'
                  "
                >
                  <template v-if="totalReport.ecartKg === null">—</template>
                  <template v-else>{{ formatNumber(totalReport.ecartKg, 0) }} kg</template>
                </td>
                <td
                  class="p-3 text-right font-bold font-mono"
                  :class="
                    depasseSeuilEcartPourcent(totalReport.tauxEcart) ? 'text-rose-600' : 'text-slate-600'
                  "
                >
                  <template v-if="totalReport.tauxEcart === null">—</template>
                  <template v-else>{{ formatNumber(totalReport.tauxEcart, 2) }} %</template>
                </td>
                <td class="p-3">—</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>
    </template>

    <!-- ===================== ONGLET USINAGE 2 ===================== -->
    <template v-if="activeTab === 'usinage2'">
      <div v-if="usinageError" class="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">
        <p class="font-semibold">{{ usinageError }}</p>
        <button
          type="button"
          class="mt-3 rounded-lg bg-rose-700 px-4 py-2 text-xs font-bold uppercase text-white hover:bg-rose-800"
          @click="loadBilansUsinage"
        >
          Réessayer
        </button>
      </div>

      <div v-if="bilanUsinage" class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-6 gap-3">
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Quantité totale réceptionnée U2</p>
          <p class="mt-2 text-xl font-black text-blue-800">
            {{ formatNumber(bilanUsinage.quantiteTotaleReceptionneePieces, 0) }}
          </p>
          <p class="text-[10px] text-slate-400">pièces</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total Rebut U2</p>
          <p class="mt-2 text-xl font-black text-rose-700">{{ formatNumber(totalRebutUsinage2Pieces, 0) }}</p>
          <p class="text-[10px] text-slate-400">pièces</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Total Entrée OF</p>
          <p class="mt-2 text-xl font-black text-slate-900">{{ formatNumber(totalEntreeOfPieces, 0) }}</p>
          <p class="text-[10px] text-slate-400">pièces signées</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Écart total Bilan U2</p>
          <p class="mt-2 text-xl font-black text-blue-900">{{ formatNullable(totalBilanUsinage2Pieces, 0) }}</p>
          <p class="text-[10px] text-slate-400">pièces</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Nombre de lignes</p>
          <p class="mt-2 text-xl font-black text-slate-900">{{ bilanUsinage.nombreLignes }}</p>
        </div>
        <div class="bg-white rounded-xl border border-slate-100 p-4 shadow-sm">
          <p class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Nombre d’anomalies</p>
          <p class="mt-2 text-xl font-black text-amber-700">{{ nombreAnomaliesUsinage2 }}</p>
        </div>
      </div>

      <!-- ===== Saisie des Rebuts Usinage 2, en PIECES =====
           Le rebut valide alimente directement le terme « Rebut U2 » du bilan ci-dessous. -->
      <div v-if="isAdmin" class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-50 bg-slate-50/20">
          <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
            <Wrench class="w-4 h-4 text-rose-700" />
            {{ rebutEnEdition ? 'Modifier le rebut U2' : 'Déclarer un rebut Usinage 2' }}
          </h3>
          <p class="mt-1 text-[10px] font-semibold text-slate-500">
            Quantité en pièces. Seuls les rebuts <strong>validés</strong> entrent dans le Bilan U2.
          </p>
        </div>

        <div v-if="!rebutsDisponibles" class="p-6 text-xs font-semibold text-slate-500">
          La saisie des rebuts U2 nécessite une source API réelle.
        </div>

        <div v-else class="p-6 space-y-4">
          <div
            v-if="rebutSuccess"
            class="rounded-lg border border-emerald-200 bg-emerald-50 p-3 text-xs font-semibold text-emerald-800"
            role="status"
          >
            {{ rebutSuccess }}
          </div>
          <div
            v-if="rebutsError"
            class="flex items-center justify-between gap-3 rounded-lg border border-rose-200 bg-rose-50 p-3 text-xs text-rose-800"
          >
            <span>{{ rebutsError }}</span>
            <button type="button" class="font-black underline" @click="loadRebuts">Réessayer</button>
          </div>

          <form class="grid gap-3 md:grid-cols-2 xl:grid-cols-6" @submit.prevent="enregistrerRebut">
            <label class="space-y-1 text-[10px] font-black uppercase tracking-wider text-slate-500">
              Date de déclaration *
              <input
                v-model="rebutForm.dateDeclaration"
                type="date"
                class="w-full rounded-lg border border-slate-300 p-2 text-xs font-bold"
              />
            </label>
            <label class="space-y-1 text-[10px] font-black uppercase tracking-wider text-slate-500">
              Numéro OF *
              <input
                v-model.trim="rebutForm.numeroOF"
                placeholder="OF26070378"
                class="w-full rounded-lg border border-slate-300 p-2 font-mono text-xs"
              />
            </label>
            <label class="space-y-1 text-[10px] font-black uppercase tracking-wider text-slate-500">
              Code article *
              <input
                v-model.trim="rebutForm.codeArticle"
                placeholder="07BL201-2"
                class="w-full rounded-lg border border-slate-300 p-2 font-mono text-xs"
              />
            </label>
            <label class="space-y-1 text-[10px] font-black uppercase tracking-wider text-slate-500">
              Quantité rebutée (pièces) *
              <input
                v-model.number="rebutForm.quantiteRebutPieces"
                type="number"
                min="0.0001"
                step="any"
                class="w-full rounded-lg border border-slate-300 p-2 font-mono text-xs"
              />
            </label>
            <label class="space-y-1 text-[10px] font-black uppercase tracking-wider text-slate-500">
              Motif
              <input v-model.trim="rebutForm.motif" maxlength="200" class="w-full rounded-lg border border-slate-300 p-2 text-xs" />
            </label>
            <div class="flex items-end gap-2">
              <button
                type="submit"
                :disabled="blocagesRebut.length > 0 || rebutActionEnCours"
                class="flex-1 rounded-lg bg-blue-900 px-3 py-2.5 text-[10px] font-black uppercase text-white hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {{ rebutEnEdition ? 'Enregistrer' : 'Déclarer' }}
              </button>
              <button
                v-if="rebutEnEdition"
                type="button"
                class="rounded-lg border border-slate-300 px-3 py-2.5 text-[10px] font-black uppercase text-slate-600"
                @click="reinitialiserRebutForm"
              >
                Annuler
              </button>
            </div>
          </form>

          <ul v-if="blocagesRebut.length" class="list-disc pl-5 text-[11px] font-semibold text-rose-700">
            <li v-for="motif in blocagesRebut" :key="motif">{{ motif }}</li>
          </ul>

          <LoadingState v-if="rebutsLoading" />
          <div v-else class="overflow-x-auto rounded-xl border border-slate-100">
            <table class="w-full min-w-[820px] text-left text-xs">
              <thead class="bg-slate-50/80 text-[10px] font-black uppercase tracking-wider text-slate-500">
                <tr>
                  <th class="p-3">Date</th>
                  <th class="p-3">Numéro OF</th>
                  <th class="p-3">Code article</th>
                  <th class="p-3 text-right">Rebut (pièces)</th>
                  <th class="p-3">Motif</th>
                  <th class="p-3">Statut</th>
                  <th class="p-3">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                <tr v-for="rebut in rebuts" :key="rebut.id" class="hover:bg-slate-50/40">
                  <td class="p-3 font-mono text-slate-600">{{ rebut.dateDeclaration }}</td>
                  <td class="p-3 font-mono font-bold text-slate-800">{{ rebut.numeroOF }}</td>
                  <td class="p-3 font-mono text-slate-700">{{ rebut.codeArticle }}</td>
                  <td class="p-3 text-right font-mono font-black text-rose-700">
                    {{ formatNumber(rebut.quantiteRebutPieces, 0) }}
                  </td>
                  <td class="p-3 text-slate-600">{{ rebut.motif ?? '—' }}</td>
                  <td class="p-3">
                    <span
                      class="inline-flex items-center rounded-full border px-2.5 py-0.5 text-[10px] font-bold"
                      :class="{
                        'border-slate-200 bg-slate-50 text-slate-600': rebut.statut === 'Brouillon',
                        'border-emerald-100 bg-emerald-50 text-emerald-700': rebut.statut === 'Valide',
                        'border-slate-200 bg-slate-100 text-slate-500': rebut.statut === 'Annule'
                      }"
                    >
                      {{ rebut.statut }}
                    </span>
                  </td>
                  <td class="p-3">
                    <div class="flex gap-2">
                      <button
                        v-if="rebut.statut === 'Brouillon'"
                        class="font-bold text-blue-800"
                        :disabled="rebutActionEnCours"
                        @click="modifierRebut(rebut)"
                      >
                        Modifier
                      </button>
                      <button
                        v-if="rebut.statut === 'Brouillon'"
                        class="font-bold text-emerald-700"
                        :disabled="rebutActionEnCours"
                        @click="validerRebut(rebut)"
                      >
                        Valider
                      </button>
                      <button
                        v-if="rebut.statut !== 'Annule'"
                        class="font-bold text-rose-700"
                        :disabled="rebutActionEnCours"
                        @click="annulerRebut(rebut)"
                      >
                        Annuler
                      </button>
                    </div>
                  </td>
                </tr>
                <tr v-if="!rebuts.length">
                  <td colspan="7" class="p-6 text-center font-bold uppercase tracking-wider text-slate-400">
                    Aucun rebut U2 déclaré sur cette période
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-50 flex justify-between items-center bg-slate-50/20">
          <div>
            <h3 class="text-xs font-black text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
              <Wrench class="w-4 h-4 text-blue-900" />
              Bilan Usinage 2
            </h3>
            <p class="mt-1 text-[10px] font-semibold text-slate-500">
              Réception U2 - Rebut U2 - Entrée OF
            </p>
          </div>
          <div v-if="bilansUsinage.length > 0" class="flex gap-2">
            <button
              @click="handlePrint"
              class="bg-slate-700 hover:bg-slate-800 text-white font-bold text-[10px] uppercase py-1.5 px-3 rounded-lg flex items-center gap-1.5 cursor-pointer"
            >
              <Printer class="w-3.5 h-3.5" />
              Imprimer
            </button>
            <button
              @click="loadBilansUsinage"
              class="bg-blue-900 hover:bg-blue-800 text-white font-bold text-[10px] uppercase py-1.5 px-3 rounded-lg flex items-center gap-1.5 cursor-pointer"
            >
              <RefreshCw class="w-3.5 h-3.5" />
              Recharger
            </button>
          </div>
        </div>

        <div v-if="isLoadingUsinage">
          <LoadingState />
        </div>

        <div v-else-if="!usinageError" class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr
                class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
              >
                <th class="p-3">Numéro OF</th>
                <th class="p-3">Code article</th>
                <th class="p-3">Désignation</th>
                <th class="p-3">Atelier</th>
                <th class="p-3">Type matière</th>
                <th class="p-3 text-right">Quantité réceptionnée U2 (pièces)</th>
                <th class="p-3 text-right">Rebut U2 (pièces)</th>
                <th class="p-3 text-right">Entrée OF signée (pièces)</th>
                <th class="p-3 text-right">Résultat Bilan U2 (pièces)</th>
                <th class="p-3">Statut</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 text-xs font-medium">
              <tr
                v-for="item in bilansUsinage"
                :key="`pieces-${item.numeroOF}-${item.codeArticle}-${item.typeMatiere ?? ''}`"
                class="hover:bg-slate-50/30 transition-colors"
              >
                <td class="p-3 font-black text-blue-900 font-mono whitespace-nowrap">{{ item.numeroOF }}</td>
                <td class="p-3 font-bold text-slate-800 font-mono">{{ item.codeArticle }}</td>
                <td class="p-3 text-slate-600 max-w-[180px] truncate">{{ item.designationArticle ?? '—' }}</td>
                <td class="p-3">{{ item.atelierOrigine ?? '—' }}</td>
                <td class="p-3">{{ item.typeMatiere ?? '—' }}</td>
                <td class="p-3 text-right font-mono text-blue-700 font-bold">
                  {{ formatNumber(item.quantiteReceptionneePieces, 0) }}
                </td>
                <td class="p-3 text-right font-mono">{{ formatNumber(item.rebutUsinage2Pieces, 0) }}</td>
                <td class="p-3 text-right font-mono">{{ formatNullable(item.entreeOfPieces) }}</td>
                <td class="p-3 text-right font-mono font-black text-blue-900">
                  {{ formatNullable(item.bilanUsinage2Pieces) }}
                </td>
                <td class="p-3"><BadgeStatus :status="getBilanUsinageStatus(item)" /></td>
              </tr>
              <tr v-if="!bilansUsinage.length">
                <td colspan="10" class="p-6 text-center text-slate-400">
                  <p class="font-bold uppercase tracking-wider">{{ usinageEmptyMessage }}</p>
                  <div class="mt-3 flex flex-wrap justify-center gap-2">
                    <RouterLink
                      class="rounded-lg bg-slate-900 px-3 py-2 text-[10px] font-black uppercase text-white hover:bg-slate-800"
                      to="/receptions-usinage-2"
                    >
                      Créer une réception
                    </RouterLink>
                    <button
                      type="button"
                      class="rounded-lg border border-slate-200 px-3 py-2 text-[10px] font-black uppercase text-slate-700 hover:bg-slate-50"
                      @click="loadBilansUsinage"
                    >
                      Recharger
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>

    <!-- ===================== ONGLET UAP2 TOTAL ===================== -->
    <template v-if="activeTab === 'uap2'">
      <BilanMatiereUap2View />
    </template>

  </div>
</template>
