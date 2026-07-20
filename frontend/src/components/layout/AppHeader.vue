<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { LogOut, User } from 'lucide-vue-next';
import { useAuthStore } from '@/features/auth/auth.store';
import AppButton from '@/components/ui/AppButton.vue';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const pageTitle = computed(() => (route.meta?.title as string) || 'Accueil');
const pageSubtitle = computed(() => (route.meta?.subtitle as string) || 'Vue générale');
const currentUser = computed(() => authStore.user);

const formatRoleLabel = (role: string | undefined): string => {
  if (!role) return 'Rôle non défini';
  const mapping: Record<string, string> = {
    Admin: 'Administrateur',
    ResponsableFonderie: 'Responsable Fonderie',
    ResponsableUsinage2: 'Responsable Usinage 2'
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
    class="app-header z-10 flex min-h-16 shrink-0 items-center justify-between gap-4 border-b px-4 py-3 sm:px-6 xl:px-8"
  >
    <div class="min-w-0">
      <div class="app-header__breadcrumb flex items-center gap-1.5 text-xs font-medium">
        <span class="text-primary-700">SOPAL</span><span class="text-[var(--color-border-strong)]" aria-hidden="true">/</span
        ><span class="truncate text-[var(--color-text)]">{{ pageTitle }}</span>
      </div>
      <p class="mt-0.5 truncate text-sm font-semibold text-[var(--color-text)]">{{ pageSubtitle }}</p>
    </div>

    <div class="flex shrink-0 items-center gap-2 sm:gap-4">
      <div class="app-header__user hidden items-center gap-3 pl-4 md:flex">
        <div
          class="app-header__avatar flex h-9 w-9 shrink-0 items-center justify-center rounded-lg border border-primary-100 bg-primary-50 text-primary-700"
        >
          <User class="h-4 w-4" aria-hidden="true" />
        </div>
        <div class="text-left">
          <p class="text-sm font-semibold leading-none text-slate-900">{{ currentUser?.fullName || 'Utilisateur' }}</p>
          <p class="mt-1 text-xs text-slate-500">{{ formatRoleLabel(currentUser?.role) }}</p>
        </div>
      </div>

      <AppButton
        variant="secondary"
        size="sm"
        title="Déconnexion sécurisée"
        aria-label="Se déconnecter de l’application"
        @click="handleLogout"
      >
        <template #icon><LogOut class="h-4 w-4" aria-hidden="true" /></template>
        <span class="hidden sm:inline">Déconnexion</span>
      </AppButton>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  border-color: rgb(203 221 234 / 0.88);
  background: rgb(255 255 255 / 0.96);
  box-shadow: var(--shadow-header);
  backdrop-filter: blur(12px);
}

.app-header__breadcrumb {
  color: var(--color-text-muted);
  letter-spacing: 0.01em;
}

.app-header__user {
  border-left: 1px solid var(--color-border);
}

.app-header__avatar {
  box-shadow: inset 0 0 0 1px rgb(255 255 255 / 0.7);
}
</style>
