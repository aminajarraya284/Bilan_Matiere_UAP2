<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { Loader2, ShieldCheck } from 'lucide-vue-next';
import { useAuthStore } from './auth.store';
import logoUrl from '@/assets/sopal-logo-mark-hd.png?url';
import buildingBgUrl from '@/assets/sopal-building-bg.webp?url';

const router = useRouter();
const authStore = useAuthStore();
const username = ref('admin');
const password = ref('admin');
const errorMsg = ref('');
const isLoading = ref(false);

const handleLogin = async () => {
  errorMsg.value = '';
  isLoading.value = true;
  try {
    const user = await authStore.login(username.value, password.value);
    if (user.role === 'Admin') router.push('/dashboard');
    else if (user.role === 'ResponsableFonderie') router.push('/dechets-fonderie');
    else if (user.role === 'ResponsableUsinage2') router.push('/receptions-usinage-2');
    else router.push('/dashboard');
  } catch (err: unknown) {
    errorMsg.value = err instanceof Error ? err.message : 'Identifiants de démonstration incorrects.';
  } finally {
    isLoading.value = false;
  }
};
</script>

<template>
  <div
    class="relative min-h-screen flex items-center justify-center overflow-hidden bg-slate-100 p-4 sm:p-6 select-none"
  >
    <div class="absolute inset-0 bg-cover bg-center" :style="{ backgroundImage: `url(${buildingBgUrl})` }"></div>
    <div class="absolute inset-0 bg-white/10 backdrop-blur-[0.75px]"></div>
    <div class="absolute inset-0 bg-gradient-to-br from-white/15 via-transparent to-sky-900/10"></div>

    <div
      class="relative w-full max-w-md bg-white/95 rounded-2xl border border-white shadow-2xl shadow-slate-900/15 overflow-hidden"
    >
      <div class="bg-white p-8 text-center space-y-4 relative border-b border-slate-100">
        <div class="mx-auto flex h-20 w-20 items-center justify-center rounded-2xl bg-white p-2 shadow-sm">
          <img :src="logoUrl" alt="Logo SOPAL" class="block h-full w-full object-contain" />
        </div>
        <div>
          <h1 class="text-xl font-bold tracking-tight text-slate-900 font-display">Bilan Matière UAP2</h1>
          <p class="mt-1 text-[11px] font-bold text-blue-700 uppercase tracking-[0.14em]">UAP2 · SOPAL</p>
        </div>
      </div>

      <div class="p-8 space-y-6">
        <div class="text-center space-y-1">
          <h2 class="text-base font-bold text-slate-800 tracking-tight">Connexion interne</h2>
          <p class="text-xs text-slate-500 font-medium">Fonderie &amp; Usinage 2</p>
        </div>

        <div class="bg-blue-50/70 border border-blue-100 p-3.5 rounded-xl space-y-2">
          <p
            class="flex items-center gap-1.5 text-[10px] font-bold text-blue-700 uppercase tracking-[0.08em] leading-none"
          >
            <ShieldCheck class="w-3.5 h-3.5 text-blue-700" />
            Profils d'accès de démonstration :
          </p>
          <div class="grid grid-cols-1 gap-1 text-[10px] text-slate-700 font-semibold font-mono">
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
              class="w-full bg-slate-50 border border-slate-200 rounded-xl text-xs p-3 font-semibold focus:border-blue-600 focus:bg-white"
              placeholder="Ex: admin"
            />
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600">Mot de passe</label>
            <input
              v-model="password"
              type="password"
              required
              class="w-full bg-slate-50 border border-slate-200 rounded-xl text-xs p-3 font-semibold focus:border-blue-600 focus:bg-white"
              placeholder="********"
            />
          </div>

          <div
            v-if="errorMsg"
            class="p-3 bg-rose-50 border border-rose-100 rounded-xl text-rose-700 text-[11px] font-semibold text-center"
          >
            {{ errorMsg }}
          </div>

          <button
            type="submit"
            :disabled="isLoading"
            class="w-full bg-blue-700 hover:bg-blue-800 active:scale-98 transition-all text-white font-bold text-sm tracking-wide py-3 px-4 rounded-xl flex items-center justify-center gap-2 cursor-pointer shadow-md shadow-blue-900/10 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <Loader2 v-if="isLoading" class="w-4 h-4 animate-spin text-white" />
            <span v-else>Se connecter</span>
          </button>
        </form>
      </div>
    </div>
  </div>
</template>
