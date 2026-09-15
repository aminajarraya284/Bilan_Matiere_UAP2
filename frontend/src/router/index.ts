import { createRouter, createWebHistory, type NavigationGuard, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/features/auth/auth.store';
import { consumeAuthReturnPath, rememberAuthReturnPath } from '@/features/auth/auth.navigation';
import { getDefaultRouteForRoles, type ApplicationRole } from '@/features/auth/auth.roles';
import { routePermissions } from '@/utils/permissions';

type AppRouteRecord = RouteRecordRaw & { children: RouteRecordRaw[] };

const appRoute = (
  path: string,
  name: string,
  component: RouteRecordRaw['component'],
  meta: RouteRecordRaw['meta']
): AppRouteRecord =>
  ({
    path,
    component: () => import('@/components/layout/AppLayout.vue'),
    children: [
      {
        path: '',
        name,
        component,
        meta: { ...meta, requiresAuth: true, roles: routePermissions[name] ?? [] }
      }
    ]
  }) as AppRouteRecord;

export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('@/components/layout/AuthLayout.vue'),
    children: [
      {
        path: '',
        name: 'Home',
        component: () => import('@/features/auth/LoginView.vue'),
        meta: { title: 'Connexion', subtitle: 'Accès sécurisé' }
      }
    ]
  },
  {
    path: '/login',
    component: () => import('@/components/layout/AuthLayout.vue'),
    children: [
      {
        path: '',
        name: 'Login',
        component: () => import('@/features/auth/LoginView.vue'),
        meta: { title: 'Connexion', subtitle: 'Accès sécurisé' }
      }
    ]
  },
  { path: '/access-denied', redirect: '/forbidden' },
  {
    path: '/forbidden',
    component: () => import('@/components/layout/AuthLayout.vue'),
    children: [
      {
        path: '',
        name: 'Forbidden',
        component: () => import('@/features/auth/ForbiddenView.vue'),
        meta: { title: 'Accès interdit', subtitle: 'Permissions insuffisantes' }
      }
    ]
  },
  appRoute('/dashboard', 'Dashboard', () => import('@/features/dashboard/DashboardView.vue'), {
    title: 'Accueil',
    subtitle: 'Vue générale'
  }),
  appRoute('/bilan-matiere', 'BilanMatiere', () => import('@/features/bilan-matiere/BilanMatiereView.vue'), {
    title: 'Bilan matière',
    subtitle: 'Suivi par atelier'
  }),
  appRoute('/bilan-uap2', 'BilanUap2', () => import('@/features/bilan-matiere/BilanMatiereUap2View.vue'), {
    title: 'Bilan UAP2 par OF',
    subtitle: 'Sortie matière et écart par ordre de fabrication'
  }),
  appRoute('/inventaire', 'Inventaire', () => import('@/features/inventaire/InventaireView.vue'), {
    title: 'Inventaire Fonderie',
    subtitle: 'Stock physique'
  }),
  appRoute(
    '/controle-poids-fonderie',
    'ControlePoidsFonderie',
    () => import('@/features/controle-poids-fonderie/ControlePoidsFonderieView.vue'),
    { title: 'Résultat de contrôle poids', subtitle: 'Opérations Fonderie' }
  ),
  appRoute('/sorties-x3', 'SortiesX3', () => import('@/features/sorties-x3/SortiesX3View.vue'), {
    title: 'Consommations composants',
    subtitle: 'Mouvements VCR et encours'
  }),
  appRoute('/etats-x3', 'EtatsX3', () => import('@/features/etats-x3/EtatsX3View.vue'), {
    title: 'États X3',
    subtitle: 'Import et synchronisation'
  }),
  appRoute(
    '/references-fabrication',
    'ReferencesFabrication',
    () => import('@/features/reference/ReferencesFabricationView.vue'),
    {
      title: 'Références fabrication',
      subtitle: 'Import, synchronisation et consultation des références métier utilisées dans les réceptions Usinage 2.'
    }
  ),
  appRoute(
    '/dechets-fonderie',
    'DechetsFonderie',
    () => import('@/features/dechets-fonderie/DechetsFonderieView.vue'),
    { title: 'Déchets Fonderie', subtitle: 'Suivi des déchets' }
  ),
  appRoute(
    '/receptions-usinage-2',
    'ReceptionsUsinage2',
    () => import('@/features/receptions-usinage2/ReceptionsUsinage2View.vue'),
    { title: 'Réceptions Usinage 2', subtitle: 'Quantités reçues' }
  ),
  { path: '/receptions-usinage2', redirect: '/receptions-usinage-2' },
  appRoute('/rapports', 'Rapports', () => import('@/features/rapports/RapportsView.vue'), {
    title: 'Rapports',
    subtitle: 'Résultats et exports'
  }),
  appRoute(
    '/imports-historique',
    'ImportsHistorique',
    () => import('@/features/imports-historique/ImportsHistoriqueView.vue'),
    { title: 'Historique des imports', subtitle: 'Fichiers sources et téléchargement' }
  ),
  appRoute('/admin', 'Admin', () => import('@/features/admin/AdminView.vue'), {
    title: 'Administration',
    subtitle: 'Utilisateurs et paramètres'
  }),
  appRoute('/alertes', 'Alertes', () => import('@/features/alertes/AlertesView.vue'), {
    title: 'Alertes',
    subtitle: 'Écarts et poids'
  }),
  appRoute('/audit', 'Audit', () => import('@/features/audit/AuditView.vue'), {
    title: 'Journal d’audit',
    subtitle: 'Traçabilité des actions'
  }),
  appRoute('/historique-periodes', 'HistoriquePeriodes', () => import('@/features/archivage/HistoriquePeriodesView.vue'), {
    title: 'Historique des périodes',
    subtitle: 'Bilans mensuels archivés'
  }),
  appRoute('/historique-periodes/:periode', 'PeriodeArchivee', () => import('@/features/archivage/PeriodeArchiveeView.vue'), {
    title: 'Période archivée',
    subtitle: 'Consultation en lecture seule'
  }),
  { path: '/:pathMatch(.*)*', redirect: '/' }
];

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0, left: 0 })
});

export const authGuard: NavigationGuard = async (to) => {
  const authStore = useAuthStore();
  const isLoginRoute = to.name === 'Home' || to.name === 'Login';

  if (!authStore.initialized) {
    try {
      await authStore.initialize();
    } catch {
      if (isLoginRoute || to.name === 'Forbidden') return true;
      rememberAuthReturnPath(to.fullPath);
      return { name: 'Login', query: { error: 'configuration', redirect: to.fullPath } };
    }
  }

  if (isLoginRoute && authStore.isAuthenticated) {
    return consumeAuthReturnPath() ?? getDefaultRouteForRoles(authStore.roles);
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    rememberAuthReturnPath(to.fullPath);
    return { name: 'Login', query: { redirect: to.fullPath } };
  }

  const allowedRoles = (to.meta.roles ?? []) as ApplicationRole[];
  if (to.meta.requiresAuth && allowedRoles.length > 0 && !authStore.hasAnyRole(allowedRoles)) {
    return { name: 'Forbidden', query: { from: to.fullPath } };
  }

  return true;
};

router.beforeEach(authGuard);

router.afterEach((to, from, failure) => {
  if (failure) console.error('Navigation failed:', failure);
});

router.onError((error) => console.error('Router error:', error));

export default router;
