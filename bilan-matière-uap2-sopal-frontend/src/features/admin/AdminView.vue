<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { Users, Settings, Package, Layers, Plus, CheckCircle2, UserPlus, Save } from 'lucide-vue-next';
import { adminService } from './admin.service';
import { AdminUser, AdminArticle, AdminSettings } from './admin.types';
import PageHeader from '@/components/ui/PageHeader.vue';
import FormField from '@/components/ui/FormField.vue';
import LoadingState from '@/components/ui/LoadingState.vue';
import { useAppDialog } from '@/composables/useAppDialog';

const activeTab = ref<'users' | 'articles' | 'types' | 'settings'>('users');
const dialog = useAppDialog();
const users = ref<AdminUser[]>([]);
const articles = ref<AdminArticle[]>([]);
const settings = ref<AdminSettings | null>(null);
const isLoading = ref(true);
const successMessage = ref('');

// Forms state
const formUser = ref({
  fullName: '',
  email: '',
  role: 'ResponsableFonderie' as 'Admin' | 'ResponsableFonderie' | 'ResponsableUsinage2',
  actif: true
});

const formArticle = ref({
  code: '',
  designation: '',
  typeMatiere: 'M3010L0',
  unite: 'KG',
  actif: true
});

const materialTypes = [
  { code: 'M3010L0', label: 'Laiton Sanitaire (Sans Plomb)', description: 'Usage sanitaire.', colorClass: 'text-blue-600' },
  { code: 'M3000L0', label: 'Laiton Standard Industriel', description: 'Usage standard.', colorClass: 'text-indigo-600' },
  { code: 'M3020L0', label: 'Type matière M3020L0', description: 'Type matière autorisé SOPAL.', colorClass: 'text-emerald-600' }
];

const loadAdminData = async () => {
  isLoading.value = true;
  try {
    const [uRes, aRes, sRes] = await Promise.all([
      adminService.getUsers(),
      adminService.getArticles(),
      adminService.getSettings()
    ]);
    users.value = uRes;
    articles.value = aRes;
    settings.value = sRes;
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const handleCreateUser = async () => {
  if (!formUser.value.fullName || !formUser.value.email) {
    await dialog.alert('Veuillez remplir tous les champs.', {
      title: 'Champs obligatoires',
      tone: 'warning'
    });
    return;
  }
  try {
    await adminService.createUser(formUser.value);
    formUser.value.fullName = '';
    formUser.value.email = '';
    successMessage.value = 'Utilisateur créé avec succès !';
    await loadAdminData();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const handleToggleUser = async (id: number) => {
  try {
    await adminService.toggleUserStatus(id);
    successMessage.value = 'Statut utilisateur mis à jour !';
    await loadAdminData();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const handleCreateArticle = async () => {
  if (!formArticle.value.code || !formArticle.value.designation) {
    await dialog.alert('Veuillez remplir tous les champs.', {
      title: 'Champs obligatoires',
      tone: 'warning'
    });
    return;
  }
  try {
    await adminService.createArticle(formArticle.value);
    formArticle.value.code = '';
    formArticle.value.designation = '';
    successMessage.value = 'Article créé avec succès !';
    await loadAdminData();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const handleToggleArticle = async (id: number) => {
  try {
    await adminService.toggleArticleStatus(id);
    successMessage.value = 'Statut article mis à jour !';
    await loadAdminData();
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

const handleSaveSettings = async () => {
  if (!settings.value) return;
  try {
    await adminService.updateSettings(settings.value);
    successMessage.value = "Paramètres d'exploitation enregistrés !";
    setTimeout(() => (successMessage.value = ''), 4000);
  } catch (err) {
    console.error(err);
  }
};

onMounted(() => {
  loadAdminData();
});
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <PageHeader title="Espace Administration" description="Utilisateurs, seuils et articles." />

    <!-- Notice -->
    <div
      v-if="successMessage"
      class="bg-emerald-50 border border-emerald-100 rounded-xl p-4 text-emerald-800 text-xs font-semibold flex items-center gap-3"
    >
      <CheckCircle2 class="w-5 h-5 text-emerald-500 shrink-0" />
      <span>{{ successMessage }}</span>
    </div>

    <!-- Tab navigation -->
    <div class="flex border-b border-slate-200 gap-1 select-none">
      <button
        @click="activeTab = 'users'"
        class="px-4 py-2 text-xs font-bold uppercase tracking-wider border-b-2 transition-all cursor-pointer flex items-center gap-2"
        :class="
          activeTab === 'users'
            ? 'border-blue-900 text-blue-900'
            : 'border-transparent text-slate-400 hover:text-slate-600'
        "
      >
        <Users class="w-4 h-4" />
        Utilisateurs & Rôles
      </button>

      <button
        @click="activeTab = 'articles'"
        class="px-4 py-2 text-xs font-bold uppercase tracking-wider border-b-2 transition-all cursor-pointer flex items-center gap-2"
        :class="
          activeTab === 'articles'
            ? 'border-blue-900 text-blue-900'
            : 'border-transparent text-slate-400 hover:text-slate-600'
        "
      >
        <Package class="w-4 h-4" />
        Articles d'Exploitation
      </button>

      <button
        @click="activeTab = 'types'"
        class="px-4 py-2 text-xs font-bold uppercase tracking-wider border-b-2 transition-all cursor-pointer flex items-center gap-2"
        :class="
          activeTab === 'types'
            ? 'border-blue-900 text-blue-900'
            : 'border-transparent text-slate-400 hover:text-slate-600'
        "
      >
        <Layers class="w-4 h-4" />
        Types Matière
      </button>

      <button
        @click="activeTab = 'settings'"
        class="px-4 py-2 text-xs font-bold uppercase tracking-wider border-b-2 transition-all cursor-pointer flex items-center gap-2"
        :class="
          activeTab === 'settings'
            ? 'border-blue-900 text-blue-900'
            : 'border-transparent text-slate-400 hover:text-slate-600'
        "
      >
        <Settings class="w-4 h-4" />
        Paramètres & Seuils
      </button>
    </div>

    <div v-if="isLoading">
      <LoadingState />
    </div>

    <!-- Active tabs content -->
    <div v-else class="space-y-6">
      <!-- TAB 1 : USERS & ROLES -->
      <div v-if="activeTab === 'users'" class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Add user card -->
        <div class="bg-white rounded-xl border border-slate-100 p-6 shadow-sm space-y-4 h-fit">
          <div class="flex items-center gap-2 border-b border-slate-50 pb-2">
            <UserPlus class="w-4.5 h-4.5 text-blue-900" />
            <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">Nouvel Utilisateur</h4>
          </div>

          <form @submit.prevent="handleCreateUser" class="space-y-4">
            <FormField id="u-name" label="Nom Complet" required>
              <input
                type="text"
                v-model="formUser.fullName"
                class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-semibold w-full focus:bg-white"
                placeholder="Nom Prénom"
                required
              />
            </FormField>

            <FormField id="u-email" label="Adresse Email" required>
              <input
                type="email"
                v-model="formUser.email"
                class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-semibold w-full focus:bg-white"
                placeholder="email@sopal.com"
                required
              />
            </FormField>

            <FormField id="u-role" label="Rôle Système" required>
              <select
                v-model="formUser.role"
                class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-semibold w-full focus:bg-white cursor-pointer"
                required
              >
                <option value="Admin">Administrateur</option>
                <option value="ResponsableFonderie">Responsable Fonderie</option>
                <option value="ResponsableUsinage2">Responsable Usinage 2</option>
              </select>
            </FormField>

            <button
              type="submit"
              class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-xl flex items-center justify-center gap-1.5 cursor-pointer transition-all"
            >
              <Plus class="w-4 h-4" />
              Créer l'utilisateur
            </button>
          </form>
        </div>

        <!-- Users list table -->
        <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col lg:col-span-2">
          <div class="px-6 py-4 border-b border-slate-50 bg-slate-50/20">
            <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">Liste des Comptes d'Accès</h4>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr
                  class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
                >
                  <th class="p-3">ID</th>
                  <th class="p-3">Nom complet</th>
                  <th class="p-3">Rôle Assigné</th>
                  <th class="p-3">Email</th>
                  <th class="p-3">Statut</th>
                  <th class="p-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 text-xs font-medium">
                <tr v-for="u in users" :key="u.id" class="hover:bg-slate-50/30 transition-colors">
                  <td class="p-3 font-mono text-slate-400">#{{ u.id }}</td>
                  <td class="p-3 font-bold text-slate-800">{{ u.fullName }}</td>
                  <td class="p-3">
                    <span
                      class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-blue-50 text-blue-700"
                    >
                      {{ u.role }}
                    </span>
                  </td>
                  <td class="p-3 text-slate-500 font-mono">{{ u.email }}</td>
                  <td class="p-3">
                    <span
                      class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-bold"
                      :class="
                        u.actif
                          ? 'bg-emerald-50 text-emerald-700 border border-emerald-100'
                          : 'bg-rose-50 text-rose-700 border border-rose-100'
                      "
                    >
                      <span class="w-1 h-1 rounded-full" :class="u.actif ? 'bg-emerald-500' : 'bg-rose-500'"></span>
                      {{ u.actif ? 'Actif' : 'Désactivé' }}
                    </span>
                  </td>
                  <td class="p-3 text-right">
                    <button
                      @click="handleToggleUser(u.id)"
                      class="px-2.5 py-1 rounded border text-[10px] font-bold cursor-pointer transition-colors"
                      :class="
                        u.actif
                          ? 'bg-rose-50 border-rose-150 text-rose-700 hover:bg-rose-100'
                          : 'bg-emerald-50 border-emerald-150 text-emerald-700 hover:bg-emerald-100'
                      "
                    >
                      {{ u.actif ? 'Désactiver' : 'Activer' }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- TAB 2 : ARTICLES -->
      <div v-if="activeTab === 'articles'" class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Add article form -->
        <div class="bg-white rounded-xl border border-slate-100 p-6 shadow-sm space-y-4 h-fit">
          <div class="flex items-center gap-2 border-b border-slate-50 pb-2">
            <Package class="w-4.5 h-4.5 text-blue-900" />
            <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">Nouvel Article</h4>
          </div>

          <form @submit.prevent="handleCreateArticle" class="space-y-4">
            <FormField id="a-code" label="Code Article" required>
              <input
                type="text"
                v-model="formArticle.code"
                class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-bold w-full focus:bg-white font-mono"
                placeholder="ART-0000"
                required
              />
            </FormField>

            <FormField id="a-des" label="Désignation" required>
              <input
                type="text"
                v-model="formArticle.designation"
                class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-semibold w-full focus:bg-white"
                placeholder="Désignation complète"
                required
              />
            </FormField>

            <FormField id="a-type" label="Type Matière" required>
              <select
                v-model="formArticle.typeMatiere"
                class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-semibold w-full focus:bg-white cursor-pointer"
                required
              >
                <option v-for="type in materialTypes" :key="type.code" :value="type.code">{{ type.code }}</option>
              </select>
            </FormField>

            <button
              type="submit"
              class="w-full bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-4 rounded-xl flex items-center justify-center gap-1.5 cursor-pointer transition-all"
            >
              <Plus class="w-4 h-4" />
              Créer l'article
            </button>
          </form>
        </div>

        <!-- Articles list table -->
        <div class="bg-white rounded-xl border border-slate-100 shadow-sm overflow-hidden flex flex-col lg:col-span-2">
          <div class="px-6 py-4 border-b border-slate-50 bg-slate-50/20">
            <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">Catalogue Articles Validés</h4>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr
                  class="bg-slate-50/50 border-b border-slate-100 text-[10px] text-slate-500 font-bold uppercase tracking-wider"
                >
                  <th class="p-3">ID</th>
                  <th class="p-3">Code Article</th>
                  <th class="p-3">Désignation</th>
                  <th class="p-3">Type Matière</th>
                  <th class="p-3">Unité</th>
                  <th class="p-3">Statut</th>
                  <th class="p-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 text-xs font-medium">
                <tr v-for="a in articles" :key="a.id" class="hover:bg-slate-50/30 transition-colors">
                  <td class="p-3 font-mono text-slate-400">#{{ a.id }}</td>
                  <td class="p-3 font-bold font-mono text-slate-800">{{ a.code }}</td>
                  <td class="p-3 text-slate-700 font-semibold">{{ a.designation }}</td>
                  <td class="p-3 font-bold font-mono text-blue-900">{{ a.typeMatiere }}</td>
                  <td class="p-3 font-mono text-slate-400">{{ a.unite }}</td>
                  <td class="p-3">
                    <span
                      class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-bold"
                      :class="
                        a.actif
                          ? 'bg-emerald-50 text-emerald-700 border border-emerald-100'
                          : 'bg-rose-50 text-rose-700 border border-rose-100'
                      "
                    >
                      <span class="w-1 h-1 rounded-full" :class="a.actif ? 'bg-emerald-500' : 'bg-rose-500'"></span>
                      {{ a.actif ? 'Actif' : 'Inactif' }}
                    </span>
                  </td>
                  <td class="p-3 text-right">
                    <button
                      @click="handleToggleArticle(a.id)"
                      class="px-2.5 py-1 rounded border text-[10px] font-bold cursor-pointer transition-colors"
                      :class="
                        a.actif
                          ? 'bg-rose-50 border-rose-150 text-rose-700 hover:bg-rose-100'
                          : 'bg-emerald-50 border-emerald-150 text-emerald-700 hover:bg-emerald-100'
                      "
                    >
                      {{ a.actif ? 'Désactiver' : 'Activer' }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- TAB 3 : MATERIAL TYPES -->
      <div v-if="activeTab === 'types'" class="bg-white rounded-xl border border-slate-100 p-6 shadow-sm space-y-4">
        <div class="flex items-center gap-2 border-b border-slate-50 pb-2">
          <Layers class="w-4.5 h-4.5 text-blue-900" />
          <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">Types Matière Autorisés</h4>
        </div>

        <p class="text-xs text-slate-500 font-semibold">Types matière validés SOPAL.</p>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-6 pt-2">
          <div
            v-for="type in materialTypes"
            :key="type.code"
            class="border border-slate-100 rounded-xl p-5 space-y-2 bg-slate-50/30"
          >
            <span class="text-[10px] font-bold uppercase tracking-wider" :class="type.colorClass">
              Code : {{ type.code }}
            </span>
            <h5 class="text-sm font-black text-slate-800 leading-tight">{{ type.label }}</h5>
            <p class="text-xs text-slate-400 font-medium">{{ type.description }}</p>
          </div>
        </div>

        <div
          class="bg-blue-50/50 border border-blue-100 p-4 rounded-xl flex items-center gap-3 mt-4 text-xs font-semibold text-blue-900"
        >
          <span>Ajout ou modification : validation SOPAL requise.</span>
        </div>
      </div>

      <!-- TAB 4 : SETTINGS -->
      <div
        v-if="activeTab === 'settings' && settings"
        class="bg-white rounded-xl border border-slate-100 p-6 shadow-sm space-y-6"
      >
        <div class="flex items-center gap-2 border-b border-slate-50 pb-2">
          <Settings class="w-4.5 h-4.5 text-blue-900" />
          <h4 class="text-xs font-black text-slate-700 uppercase tracking-wider">
            Configuration des Seuils d'Exploitation
          </h4>
        </div>

        <form @submit.prevent="handleSaveSettings" class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <FormField id="s-seuil" label="Seuil d'Alerte Écart (%)" required>
            <input
              type="number"
              step="0.1"
              v-model="settings.seuilAlerteEcart"
              class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-bold w-full focus:bg-white font-mono"
              required
            />
          </FormField>

          <FormField id="s-crit" label="Seuil Critique d'Écart (%)" required>
            <input
              type="number"
              step="0.1"
              v-model="settings.seuilCritiqueEcart"
              class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-bold w-full focus:bg-white font-mono"
              required
            />
          </FormField>

          <FormField id="s-tol" label="Tolérance d'Écart Poids (kg)" required>
            <input
              type="number"
              v-model="settings.toleranceAlertePoids"
              class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-bold w-full focus:bg-white font-mono"
              required
            />
          </FormField>

          <FormField id="s-freq" label="Fréquence de Synchronisation Sage X3" required>
            <select
              v-model="settings.frequenceSyncX3"
              class="bg-slate-50 border border-slate-100 rounded-lg p-2.5 text-xs font-semibold w-full focus:bg-white cursor-pointer"
              required
            >
              <option value="En temps réel">En temps réel (Webhook)</option>
              <option value="Toutes les heures">Toutes les heures</option>
              <option value="Toutes les 4 heures">Toutes les 4 heures</option>
              <option value="Une fois par jour">Une fois par jour</option>
            </select>
          </FormField>

          <div class="md:col-span-2 pt-4 border-t border-slate-50 flex justify-end">
            <button
              type="submit"
              class="bg-blue-900 hover:bg-blue-800 text-white font-bold text-xs uppercase tracking-wider py-2.5 px-6 rounded-xl flex items-center gap-1.5 cursor-pointer active:scale-95 transition-all"
            >
              <Save class="w-4 h-4" />
              Enregistrer les Paramètres
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
