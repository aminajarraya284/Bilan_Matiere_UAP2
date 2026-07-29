import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import EtatsX3View from './EtatsX3View.vue';
import type { X3BilanType, X3ExcelFileType, X3ImportedSourceStatus } from '@/services/x3ExcelImportService';

const mocks = vi.hoisted(() => ({
  refreshStatuses: vi.fn(),
  getStatus: vi.fn(),
  importMVTSTOTRSVCR: vi.fn(),
  importYRJMCH: vi.fn(),
  downloadArchivedFile: vi.fn(),
  recalculerClassificationYRJMCH: vi.fn(),
  success: vi.fn(),
  error: vi.fn()
}));

vi.mock('@/services/x3ExcelImportService', () => ({
  x3ExcelImportService: {
    refreshStatuses: mocks.refreshStatuses,
    getStatus: mocks.getStatus,
    importMVTSTOTRSVCR: mocks.importMVTSTOTRSVCR,
    importYRJMCH: mocks.importYRJMCH,
    downloadArchivedFile: mocks.downloadArchivedFile,
    recalculerClassificationYRJMCH: mocks.recalculerClassificationYRJMCH
  }
}));

vi.mock('@/composables/useAppToast', () => ({
  useAppToast: () => ({
    success: mocks.success,
    error: mocks.error
  })
}));

const status = (
  bilanType: X3BilanType,
  fileType: X3ExcelFileType,
  overrides: Partial<X3ImportedSourceStatus> = {}
): X3ImportedSourceStatus => ({
  bilanType,
  fileType,
  fileName: '',
  importedAt: '',
  rowCount: 0,
  status: 'Non importé',
  ...overrides
});

const mountView = async () => {
  const wrapper = mount(EtatsX3View, {
    global: {
      components: {
        RouterLink: {
          props: ['to'],
          template: '<a :data-to="to"><slot /></a>'
        }
      },
      stubs: { PageHeader: true }
    }
  });
  await vi.dynamicImportSettled();
  await flushPromises();
  return wrapper;
};

describe('EtatsX3View', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.refreshStatuses.mockResolvedValue(undefined);
    mocks.getStatus.mockImplementation((bilanType: X3BilanType, fileType: X3ExcelFileType) =>
      status(bilanType, fileType)
    );
    mocks.recalculerClassificationYRJMCH.mockResolvedValue({
      lignesAnalysees: 100,
      lignesModifiees: 4,
      lignesInchangees: 96,
      lignesSci: 20,
      lignesEmr: 50,
      lignesTrn: 25,
      lignesInconnues: 5
    });
  });

  it('affiche les deux cartes et les actions communes', async () => {
    const text = (await mountView()).text();

    expect(text).toContain('MVTSTOTRSVCR — Entrées OF');
    expect(text).toContain('YRJMCH — Rapports machine');
    expect(text).toContain('Importer Excel');
    expect(text).toContain("Consulter l'historique");
    expect(text).not.toContain('Synchroniser');
  });

  it('affiche exactement la règle métier YRJMCH validée', async () => {
    const text = (await mountView()).text();

    expect(text).toContain(
      'Pour YRJMCH, le processus est déterminé automatiquement par les trois premiers caractères du poste de la colonne B : SCI, EMR ou TRN.'
    );
    expect(text).toContain('Lecture des 3 premiers caractères');
    expect(text).toContain('SCI / EMR / TRN / Non classé');
    expect(text).toContain('Regroupement par période + Numéro OF + Code article');
  });

  it('affiche les usages attendus de YRJMCH', async () => {
    const text = (await mountView()).text();

    expect(text).toContain('Bilan Usinage 2');
    expect(text).toContain('Bilan UAP2');
    expect(text).toContain('Alertes de non-réception');
  });

  it('affiche le message d information sur les préfixes inconnus', async () => {
    const text = (await mountView()).text();

    expect(text).toContain('Les postes commençant par SCI, EMR ou TRN sont classés automatiquement.');
    expect(text).toContain('Tout autre préfixe reste “Non classé”');
  });

  it('ne contient aucune terminologie liée au mapping manuel', async () => {
    const text = (await mountView()).text().toLowerCase();

    for (const texteInterdit of [
      'code poste exact',
      'référentiel postes processus',
      'poste administré',
      'poste non mappé',
      'administrer les correspondances',
      'correspondance définie une seule fois',
      'recalculer les rapports'
    ]) {
      expect(text).not.toContain(texteInterdit);
    }
  });

  it('ne contient aucun lien vers la page supprimée', async () => {
    const wrapper = await mountView();

    expect(wrapper.html()).not.toContain('/postes-processus');
    expect(wrapper.text()).not.toContain('Postes processus');
  });

  it('appelle le recalcul backend et affiche tous les compteurs', async () => {
    const wrapper = await mountView();
    const button = wrapper.findAll('button').find((node) => node.text().includes('Recalculer la classification'));

    expect(button).toBeDefined();
    await button!.trigger('click');
    await flushPromises();

    expect(mocks.recalculerClassificationYRJMCH).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('Total : 100');
    expect(wrapper.text()).toContain('Modifiées : 4');
    expect(wrapper.text()).toContain('Inchangées : 96');
    expect(wrapper.text()).toContain('SCI : 20');
    expect(wrapper.text()).toContain('EMR : 50');
    expect(wrapper.text()).toContain('TRN : 25');
    expect(wrapper.text()).toContain('Inconnues : 5');
  });

  it('conserve le bouton de réimport après un premier import', async () => {
    mocks.getStatus.mockImplementation((bilanType: X3BilanType, fileType: X3ExcelFileType) =>
      status(bilanType, fileType, { status: 'Importé', fileName: `${fileType}.xls` })
    );

    expect((await mountView()).text()).toContain('Réimporter une nouvelle version');
  });

  it('décrit MVT sans lui attribuer la classification par poste', async () => {
    const text = (await mountView()).text();

    expect(text).toContain(
      'Ce fichier contient les mouvements Entrée OF. Il ne sert pas à déterminer les processus SCI, EMR ou TRN.'
    );
    expect(text).toContain('Date d’imputation');
    expect(text).toContain('Pièce origine / Numéro OF');
    expect(text).toContain('Quantité signée');
  });

  it('conserve la règle des quantités signées MVT', async () => {
    const text = (await mountView()).text();

    expect(text).toContain('Les quantités restent signées.');
    expect(text).toContain('-980 + 980 = 0');
  });

  it('signale un export MVT sans transaction Entrée OF', async () => {
    mocks.getStatus.mockImplementation((bilanType: X3BilanType, fileType: X3ExcelFileType) =>
      status(bilanType, fileType, {
        status: 'Importé',
        rowsRead: fileType === 'MVTSTOTRSVCR' ? 1200 : 100,
        rowCount: fileType === 'MVTSTOTRSVCR' ? 0 : 100
      })
    );

    expect((await mountView()).text()).toContain('Ce fichier ne contient aucune transaction Entrée OF.');
  });

  it('affiche les compteurs de lecture et l état des archives', async () => {
    mocks.getStatus.mockImplementation((bilanType: X3BilanType, fileType: X3ExcelFileType) =>
      status(bilanType, fileType, {
        status: 'Importé',
        fileName: `${fileType}.xls`,
        rowsRead: 100,
        rowCount: 95,
        rowsRejected: 5,
        hasArchivedFile: fileType === 'YRJMCH',
        archivedFileId: fileType === 'YRJMCH' ? 'file-id' : undefined
      })
    );

    const text = (await mountView()).text();
    expect(text).toContain('Erreurs de lecture5');
    expect(text).toContain('Télécharger');
    expect(text).toContain('Aucun fichier archivé pour cet ancien import');
  });
});
