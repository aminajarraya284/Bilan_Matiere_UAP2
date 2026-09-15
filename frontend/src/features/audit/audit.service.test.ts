import { afterEach, describe, expect, it, vi } from 'vitest';
import { auditListItem, createAuditDetail, createPagedAudit } from './audit.test.fixtures';

const api = vi.hoisted(() => ({ get: vi.fn() }));
vi.mock('@/services/api', () => ({ default: api }));

import { auditService } from './audit.service';

const filters = {
  dateDepuis: '2026-08-01',
  dateJusqua: '',
  utilisateur: ' Administrateur SOPAL ',
  role: '',
  action: 'Validation' as const,
  domaine: 'ReceptionUsinage2' as const,
  entite: '',
  referenceMetier: ' OF-2026-001 ',
  correlationId: '',
  recherche: ''
};

describe('auditService', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.clearAllMocks();
  });

  it('recherche avec pagination et retire les filtres vides', async () => {
    const payload = createPagedAudit();
    api.get.mockResolvedValue({ data: payload });

    await expect(auditService.search({ filters, page: 2, pageSize: 25 })).resolves.toEqual(payload);

    expect(api.get).toHaveBeenCalledWith('/v1/audit', {
      params: {
        dateDepuis: '2026-08-01',
        utilisateur: 'Administrateur SOPAL',
        action: 'Validation',
        domaine: 'ReceptionUsinage2',
        referenceMetier: 'OF-2026-001',
        page: 2,
        pageSize: 25
      }
    });
  });

  it('charge le détail par identifiant encodé', async () => {
    const detail = createAuditDetail();
    api.get.mockResolvedValue({ data: detail });

    await expect(auditService.getDetail(auditListItem.id)).resolves.toEqual(detail);

    expect(api.get).toHaveBeenCalledWith(`/v1/audit/${auditListItem.id}`);
  });

  it("propage l'erreur API sans fallback vers une trace fictive", async () => {
    api.get.mockRejectedValue(new Error('API audit indisponible'));

    await expect(auditService.search({ filters, page: 1, pageSize: 50 })).rejects.toThrow(
      'API audit indisponible'
    );
    expect(api.get).toHaveBeenCalledOnce();
  });

  it('refuse explicitement le mode mock', async () => {
    vi.stubEnv('VITE_AUDIT_SOURCE', 'mock');

    await expect(auditService.search({ filters, page: 1, pageSize: 50 })).rejects.toThrow(
      "Journal d'audit nécessite une source API"
    );
    expect(api.get).not.toHaveBeenCalled();
  });
});
