import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia, type Pinia } from 'pinia';
import { auditListItem, createAuditDetail, createPagedAudit } from './audit.test.fixtures';

const mocks = vi.hoisted(() => ({ search: vi.fn(), getDetail: vi.fn() }));
vi.mock('./audit.service', () => ({
  auditService: {
    search: mocks.search,
    getDetail: mocks.getDetail
  }
}));

import AuditView from './AuditView.vue';

let pinia: Pinia;
const mountView = () => mount(AuditView, { global: { plugins: [pinia] } });
const findButton = (wrapper: ReturnType<typeof mountView>, label: string) =>
  wrapper.findAll('button').find((button) => button.text().includes(label));

describe('AuditView', () => {
  beforeEach(() => {
    pinia = createPinia();
    setActivePinia(pinia);
    vi.clearAllMocks();
    mocks.search.mockResolvedValue(createPagedAudit());
  });

  it("affiche l'état de chargement", async () => {
    mocks.search.mockReturnValue(new Promise(() => undefined));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.find('[role="status"]').exists()).toBe(true);
  });

  it("affiche l'erreur API sans trace fictive", async () => {
    mocks.search.mockRejectedValue(new Error('API audit indisponible'));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('API audit indisponible');
    expect(wrapper.text()).not.toContain(auditListItem.referenceMetier);
  });

  it("affiche l'état vide explicite", async () => {
    mocks.search.mockResolvedValue(createPagedAudit([]));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Aucune trace pour ces filtres');
    expect(wrapper.text()).toContain('aucune donnée fictive');
  });

  it('affiche les traces persistantes et la pagination', async () => {
    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Administrateur SOPAL');
    expect(wrapper.text()).toContain('Validation');
    expect(wrapper.text()).toContain('ReceptionUsinage2');
    expect(wrapper.text()).toContain('CORR-AUDIT-001');
    expect(wrapper.text()).toContain('Page 1 sur 1');
  });

  it('ouvre le détail et compare lisiblement avant et après', async () => {
    mocks.getDetail.mockResolvedValue(createAuditDetail());
    const wrapper = mountView();
    await flushPromises();

    await findButton(wrapper, 'Détail')!.trigger('click');
    await flushPromises();

    const dialog = wrapper.get('[role="dialog"]');
    expect(dialog.text()).toContain('Valeurs avant / après');
    expect(dialog.text()).toContain('quantitePieces');
    expect(dialog.text()).toContain('Brouillon');
    expect(dialog.text()).toContain('Valide');
    expect(dialog.text()).toContain('Non renseigné');
    expect(dialog.text()).toContain('250');
    expect(dialog.text()).toContain('origine');
    expect(dialog.text()).not.toContain('{"statut"');
  });
});
