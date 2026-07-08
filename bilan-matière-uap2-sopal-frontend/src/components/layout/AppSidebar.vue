<script setup lang="ts">
import { computed } from 'vue';
import { useAuthStore } from '@/features/auth/auth.store';
import logoUrl from '@/assets/sopal-logo.svg?url';
import {
  LayoutDashboard,
  Scale,
  ClipboardList,
  RefreshCw,
  Trash2,
  CornerDownRight,
  FileText,
  Shield,
  History,
  AlertTriangle
} from 'lucide-vue-next';

const authStore = useAuthStore();
const role = computed(() => authStore.userRole);

const menuItems = [
  {
    name: 'Tableau de bord',
    path: '/dashboard',
    icon: LayoutDashboard,
    roles: ['Admin']
  },
  {
    name: 'Bilan matière',
    path: '/bilan-matiere',
    icon: Scale,
    roles: ['Admin']
  },
  {
    name: 'Inventaire Fonderie',
    path: '/inventaire',
    icon: ClipboardList,
    roles: ['Admin']
  },
  {
    name: 'Sorties Magasin ERP: X3',
    path: '/sorties-x3',
    icon: RefreshCw,
    roles: ['Admin']
  },
  {
    name: 'Déchets fonderie',
    path: '/dechets-fonderie',
    icon: Trash2,
    roles: ['Admin', 'ResponsableFonderie']
  },
  {
    name: 'Réceptions Usinage 2',
    path: '/receptions-usinage-2',
    icon: CornerDownRight,
    roles: ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2']
  },
  {
    name: 'Rapports',
    path: '/rapports',
    icon: FileText,
    roles: ['Admin']
  },
  {
    name: 'Administration',
    path: '/admin',
    icon: Shield,
    roles: ['Admin']
  },
  {
    name: 'Alertes',
    path: '/alertes',
    icon: AlertTriangle,
    roles: ['Admin']
  },
  {
    name: "Journal d'audit",
    path: '/audit',
    icon: History,
    roles: ['Admin']
  }
];

const filteredMenu = computed(() => {
  if (!role.value) return [];
  return menuItems.filter((item) => item.roles.includes(role.value!));
});
</script>

<template>
  <aside
    class="w-64 bg-white flex flex-col shadow-xl shadow-slate-200/70 shrink-0 h-screen select-none text-slate-800 border-r border-slate-200"
  >
    <div class="p-5 border-b border-slate-200">
      <div class="space-y-2">
        <img :src="logoUrl" alt="SOPAL logo" class="block h-14 w-auto max-w-[210px] object-contain" />
        <div class="leading-tight">
          <h1 class="font-bold text-sm tracking-wider uppercase font-display">Bilan Matière</h1>
          <span class="text-blue-700 block text-[10px] font-bold uppercase tracking-wider">UAP2 Fonderie</span>
        </div>
      </div>
    </div>

    <nav class="flex-1 py-4 px-3 space-y-1 overflow-y-auto">
      <router-link
        v-for="item in filteredMenu"
        :key="item.path"
        :to="item.path"
        class="flex items-center gap-3 px-4 py-2.5 rounded-lg text-xs font-semibold tracking-wide transition-all duration-150 cursor-pointer"
        :class="
          $route.path === item.path
            ? 'bg-blue-700 text-white shadow-md shadow-blue-700/10'
            : 'text-slate-600 hover:bg-blue-50 hover:text-blue-800'
        "
      >
        <component :is="item.icon" class="w-4.5 h-4.5 opacity-80 shrink-0" />
        {{ item.name }}
      </router-link>
    </nav>

    <div class="p-4 border-t border-slate-200 bg-slate-50">
      <div class="flex items-center gap-2.5 px-3 py-2 bg-white border border-slate-100 rounded-xl">
        <div class="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></div>
        <div class="text-[10px] text-slate-500 font-bold uppercase tracking-wider leading-none">Mode Hors Ligne</div>
      </div>
    </div>
  </aside>
</template>
