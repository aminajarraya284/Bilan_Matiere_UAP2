import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/features/auth/auth.store';
import { hasPermission } from '@/utils/permissions';

const appRoute = (
  path: string,
  name: string,
  component: RouteRecordRaw['component'],
  meta: RouteRecordRaw['meta']
): RouteRecordRaw => ({
  path,
  component: () => import('@/components/layout/AppLayout.vue'),
  children: [
    {
      path: '',
      name,
      component,
      meta: { ...meta, requiresAuth: true }
    }
  ]
});

const routes: Array<RouteRecordRaw> = [
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
  {
    path: '/access-denied',
    component: () => import('@/components/layout/AuthLayout.vue'),
    children: [
      {
        path: '',
        name: 'AccessDenied',
        component: () => import('@/features/auth/LoginView.vue'),
        meta: { title: 'Accès refusé', subtitle: 'Permissions insuffisantes' }
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
  appRoute('/inventaire', 'Inventaire', () => import('@/features/inventaire/InventaireView.vue'), {
    title: 'Inventaire Fonderie',
    subtitle: 'Stock physique'
  }),
  appRoute(
    '/controle-poids-fonderie',
    'ControlePoidsFonderie',
    () => import('@/features/controle-poids-fonderie/ControlePoidsFonderieView.vue'),
    {
      title: 'Résultat de contrôle poids',
      subtitle: 'Opérations Fonderie'
    }
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
    {
      title: 'Déchets Fonderie',
      subtitle: 'Suivi des déchets'
    }
  ),
  appRoute(
    '/receptions-usinage-2',
    'ReceptionsUsinage2',
    () => import('@/features/receptions-usinage2/ReceptionsUsinage2View.vue'),
    {
      title: 'Réceptions Usinage 2',
      subtitle: 'Quantités reçues'
    }
  ),
  appRoute('/rapports', 'Rapports', () => import('@/features/rapports/RapportsView.vue'), {
    title: 'Rapports',
    subtitle: 'Résultats et exports'
  }),
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
  {
    path: '/:pathMatch(.*)*',
    redirect: '/'
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0, left: 0 };
  }
});

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore();
  const isAuthenticated = authStore.isAuthenticated;
  const role = authStore.userRole;

  if ((to.path === '/' || to.path === '/login') && isAuthenticated) {
    if (role === 'Admin') {
      return next('/dashboard');
    } else if (role === 'ResponsableFonderie') {
      return next('/dechets-fonderie');
    } else if (role === 'ResponsableUsinage2') {
      return next('/receptions-usinage-2');
    }
  }

  if (to.meta.requiresAuth && !isAuthenticated) {
    return next({ name: 'Login' });
  }

  if (to.name && to.meta.requiresAuth) {
    const authorized = hasPermission(role, to.name as string);
    if (!authorized) {
      if (role === 'ResponsableFonderie') {
        return next('/dechets-fonderie');
      } else if (role === 'ResponsableUsinage2') {
        return next('/receptions-usinage-2');
      } else {
        return next('/login');
      }
    }
  }

  next();
});

router.afterEach((to, from, failure) => {
  if (failure) {
    console.error('Navigation failed:', failure);
  }
});

router.onError((error) => {
  console.error('Router error:', error);
});

export default router;
