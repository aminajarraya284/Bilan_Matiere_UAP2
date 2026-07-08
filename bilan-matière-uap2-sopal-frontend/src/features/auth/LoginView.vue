<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from './auth.store';
import { ShieldCheck, Loader2 } from 'lucide-vue-next';
import logoUrl from '@/assets/sopal-logo.svg?url';
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
    if (user.role === 'Admin') {
      router.push('/dashboard');
    } else if (user.role === 'ResponsableFonderie') {
      router.push('/dechets-fonderie');
    } else if (user.role === 'ResponsableUsinage2') {
      router.push('/receptions-usinage-2');
    } else {
      router.push('/dashboard');
    }
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
    <div class="absolute inset-0 bg-white/70 backdrop-blur-[1px]"></div>
    <div class="absolute inset-0 bg-gradient-to-br from-white via-white/85 to-sky-100/70"></div>

    <div
      class="relative w-full max-w-md bg-white/95 rounded-2xl border border-white shadow-2xl shadow-slate-900/15 overflow-hidden"
    >
      <div class="bg-white p-8 text-center space-y-4 relative border-b border-slate-100">
        <img :src="logoUrl" alt="SOPAL" class="mx-auto block h-20 w-auto max-w-[320px] object-contain" />
        <div>
          <h1 class="text-lg font-black tracking-wider uppercase text-slate-900 font-display">
            Bilan Matière Fonderie
          </h1>
          <p class="text-[10px] font-bold text-blue-700 uppercase tracking-widest mt-0.5">UAP2 - SOPAL</p>
        </div>
      </div>

      <div class="p-8 space-y-6">
        <div class="text-center space-y-1">
          <h2 class="text-sm font-black text-slate-800 uppercase tracking-wider">Connexion Interne</h2>
          <p class="text-xs text-slate-500 font-medium">Fonderie & Usinage 2</p>
        </div>

        <div class="bg-blue-50/70 border border-blue-100 p-3.5 rounded-xl space-y-2">
          <p
            class="text-[9px] font-extrabold text-blue-700 uppercase tracking-wider flex items-center gap-1.5 leading-none"
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

        <form @submit.prevent="handleLogin" class="space-y-4">
          <div class="space-y-1.5">
            <label class="block text-[10px] font-black text-slate-500 uppercase tracking-wider">Identifiant</label>
            <input
              v-model="username"
              type="text"
              required
              class="w-full bg-slate-50 border border-slate-200 rounded-xl text-xs p-3 font-semibold focus:border-blue-600 focus:bg-white"
              placeholder="Ex: admin"
            />
          </div>

          <div class="space-y-1.5">
            <label class="block text-[10px] font-black text-slate-500 uppercase tracking-wider">Mot de passe</label>
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
            class="w-full bg-blue-800 hover:bg-blue-700 active:scale-98 transition-all text-white font-bold text-xs uppercase tracking-wider py-3 px-4 rounded-xl flex items-center justify-center gap-2 cursor-pointer shadow-md shadow-blue-900/10"
          >
            <Loader2 v-if="isLoading" class="w-4 h-4 animate-spin text-white" />
            <span v-else>Se connecter</span>
          </button>
        </form>
      </div>
    </div>
  </div>
</template>
