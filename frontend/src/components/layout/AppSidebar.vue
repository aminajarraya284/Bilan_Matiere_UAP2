<script setup lang="ts">
import { computed } from 'vue';
import { useAuthStore } from '@/features/auth/auth.store';
import logoUrl from '@/assets/sopal-logo-mark-hd.png?url';
import { getGlobalMode, GLOBAL_MODE_LABELS } from '@/config/dataSources';
import {
  LayoutDashboard,
  Scale,
  ClipboardList,
  RefreshCw,
  FileSpreadsheet,
  Layers3,
  Trash2,
  CornerDownRight,
  FileText,
  Shield,
  History,
  AlertTriangle,
  Gauge,
  FileArchive
} from 'lucide-vue-next';

const authStore = useAuthStore();
const roles = computed(() => authStore.roles);
const environmentLabel = computed(() => GLOBAL_MODE_LABELS[getGlobalMode()]);

const menuItems = [
  {
    group: 'Pilotage',
    name: 'Tableau de bord',
    path: '/dashboard',
    icon: LayoutDashboard,
    roles: ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2']
  },
  {
    group: 'Pilotage',
    name: 'Bilans matière',
    path: '/bilan-matiere',
    icon: Scale,
    roles: ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2']
  },
  { group: 'Pilotage', name: 'Alertes', path: '/alertes', icon: AlertTriangle, roles: ['Admin'] },
  { group: 'Opérations', name: 'Inventaire Fonderie', path: '/inventaire', icon: ClipboardList, roles: ['Admin'] },
  {
    group: 'Opérations',
    name: 'Contrôle poids',
    path: '/controle-poids-fonderie',
    icon: Gauge,
    roles: ['Admin', 'ResponsableFonderie']
  },
  {
    group: 'Opérations',
    name: 'Déchets Fonderie',
    path: '/dechets-fonderie',
    icon: Trash2,
    roles: ['Admin', 'ResponsableFonderie']
  },
  {
    group: 'Opérations',
    name: 'Réceptions Usinage 2',
    path: '/receptions-usinage-2',
    icon: CornerDownRight,
    roles: ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2']
  },
  { group: 'Données X3', name: 'Sorties magasin', path: '/sorties-x3', icon: RefreshCw, roles: ['Admin'] },
  { group: 'Données X3', name: 'États X3', path: '/etats-x3', icon: FileSpreadsheet, roles: ['Admin'] },
  {
    group: 'Données X3',
    name: 'Références de fabrication',
    path: '/references-fabrication',
    icon: Layers3,
    roles: ['Admin']
  },
  { group: 'Pilotage', name: 'Rapports', path: '/rapports', icon: FileText, roles: ['Admin'] },
  {
    group: 'Administration',
    name: 'Historique des imports',
    path: '/imports-historique',
    icon: FileArchive,
    roles: ['Admin']
  },
  { group: 'Administration', name: 'Utilisateurs et paramètres', path: '/admin', icon: Shield, roles: ['Admin'] },
  { group: 'Administration', name: 'Journal d’audit', path: '/audit', icon: History, roles: ['Admin'] }
];

const groupedMenu = computed(() => {
  if (roles.value.length === 0) return [];
  const visible = menuItems.filter((item) => item.roles.some((role) => roles.value.includes(role)));
  return ['Pilotage', 'Opérations', 'Données X3', 'Administration']
    .map((label) => ({ label, items: visible.filter((item) => item.group === label) }))
    .filter((group) => group.items.length > 0);
});
</script>

<template>
  <aside
    class="sidebar flex h-dvh shrink-0 flex-col border-r bg-[var(--color-sidebar-background)] text-primary-100 shadow-sm select-none"
  >
    <div class="sidebar-logo-zone border-b px-5 py-5">
      <div class="flex items-center gap-3">
        <div class="sidebar-logo-frame rounded-xl bg-white p-1.5 shadow-sm">
          <img :src="logoUrl" alt="Logo SOPAL" class="sidebar-logo" />
        </div>
        <div class="sidebar-brand-copy min-w-0 leading-tight">
          <h1 class="truncate text-sm font-semibold tracking-tight text-white">Bilan matière</h1>
          <span class="mt-1 block text-xs font-medium text-primary-100/80">UAP2 · SOPAL</span>
        </div>
      </div>
    </div>

    <nav class="flex-1 space-y-5 overflow-y-auto px-3 py-5" aria-label="Navigation principale">
      <section v-for="group in groupedMenu" :key="group.label" class="space-y-1.5">
        <h2 class="sidebar-group-label px-3 text-[11px] font-semibold tracking-[0.08em] text-primary-100/55 uppercase">
          {{ group.label }}
        </h2>
        <router-link
          v-for="item in group.items"
          :key="item.path"
          :to="item.path"
          :title="item.name"
          class="sidebar-link flex min-h-10 items-center gap-3 rounded-lg px-3 py-2 text-[13px] font-medium focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-400"
          :class="
            $route.path === item.path
              ? 'sidebar-link--active bg-primary-700 text-white shadow-sm ring-1 ring-inset ring-white/10'
              : 'sidebar-link--idle hover:bg-primary-800 hover:text-white'
          "
        >
          <component
            :is="item.icon"
            class="h-[18px] w-[18px] shrink-0"
            :class="$route.path === item.path ? 'text-white' : 'sidebar-link__icon--idle'"
            aria-hidden="true"
          />
          <span class="sidebar-nav-label truncate">{{ item.name }}</span>
        </router-link>
      </section>
    </nav>

    <div class="sidebar-footer border-t p-4">
      <div class="sidebar-environment flex items-center gap-2.5 rounded-lg border px-3 py-2.5">
        <span class="h-2 w-2 rounded-full bg-emerald-400" aria-hidden="true"></span>
        <span class="sidebar-footer-label text-xs font-medium text-primary-100/75">{{ environmentLabel }}</span>
      </div>
    </div>
  </aside>
</template>

<style scoped>
.sidebar-logo-frame {
  display: flex;
  width: 46px;
  height: 46px;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
}

.sidebar-logo-zone {
  background: var(--color-sidebar-logo);
  box-shadow: inset 0 -1px 0 rgb(169 213 255 / 0.08);
  border-color: var(--color-sidebar-border);
}

.sidebar-link--idle {
  color: var(--color-sidebar-text);
}

.sidebar-link--active {
  box-shadow: 0 5px 14px rgb(0 23 47 / 0.2);
}

.sidebar-link__icon--idle {
  color: var(--color-sidebar-icon);
}

.sidebar-group-label {
  color: var(--color-sidebar-group);
  font-weight: 700;
  letter-spacing: 0.11em;
}

.sidebar-footer {
  border-color: var(--color-sidebar-border);
}

.sidebar-environment {
  border-color: var(--color-sidebar-border);
  background: rgb(255 255 255 / 0.045);
}

.sidebar-logo {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.sidebar-brand-copy,
.sidebar-group-label,
.sidebar-nav-label,
.sidebar-footer-label {
  transition: opacity var(--motion-duration-fast) ease;
}

@media (max-width: 900px) {
  .sidebar {
    width: 72px;
    flex-basis: 72px;
  }

  .sidebar-brand-copy,
  .sidebar-group-label,
  .sidebar-nav-label,
  .sidebar-footer-label {
    display: none;
  }

  .sidebar-logo-frame {
    width: 40px;
    height: 40px;
  }

  .sidebar > div:first-child {
    padding-inline: 1rem;
  }

  .sidebar > div:last-of-type {
    padding-inline: 0.75rem;
  }

  .sidebar > div:last-of-type > div {
    justify-content: center;
    padding-inline: 0.5rem;
  }

  .sidebar nav {
    padding-inline: 0.5rem;
  }

  .sidebar nav section {
    margin-top: 0;
  }

  .sidebar a {
    justify-content: center;
    padding-inline: 0.5rem;
  }
}
</style>
