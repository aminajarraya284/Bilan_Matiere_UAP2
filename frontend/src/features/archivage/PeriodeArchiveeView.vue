<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { Download, Lock } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { archivageService } from './archivage.service';
import type { ArchiveDetail, ArchivedBilan } from './archivage.types';

const route = useRoute();
const periode = String(route.params.periode ?? '');
const archive = ref<ArchiveDetail | null>(null);
const loading = ref(true);
const error = ref('');
const bilans = (detail: ArchiveDetail): { nom: string; unite: string; data: ArchivedBilan }[] => [
  { nom: 'Fonderie', unite: 'kg', data: detail.snapshot.fonderie },
  { nom: 'Usinage 2', unite: 'pièces', data: detail.snapshot.usinage2 },
  { nom: 'UAP2', unite: 'pièces', data: detail.snapshot.uap2 }
];
onMounted(async () => { try { archive.value = await archivageService.get(periode); } catch { error.value = 'Archive introuvable ou inaccessible.'; } finally { loading.value = false; } });
</script>

<template>
  <div class="space-y-6">
    <PageHeader :title="`Période archivée — ${periode}`" description="Snapshot SQL officiel ; aucune donnée n’est recalculée." />
    <LoadingState v-if="loading" />
    <div v-else-if="error" class="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{{ error }}</div>
    <template v-else-if="archive">
      <section class="rounded-xl border border-blue-200 bg-blue-50 p-5"><div class="flex items-center gap-2 font-bold text-blue-900"><Lock class="h-5 w-5" />Lecture seule</div><dl class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4"><div><dt>Période</dt><dd>{{ archive.metadata.periode }}</dd></div><div><dt>Enregistré par</dt><dd>{{ archive.metadata.enregistrePar }}</dd></div><div><dt>Statut</dt><dd>{{ archive.metadata.statutGlobal }}</dd></div><div><dt>Version</dt><dd>{{ archive.metadata.versionCalcul }}</dd></div></dl><button class="mt-4 inline-flex items-center gap-2 rounded-lg bg-emerald-700 px-4 py-2 text-xs font-bold text-white" @click="archivageService.telechargerRapport(periode)"><Download class="h-4 w-4" />Télécharger le rapport</button></section>
      <section class="grid gap-4 lg:grid-cols-3"><article v-for="bilan in bilans(archive)" :key="bilan.nom" class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm"><h2 class="font-black text-slate-900">Bilan {{ bilan.nom }}</h2><p class="mt-1 text-xs text-slate-500">Unité : {{ bilan.unite }}</p><dl class="mt-4 grid grid-cols-2 gap-3 text-sm"><div><dt>Lignes</dt><dd>{{ bilan.data.nombreLignes }}</dd></div><div><dt>Anomalies</dt><dd>{{ bilan.data.nombreAnomalies }}</dd></div><div><dt>Données manquantes</dt><dd>{{ bilan.data.nombreLignesDonneesManquantes }}</dd></div></dl></article></section>
      <section class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm"><h2 class="font-black">Paramètres utilisés</h2><dl class="mt-4 grid gap-3 sm:grid-cols-2"><div v-for="(value, key) in archive.snapshot.parametresCalcul" :key="key"><dt class="text-xs font-bold text-slate-500">{{ key }}</dt><dd>{{ value ?? '—' }}</dd></div></dl></section>
      <section class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm"><h2 class="font-black">Anomalies archivées</h2><p v-if="!archive.snapshot.anomalies.length" class="mt-3 text-sm text-slate-500">Aucune anomalie.</p><ul v-else class="mt-3 list-disc space-y-1 pl-5 text-sm"><li v-for="item in archive.snapshot.anomalies" :key="item">{{ item }}</li></ul></section>
    </template>
  </div>
</template>
