<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Loader2, LogIn, ShieldCheck } from 'lucide-vue-next';
import { useAuthStore } from './auth.store';
import { consumeAuthReturnPath } from './auth.navigation';
import { getDefaultRouteForRoles } from './auth.roles';
import logoUrl from '@/assets/sopal-logo-mark-hd.png?url';
import buildingBgUrl from '@/assets/sopal-building-bg.webp?url';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const username = ref('admin');
const password = ref('admin');
const localError = ref('');

const isEntra = computed(() => authStore.mode === 'entra');
const isLoading = computed(() => authStore.loading);
const routeError = computed(() =>
  route.query.error === 'session_expired' ? 'Votre session a expiré. Veuillez vous reconnecter.' : ''
);
const errorMessage = computed(() => localError.value || authStore.error || routeError.value);

onMounted(async () => {
  if (!authStore.initialized) {
    try {
      await authStore.initialize();
    } catch {
      // Le store expose déjà un message sûr et compréhensible.
    }
  }
});

const redirectAfterDevelopmentLogin = async (): Promise<void> => {
  const queryRedirect = typeof route.query.redirect === 'string' ? route.query.redirect : null;
  const safeQueryRedirect = queryRedirect?.startsWith('/') && !queryRedirect.startsWith('//') ? queryRedirect : null;
  const destination = consumeAuthReturnPath() ?? safeQueryRedirect ?? getDefaultRouteForRoles(authStore.roles);
  await router.push(destination);
};

const handleLogin = async () => {
  localError.value = '';
  try {
    const user = await authStore.login(username.value, password.value);
    if (user) await redirectAfterDevelopmentLogin();
    // Entra utilise loginRedirect : la navigation est prise en charge par MSAL.
  } catch (error: unknown) {
    localError.value = error instanceof Error ? error.message : "Impossible d’ouvrir la session.";
  }
};
</script>

<template>
  <div class="relative flex min-h-screen select-none items-center justify-center overflow-hidden bg-slate-100 p-4 sm:p-6">
    <div class="absolute inset-0 bg-cover bg-center" :style="{ backgroundImage: `url(${buildingBgUrl})` }"></div>
    <div class="absolute inset-0 bg-white/10 backdrop-blur-[0.75px]"></div>
    <div class="absolute inset-0 bg-gradient-to-br from-white/15 via-transparent to-sky-900/10"></div>

    <div class="relative w-full max-w-md overflow-hidden rounded-2xl border border-white bg-white/95 shadow-2xl shadow-slate-900/15">
      <div class="relative space-y-4 border-b border-slate-100 bg-white p-8 text-center">
        <div class="mx-auto flex h-20 w-20 items-center justify-center rounded-2xl bg-white p-2 shadow-sm">
          <img :src="logoUrl" alt="Logo SOPAL" class="block h-full w-full object-contain" />
        </div>
        <div>
          <h1 class="font-display text-xl font-bold tracking-tight text-slate-900">Bilan Matière UAP2</h1>
          <p class="mt-1 text-[11px] font-bold uppercase tracking-[0.14em] text-blue-700">UAP2 · SOPAL</p>
        </div>
      </div>

      <div class="space-y-6 p-8">
        <div class="space-y-1 text-center">
          <h2 class="text-base font-bold tracking-tight text-slate-800">
            {{ isEntra ? 'Connexion Microsoft Entra ID' : 'Connexion interne' }}
          </h2>
          <p class="text-xs font-medium text-slate-500">Fonderie &amp; Usinage 2</p>
        </div>

        <div
          v-if="errorMessage"
          role="alert"
          class="rounded-xl border border-rose-100 bg-rose-50 p-3 text-center text-[11px] font-semibold text-rose-700"
        >
          {{ errorMessage }}
        </div>

        <template v-if="authStore.configurationError">
          <p class="text-center text-xs leading-5 text-slate-600">
            Corrigez les variables d’environnement puis rechargez l’application.
          </p>
        </template>

        <template v-else-if="isEntra">
          <p class="text-center text-xs leading-5 text-slate-600">
            Utilisez votre compte professionnel SOPAL. Aucun mot de passe n’est saisi dans cette application.
          </p>
          <button
            type="button"
            :disabled="isLoading"
            class="flex w-full cursor-pointer items-center justify-center gap-2 rounded-xl bg-blue-700 px-4 py-3 text-sm font-bold tracking-wide text-white shadow-md shadow-blue-900/10 transition-all hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60"
            @click="handleLogin"
          >
            <Loader2 v-if="isLoading" class="h-4 w-4 animate-spin" />
            <LogIn v-else class="h-4 w-4" />
            <span>Se connecter avec Microsoft</span>
          </button>
        </template>

        <template v-else>
          <div class="space-y-2 rounded-xl border border-blue-100 bg-blue-50/70 p-3.5">
            <p class="flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-[0.08em] text-blue-700">
              <ShieldCheck class="h-3.5 w-3.5" />
              Profils d’accès de développement
            </p>
            <div class="grid grid-cols-1 gap-1 font-mono text-[10px] font-semibold text-slate-700">
              <div>admin : Administrateur SOPAL</div>
              <div>fonderie : Responsable Fonderie</div>
              <div>usinage2 : Responsable Usinage 2</div>
            </div>
          </div>

          <form class="space-y-4" @submit.prevent="handleLogin">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600">Identifiant</label>
              <input
                v-model="username"
                type="text"
                required
                class="w-full rounded-xl border border-slate-200 bg-slate-50 p-3 text-xs font-semibold focus:border-blue-600 focus:bg-white"
                placeholder="Ex: admin"
              />
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600">Mot de passe</label>
              <input
                v-model="password"
                type="password"
                required
                class="w-full rounded-xl border border-slate-200 bg-slate-50 p-3 text-xs font-semibold focus:border-blue-600 focus:bg-white"
                placeholder="********"
              />
            </div>
            <button
              type="submit"
              :disabled="isLoading"
              class="flex w-full cursor-pointer items-center justify-center gap-2 rounded-xl bg-blue-700 px-4 py-3 text-sm font-bold tracking-wide text-white shadow-md shadow-blue-900/10 transition-all hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <Loader2 v-if="isLoading" class="h-4 w-4 animate-spin" />
              <span v-else>Se connecter</span>
            </button>
          </form>
        </template>
      </div>
    </div>
  </div>
</template>
