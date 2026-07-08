import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/features/auth/auth.store';
import { hasPermission } from '@/utils/permissions';

const routes: Array<RouteRecordRaw> = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/features/auth/LoginView.vue'),
    meta: { title: 'Connexion', subtitle: 'Accès interne SOPAL' }
  },
  {
    path: '/access-denied',
    name: 'AccessDenied',
    component: () => import('@/features/auth/LoginView.vue'),
    meta: { title: 'Accès Refusé', subtitle: 'Permissions insuffisantes' }
  },
  {
    path: '/',
    component: () => import('@/components/layout/MainLayout.vue'),
    children: [
      {
        path: '',
        redirect: '/dashboard'
      },
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/features/dashboard/DashboardView.vue'),
        meta: {
          title: 'Tableau de bord',
          subtitle: "Indicateurs clés d'exploitation et rendements",
          requiresAuth: true
        }
      },
      {
        path: 'bilan-matiere',
        name: 'BilanMatiere',
        component: () => import('@/features/bilan-matiere/BilanMatiereView.vue'),
        meta: { title: 'Bilan Matière', subtitle: 'Calcul des rendements et écarts de fonderie', requiresAuth: true }
      },
      {
        path: 'inventaire',
        name: 'Inventaire',
        component: () => import('@/features/inventaire/InventaireView.vue'),
        meta: { title: 'Inventaire Fonderie', subtitle: 'Déclaration des encours réels physiques', requiresAuth: true }
      },
      {
        path: 'sorties-x3',
        name: 'SortiesX3',
        component: () => import('@/features/sorties-x3/SortiesX3View.vue'),
        meta: { title: 'Sorties Magasin X3', subtitle: "Passerelle d'importation ERP Sage X3", requiresAuth: true }
      },
      {
        path: 'imports/x3',
        name: 'ImportsX3',
        component: () => import('@/features/imports/X3Importer.vue'),
        meta: { title: 'Import X3', subtitle: 'Importateur de fichiers YListOFFerm', requiresAuth: true }
      },
      {
        path: 'dechets-fonderie',
        name: 'DechetsFonderie',
        component: () => import('@/features/dechets-fonderie/DechetsFonderieView.vue'),
        meta: { title: 'Déchets Fonderie', subtitle: 'Registre des bavures et poudre de coulée', requiresAuth: true }
      },
      {
        path: 'receptions-usinage-2',
        name: 'ReceptionsUsinage2',
        component: () => import('@/features/receptions-usinage2/ReceptionsUsinage2View.vue'),
        meta: { title: 'Réceptions Usinage 2', subtitle: 'Registre des pièces usinées conformes', requiresAuth: true }
      },
      {
        path: 'rapports',
        name: 'Rapports',
        component: () => import('@/features/rapports/RapportsView.vue'),
        meta: { title: 'Rapports', subtitle: "Analyse périodique et rapports d'exploitation", requiresAuth: true }
      },
      {
        path: 'admin',
        name: 'Admin',
        component: () => import('@/features/admin/AdminView.vue'),
        meta: { title: 'Administration', subtitle: "Gestion des profils et configurations d'usine", requiresAuth: true }
      },
      {
        path: 'alertes',
        name: 'Alertes',
        component: () => import('@/features/alertes/AlertesView.vue'),
        meta: { title: 'Alertes', subtitle: "Alertes poids et écarts bilan réservées à l'Admin", requiresAuth: true }
      },
      {
        path: 'audit',
        name: 'Audit',
        component: () => import('@/features/audit/AuditView.vue'),
        meta: { title: "Journal d'audit", subtitle: "Traçabilité des actions d'exploitation", requiresAuth: true }
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/dashboard'
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0, left: 0 };
  }
});

// Navigation Guards: Protect routes and verify permissions
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore();
  const isAuthenticated = authStore.isAuthenticated;
  const role = authStore.userRole;

  if (to.path === '/login' && isAuthenticated) {
    // Already connected, redirect to role default page
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

  // Check roles permissions
  if (to.name && to.meta.requiresAuth) {
    const authorized = hasPermission(role, to.name as string);
    if (!authorized) {
      // Redirect to default page based on role or back
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

// After navigation hook: catch errors
router.afterEach((to, from, failure) => {
  if (failure) {
    console.error('Navigation failed:', failure);
  }
});

// Global error handler for navigation
router.onError((error) => {
  console.error('Router error:', error);
});

export default router;
