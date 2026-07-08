<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '@/features/auth/auth.store';
import { LogOut, User } from 'lucide-vue-next';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const pageTitle = computed(() => {
  return (route.meta?.title as string) || 'Bilan Matière';
});

const pageSubtitle = computed(() => {
  return (route.meta?.subtitle as string) || "Gestion de l'exploitation industrielle";
});

const currentUser = computed(() => authStore.user);

const formatRoleLabel = (role: string | undefined): string => {
  if (!role) return '';
  const mapping: Record<string, string> = {
    Admin: 'Administrateur',
    ResponsableFonderie: 'Resp. Fonderie',
    ResponsableUsinage2: 'Resp. Usinage 2'
  };
  return mapping[role] || role;
};

const handleLogout = () => {
  authStore.logout();
  router.push('/login');
};
</script>

<template>
  <header
    class="h-16 bg-white border-b border-slate-200/80 flex items-center justify-between px-8 z-10 select-none shrink-0"
  >
    <!-- Breadcrumb & Page Info -->
    <div>
      <div class="flex items-center gap-1.5 text-[10px] text-slate-400 font-bold uppercase tracking-wider">
        <span>SOPAL</span>
        <span>/</span>
        <span class="text-blue-600">{{ pageTitle }}</span>
      </div>
      <h2 class="text-sm font-black text-slate-800 leading-tight tracking-tight">{{ pageSubtitle }}</h2>
    </div>

    <!-- Active User Profile & Actions -->
    <div class="flex items-center gap-6">
      <div class="flex items-center gap-3 pl-4 border-l border-slate-100">
        <div
          class="w-8 h-8 rounded-full bg-slate-50 border border-slate-150 flex items-center justify-center text-slate-600 shrink-0"
        >
          <User class="w-4 h-4 text-slate-400" />
        </div>
        <div class="text-left hidden sm:block">
          <p class="text-xs font-bold text-slate-800 leading-none">{{ currentUser?.fullName || 'Utilisateur' }}</p>
          <p class="text-[10px] font-semibold text-blue-600 mt-1">Rôle : {{ formatRoleLabel(currentUser?.role) }}</p>
        </div>
      </div>

      <button
        @click="handleLogout"
        class="flex items-center gap-1.5 px-3 py-1.5 bg-slate-50 border border-slate-200 text-slate-600 rounded-lg hover:bg-rose-50 hover:text-rose-600 hover:border-rose-200 transition-all text-xs font-bold cursor-pointer"
        title="Déconnexion sécurisée"
      >
        <LogOut class="w-3.5 h-3.5" />
        Déconnexion
      </button>
    </div>
  </header>
</template>
