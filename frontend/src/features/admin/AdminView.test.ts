import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

const mocks = vi.hoisted(() => ({
  getParameters: vi.fn(),
  updateParameter: vi.fn(),
  getMaterialTypes: vi.fn(),
  getUsers: vi.fn(),
  setUserActivation: vi.fn(),
  getDevelopmentAccounts: vi.fn(),
  alert: vi.fn()
}));

vi.mock('./admin.service', () => ({
  adminService: {
    getParameters: mocks.getParameters,
    updateParameter: mocks.updateParameter,
    getMaterialTypes: mocks.getMaterialTypes,
    getUsers: mocks.getUsers,
    setUserActivation: mocks.setUserActivation,
    getDevelopmentAccounts: mocks.getDevelopmentAccounts
  }
}));

vi.mock('@/composables/useAppDialog', () => ({
  useAppDialog: () => ({ alert: mocks.alert })
}));

import AdminView from './AdminView.vue';

const parameter = {
  cle: 'SEUIL_ECART_UAP2',
  valeur: '1.5',
  description: 'Seuil du bilan UAP2',
  modifiable: true,
  typeValeur: 'Décimal'
};

const materialType = {
  id: '30000000-0000-0000-0000-000000000001',
  code: 'M3010L0',
  libelle: 'Laiton SOPAL',
  estActif: true,
  dateCreationUtc: '2026-01-01T00:00:00Z',
  dateModificationUtc: null
};

const userProfile = {
  id: 7,
  entraObjectId: 'OID-7',
  email: 'ahmed@sopal.com',
  nomAffichage: 'Ahmed Ben Ali',
  roleObserve: 'ResponsableFonderie',
  estActif: true,
  dateCreationUtc: '2026-09-01T08:00:00Z',
  dateModificationUtc: '2026-09-01T08:00:00Z',
  derniereConnexionUtc: '2026-09-03T10:15:00Z'
};

const pagedUsers = (items = [userProfile]) => ({
  items,
  total: items.length,
  page: 1,
  pageSize: 25,
  totalPages: 1
});

const developmentAccount = {
  username: 'admin',
  nomAffichage: 'Administrateur SOPAL',
  email: 'admin@sopal.com',
  role: 'Admin',
  estConfigure: true
};

const mountView = async () => {
  const wrapper = mount(AdminView, {
    global: {
      stubs: {
        PageHeader: {
          props: ['title', 'description'],
          template: '<header>{{ title }} {{ description }}</header>'
        },
        LoadingState: { template: '<div>Chargement</div>' }
      },
      // RouterLink est fourni par le plugin router dans l'application : sans router
      // installe ici, il faut l'enregistrer comme composant global du test.
      components: {
        RouterLink: {
          props: ['to'],
          template: '<a :href="to"><slot /></a>'
        }
      }
    }
  });
  await flushPromises();
  return wrapper;
};

const clickTab = async (wrapper: Awaited<ReturnType<typeof mountView>>, label: string) => {
  const tab = wrapper.findAll('button').find((item) => item.text().includes(label));
  expect(tab).toBeDefined();
  await tab!.trigger('click');
  await flushPromises();
};

describe('AdminView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getParameters.mockResolvedValue([parameter]);
    mocks.getMaterialTypes.mockResolvedValue([materialType]);
    mocks.getUsers.mockResolvedValue(pagedUsers());
    mocks.getDevelopmentAccounts.mockResolvedValue([developmentAccount]);
    mocks.updateParameter.mockResolvedValue(parameter);
  });

  it('présente l’état d’intégration Entra ID sans comptes ni formulaire fictifs', async () => {
    const wrapper = await mountView();
    const text = wrapper.text();

    expect(text).toContain('Microsoft Entra ID');
    expect(text).toContain('Microsoft Entra ID');
    expect(text).toContain("Aucun mot de passe n'est stocké");
    expect(text).not.toContain('Amina Jarraya');
    expect(text).not.toContain('Ahmed Trabelsi');
    expect(text).not.toContain('Khalil Ben Ali');
    expect(text).not.toContain("Créer l'utilisateur");
    expect(wrapper.find('input[type="password"]').exists()).toBe(false);
  });

  it('n’affiche aucun badge global Mode démonstration', async () => {
    const wrapper = await mountView();

    expect(wrapper.text()).not.toContain('Mode démonstration');
  });

  it('affiche les quatre onglets attendus', async () => {
    const wrapper = await mountView();
    const text = wrapper.text();

    expect(text).toContain('Utilisateurs & rôles');
    expect(text).toContain('Références X3');
    expect(text).toContain('Types matière');
    expect(text).toContain('Paramètres & seuils');
  });

  it('expose les références X3 en lecture seule avec le bon lien', async () => {
    const wrapper = await mountView();
    await clickTab(wrapper, 'Références X3');

    expect(wrapper.text()).toContain('Source : données X3 importées · Lecture seule');
    expect(wrapper.get('a').attributes('href')).toBe('/references-fabrication');
    expect(wrapper.text()).not.toContain('Créer article');
    expect(wrapper.text()).not.toContain('Modifier article');
    expect(wrapper.text()).not.toContain('Supprimer article');
    expect(wrapper.text()).not.toContain('Activer article');
  });

  it('charge et affiche les paramètres réels', async () => {
    const wrapper = await mountView();
    await clickTab(wrapper, 'Paramètres & seuils');

    expect(mocks.getParameters).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('SEUIL_ECART_UAP2');
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe('1.5');
  });

  it('enregistre un paramètre via le service réel', async () => {
    const wrapper = await mountView();
    await clickTab(wrapper, 'Paramètres & seuils');

    await wrapper.get('input').setValue('2');
    const save = wrapper.findAll('button').find((item) => item.text() === 'Enregistrer');
    await save!.trigger('click');
    await flushPromises();

    expect(mocks.updateParameter).toHaveBeenCalledWith('SEUIL_ECART_UAP2', '2');
  });

  it('affiche une erreur explicite si les paramètres sont indisponibles', async () => {
    mocks.getParameters.mockRejectedValue(new Error('API indisponible'));

    const wrapper = await mountView();
    await clickTab(wrapper, 'Paramètres & seuils');

    expect(wrapper.text()).toContain('Impossible de charger les paramètres de l’application.');
    expect(wrapper.text()).not.toContain('API indisponible');
  });

  it('charge les types matière depuis le service réel', async () => {
    const wrapper = await mountView();
    await clickTab(wrapper, 'Types matière');

    expect(mocks.getMaterialTypes).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('M3010L0');
    expect(wrapper.text()).toContain('Laiton SOPAL');
    expect(wrapper.text()).toContain('Actif');
  });

  it('affiche un état vide lorsque le référentiel matière est vide', async () => {
    mocks.getMaterialTypes.mockResolvedValue([]);

    const wrapper = await mountView();
    await clickTab(wrapper, 'Types matière');

    expect(wrapper.text()).toContain('Aucun type matière configuré.');
  });

  it('isole une erreur Types matière sans masquer l’état utilisateurs', async () => {
    mocks.getMaterialTypes.mockRejectedValue(new Error('échec'));

    const wrapper = await mountView();
    expect(wrapper.text()).toContain('Microsoft Entra ID');
    await clickTab(wrapper, 'Types matière');
    expect(wrapper.text()).toContain('Impossible de charger les types matière.');
  });

  it('affiche les profils Entra avec un rôle en lecture seule', async () => {
    const wrapper = await mountView();

    const text = wrapper.text();
    expect(text).toContain('Ahmed Ben Ali');
    expect(text).toContain('ahmed@sopal.com');
    expect(text).toContain('ResponsableFonderie');
    // Aucun contrôle de saisie du rôle : Entra en est la source.
    expect(wrapper.find('select[name="role"]').exists()).toBe(false);
    // Aucun formulaire de création manuelle d'utilisateur Entra.
    expect(text).not.toContain('EntraObjectId');
    expect(text.toLowerCase()).not.toContain('nouvel utilisateur');
  });

  it('affiche les comptes de développement sans jamais montrer de mot de passe', async () => {
    const wrapper = await mountView();

    const html = wrapper.html();
    expect(wrapper.text()).toContain('Comptes de développement');
    expect(wrapper.text()).toContain('admin');
    expect(html.toLowerCase()).not.toContain('password');
    expect(html.toLowerCase()).not.toContain('mot de passe du compte');
  });

  it('bascule l’activation d’un profil via l’API', async () => {
    mocks.setUserActivation.mockResolvedValue({ ...userProfile, estActif: false });
    const wrapper = await mountView();

    const bouton = wrapper.findAll('button').find((item) => item.text() === 'Désactiver');
    expect(bouton).toBeDefined();
    await bouton!.trigger('click');
    await flushPromises();

    expect(mocks.setUserActivation).toHaveBeenCalledWith(7, false);
    expect(wrapper.text()).toContain('Inactif');
  });

  it('distingue une erreur API d’une absence de profil', async () => {
    mocks.getUsers.mockRejectedValue(new Error('boom'));
    const wrapper = await mountView();

    expect(wrapper.text()).toContain('Impossible de charger les profils utilisateurs.');
    // Une panne ne doit jamais s'afficher comme « aucun profil ».
    expect(wrapper.text()).not.toContain('Aucun profil pour l’instant');
  });

  it('n’affiche aucune section développement quand l’API répond 404', async () => {
    mocks.getDevelopmentAccounts.mockRejectedValue(new Error('404'));
    const wrapper = await mountView();

    expect(wrapper.text()).not.toContain('Comptes de développement');
    // Ce n'est pas une panne : les profils Entra restent affichés.
    expect(wrapper.text()).toContain('Ahmed Ben Ali');
  });
});
