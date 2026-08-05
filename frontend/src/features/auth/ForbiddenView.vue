<script setup lang="ts">
import { ShieldX } from 'lucide-vue-next';
import { useRouter } from 'vue-router';
import { useAuthStore } from './auth.store';
import { getDefaultRouteForRoles } from './auth.roles';

const router = useRouter();
const authStore = useAuthStore();

const returnToAllowedArea = () => router.replace(getDefaultRouteForRoles(authStore.roles));
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-slate-100 p-6">
    <section class="w-full max-w-lg rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-xl">
      <ShieldX class="mx-auto h-12 w-12 text-rose-600" aria-hidden="true" />
      <h1 class="mt-4 text-xl font-bold text-slate-900">Accès interdit</h1>
      <p class="mt-2 text-sm text-slate-600">
        Votre session reste active, mais votre rôle ne permet pas d’accéder à cet écran.
      </p>
      <button
        type="button"
        class="mt-6 rounded-xl bg-blue-700 px-5 py-3 text-sm font-bold text-white hover:bg-blue-800"
        @click="returnToAllowedArea"
      >
        Revenir à mon espace
      </button>
    </section>
  </main>
</template>
