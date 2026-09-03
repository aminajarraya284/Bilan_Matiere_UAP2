<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { CheckCircle2, ExternalLink, Layers, Package, Settings, ShieldCheck, Users } from 'lucide-vue-next';
import PageHeader from '@/components/ui/PageHeader.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { useAppDialog } from '@/composables/useAppDialog';
import { adminService } from './admin.service';
import type {
  AdminDevelopmentAccount,
  AdminMaterialType,
  AdminParameter,
  AdminUserProfile
} from './admin.types';

type AdminTab = 'users' | 'references' | 'types' | 'settings';

const activeTab = ref<AdminTab>('users');
const dialog = useAppDialog();
const parameters = ref<AdminParameter[]>([]);
const parameterDrafts = ref<Record<string, string>>({});
const materialTypes = ref<AdminMaterialType[]>([]);
const users = ref<AdminUserProfile[]>([]);
const usersTotal = ref(0);
const developmentAccounts = ref<AdminDevelopmentAccount[]>([]);
const isLoadingUsers = ref(true);
const usersError = ref('');
const togglingUserId = ref<number | null>(null);
const isLoadingParameters = ref(true);
const isLoadingMaterialTypes = ref(true);
const parametersError = ref('');
const materialTypesError = ref('');
const successMessage = ref('');

const formatDateTime = (value: string | null): string =>
  value
    ? new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short', timeStyle: 'short', timeZone: 'UTC' }).format(
        new Date(value)
      )
    : '—';

const loadUsers = async () => {
  isLoadingUsers.value = true;
  usersError.value = '';
  try {
    const page = await adminService.getUsers({ page: 1, pageSize: 25 });
    users.value = page.items;
    usersTotal.value = page.total;
  } catch {
    // Aucun repli sur des données de démonstration : une erreur reste une erreur.
    users.value = [];
    usersTotal.value = 0;
    usersError.value = 'Impossible de charger les profils utilisateurs.';
  } finally {
    isLoadingUsers.value = false;
  }
};

const loadDevelopmentAccounts = async () => {
  try {
    developmentAccounts.value = await adminService.getDevelopmentAccounts();
  } catch {
    // 404 hors Development : il n'y a simplement pas de compte local, ce n'est pas une panne.
    developmentAccounts.value = [];
  }
};

const handleToggleUser = async (user: AdminUserProfile) => {
  togglingUserId.value = user.id;
  usersError.value = '';
  try {
    const updated = await adminService.setUserActivation(user.id, !user.estActif);
    const index = users.value.findIndex((item) => item.id === updated.id);
    if (index >= 0) users.value[index] = updated;
    successMessage.value = updated.estActif ? 'Accès activé.' : 'Accès désactivé.';
  } catch {
    usersError.value = "La modification de l'accès a échoué.";
  } finally {
    togglingUserId.value = null;
  }
};

const loadParameters = async () => {
  isLoadingParameters.value = true;
  parametersError.value = '';
  try {
    parameters.value = await adminService.getParameters();
    parameterDrafts.value = Object.fromEntries(
      parameters.value.map((parameter) => [parameter.cle, parameter.valeur])
    );
  } catch {
    parameters.value = [];
    parametersError.value = 'Impossible de charger les paramètres de l’application.';
  } finally {
    isLoadingParameters.value = false;
  }
};

const loadMaterialTypes = async () => {
  isLoadingMaterialTypes.value = true;
  materialTypesError.value = '';
  try {
    materialTypes.value = await adminService.getMaterialTypes();
  } catch {
    materialTypes.value = [];
    materialTypesError.value = 'Impossible de charger les types matière.';
  } finally {
    isLoadingMaterialTypes.value = false;
  }
};

const handleSaveParameter = async (parameter: AdminParameter) => {
  try {
    const updated = await adminService.updateParameter(
      parameter.cle,
      parameterDrafts.value[parameter.cle] ?? ''
    );
    const index = parameters.value.findIndex((item) => item.cle === updated.cle);
    if (index >= 0) parameters.value[index] = updated;
    parameterDrafts.value[updated.cle] = updated.valeur;
    successMessage.value = 'Paramètre ' + updated.cle + ' enregistré.';
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (error) {
    await dialog.alert(
      error instanceof Error ? error.message : 'Modification du paramètre impossible.',
      { title: 'Erreur de validation', tone: 'danger' }
    );
  }
};

onMounted(() => {
  void loadUsers();
  void loadDevelopmentAccounts();
  void loadParameters();
  void loadMaterialTypes();
});
</script>

<template>
  <div class="space-y-6">
    <PageHeader
      title="Administration"
      description="Gestion des paramètres, utilisateurs et références de l'application."
    />

    <div
      v-if="successMessage"
      class="flex items-center gap-3 rounded-xl border border-emerald-100 bg-emerald-50 p-4 text-xs font-semibold text-emerald-800"
    >
      <CheckCircle2 class="h-5 w-5 shrink-0 text-emerald-500" />
      <span>{{ successMessage }}</span>
    </div>

    <div class="app-tabs select-none">
      <button class="app-tab" :class="{ 'app-tab--active': activeTab === 'users' }" @click="activeTab = 'users'">
        <Users class="h-4 w-4" />
        Utilisateurs & rôles
      </button>
      <button
        class="app-tab"
        :class="{ 'app-tab--active': activeTab === 'references' }"
        @click="activeTab = 'references'"
      >
        <Package class="h-4 w-4" />
        Références X3
      </button>
      <button class="app-tab" :class="{ 'app-tab--active': activeTab === 'types' }" @click="activeTab = 'types'">
        <Layers class="h-4 w-4" />
        Types matière
      </button>
      <button
        class="app-tab"
        :class="{ 'app-tab--active': activeTab === 'settings' }"
        @click="activeTab = 'settings'"
      >
        <Settings class="h-4 w-4" />
        Paramètres & seuils
      </button>
    </div>

    <section v-if="activeTab === 'users'" class="space-y-6">
      <div class="rounded-xl border border-blue-100 bg-blue-50/50 p-4 text-sm leading-6 text-blue-900">
        <p class="flex items-start gap-2">
          <ShieldCheck class="mt-0.5 h-4 w-4 shrink-0 text-blue-700" aria-hidden="true" />
          <span>
            Les identités et les rôles sont gérés dans <strong>Microsoft Entra ID</strong>. Un profil est créé
            automatiquement à la première connexion. L'application permet uniquement d'activer ou de désactiver
            l'accès local. Aucun mot de passe n'est stocké dans l'application.
          </span>
        </p>
      </div>

      <section class="space-y-4 rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <header class="flex items-center justify-between gap-4">
          <h2 class="flex items-center gap-2 text-base font-black text-slate-900">
            <Users class="h-5 w-5 text-slate-500" aria-hidden="true" />
            Profils Entra
          </h2>
          <span class="text-xs font-semibold text-slate-500">{{ usersTotal }} profil(s)</span>
        </header>

        <LoadingState v-if="isLoadingUsers" />
        <p v-else-if="usersError" class="rounded-lg bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-800">
          {{ usersError }}
        </p>
        <p v-else-if="!users.length" class="rounded-lg bg-slate-50 px-4 py-3 text-sm text-slate-600">
          Aucun profil pour l'instant. Le premier profil apparaîtra dès qu'un utilisateur Entra se connectera.
        </p>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th class="py-2 pr-4">Nom</th>
                <th class="py-2 pr-4">Email</th>
                <th class="py-2 pr-4">Rôle Entra</th>
                <th class="py-2 pr-4">Statut</th>
                <th class="py-2 pr-4">Dernière connexion</th>
                <th class="py-2">Action</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr v-for="user in users" :key="user.id">
                <td class="py-2 pr-4 font-semibold text-slate-900">{{ user.nomAffichage ?? '—' }}</td>
                <td class="py-2 pr-4 text-slate-600">{{ user.email ?? '—' }}</td>
                <td class="py-2 pr-4 text-slate-600">{{ user.roleObserve ?? '—' }}</td>
                <td class="py-2 pr-4">
                  <span
                    class="rounded-full px-2 py-0.5 text-xs font-bold"
                    :class="user.estActif ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-700'"
                  >
                    {{ user.estActif ? 'Actif' : 'Inactif' }}
                  </span>
                </td>
                <td class="py-2 pr-4 text-slate-600">{{ formatDateTime(user.derniereConnexionUtc) }}</td>
                <td class="py-2">
                  <button
                    type="button"
                    class="rounded-lg border border-slate-300 px-3 py-1 text-xs font-bold text-slate-700 hover:bg-slate-50 disabled:opacity-50"
                    :disabled="togglingUserId === user.id"
                    @click="handleToggleUser(user)"
                  >
                    {{ user.estActif ? 'Désactiver' : 'Activer' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section
        v-if="developmentAccounts.length"
        class="space-y-4 rounded-xl border border-amber-200 bg-amber-50/50 p-6 shadow-sm"
      >
        <header>
          <h2 class="text-base font-black text-amber-950">Comptes de développement</h2>
          <p class="mt-1 text-sm text-amber-900">
            Connexion locale, disponible uniquement dans cet environnement. En lecture seule : ces comptes sont
            définis par la configuration et leurs mots de passe vivent dans les User Secrets du poste.
          </p>
        </header>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="text-xs uppercase tracking-wide text-amber-800">
              <tr>
                <th class="py-2 pr-4">Identifiant</th>
                <th class="py-2 pr-4">Nom</th>
                <th class="py-2 pr-4">Rôle</th>
                <th class="py-2 pr-4">Type</th>
                <th class="py-2">Statut</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-amber-100">
              <tr v-for="account in developmentAccounts" :key="account.username">
                <td class="py-2 pr-4 font-mono text-xs font-bold text-amber-950">{{ account.username }}</td>
                <td class="py-2 pr-4 text-amber-900">{{ account.nomAffichage }}</td>
                <td class="py-2 pr-4 text-amber-900">{{ account.role }}</td>
                <td class="py-2 pr-4 text-amber-900">Développement</td>
                <td class="py-2">
                  <span
                    class="rounded-full px-2 py-0.5 text-xs font-bold"
                    :class="account.estConfigure ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-800'"
                  >
                    {{ account.estConfigure ? 'Actif' : 'Secret non configuré' }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </section>

    <section
      v-else-if="activeTab === 'references'"
      class="rounded-xl border border-blue-100 bg-blue-50/50 p-6 shadow-sm"
    >
      <div class="flex items-start gap-3">
        <ShieldCheck class="mt-0.5 h-5 w-5 shrink-0 text-blue-800" />
        <div>
          <h2 class="text-sm font-black text-blue-950">Références de fabrication X3</h2>
          <p class="mt-1 text-xs font-medium leading-5 text-blue-900">
            Les gammes et nomenclatures proviennent des imports Sage X3. Elles sont consultables ici en lecture
            seule afin de préserver la source de référence.
          </p>
          <p class="mt-2 text-xs font-bold text-blue-800">Source : données X3 importées · Lecture seule</p>
          <RouterLink
            to="/references-fabrication"
            class="mt-4 inline-flex items-center gap-2 rounded-xl bg-blue-900 px-4 py-2.5 text-xs font-bold text-white hover:bg-blue-800"
          >
            Ouvrir les références de fabrication
            <ExternalLink class="h-4 w-4" />
          </RouterLink>
        </div>
      </div>
    </section>

    <section v-else-if="activeTab === 'types'" class="space-y-4 rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <div class="flex items-center gap-2 border-b border-slate-100 pb-3">
        <Layers class="h-4 w-4 text-blue-900" />
        <div>
          <h2 class="text-sm font-black text-slate-800">Types matière</h2>
          <p class="mt-1 text-xs text-slate-500">Liste issue du référentiel applicatif.</p>
        </div>
      </div>

      <LoadingState v-if="isLoadingMaterialTypes" />
      <div v-else-if="materialTypesError" class="rounded-xl border border-rose-100 bg-rose-50 p-4 text-xs font-semibold text-rose-700">
        {{ materialTypesError }}
      </div>
      <p v-else-if="materialTypes.length === 0" class="rounded-xl border border-slate-200 p-6 text-center text-sm text-slate-500">
        Aucun type matière configuré.
      </p>
      <div v-else class="grid grid-cols-1 gap-4 md:grid-cols-3">
        <article v-for="type in materialTypes" :key="type.id" class="rounded-xl border border-slate-200 bg-slate-50/40 p-5">
          <div class="flex items-start justify-between gap-3">
            <p class="font-mono text-xs font-black text-blue-900">{{ type.code }}</p>
            <span
              class="rounded-full px-2 py-1 text-[10px] font-bold"
              :class="type.estActif ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-200 text-slate-600'"
            >
              {{ type.estActif ? 'Actif' : 'Inactif' }}
            </span>
          </div>
          <p class="mt-2 text-sm font-bold text-slate-800">{{ type.libelle }}</p>
        </article>
      </div>
    </section>

    <section v-else class="space-y-5 rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <div class="flex items-center gap-2 border-b border-slate-100 pb-3">
        <Settings class="h-4 w-4 text-blue-900" />
        <div>
          <h2 class="text-sm font-black text-slate-800">Paramètres & seuils</h2>
          <p class="mt-1 text-xs text-slate-500">Configuration persistée par l’API d’administration.</p>
        </div>
      </div>

      <LoadingState v-if="isLoadingParameters" />
      <div v-else-if="parametersError" class="rounded-xl border border-rose-100 bg-rose-50 p-4 text-xs font-semibold text-rose-700">
        {{ parametersError }}
      </div>
      <p v-else-if="parameters.length === 0" class="rounded-xl border border-slate-200 p-6 text-center text-sm text-slate-500">
        Aucun paramètre applicatif configuré.
      </p>
      <div v-else class="grid grid-cols-1 gap-4">
        <article v-for="parameter in parameters" :key="parameter.cle" class="rounded-xl border border-slate-200 p-4">
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p class="font-mono text-xs font-black text-blue-900">{{ parameter.cle }}</p>
              <p class="mt-1 text-xs text-slate-500">{{ parameter.description }}</p>
            </div>
            <span class="rounded-full bg-slate-100 px-2 py-1 text-[10px] font-bold text-slate-600">
              {{ parameter.typeValeur }}
            </span>
          </div>
          <div class="mt-3 flex flex-wrap gap-2">
            <input
              v-model="parameterDrafts[parameter.cle]"
              :disabled="!parameter.modifiable"
              class="min-w-64 flex-1 rounded-lg border border-slate-200 px-3 py-2 text-sm font-semibold disabled:bg-slate-100"
            />
            <button
              v-if="parameter.modifiable"
              type="button"
              class="rounded-lg bg-blue-900 px-4 py-2 text-xs font-bold text-white hover:bg-blue-800"
              @click="handleSaveParameter(parameter)"
            >
              Enregistrer
            </button>
          </div>
        </article>
      </div>
    </section>
  </div>
</template>
