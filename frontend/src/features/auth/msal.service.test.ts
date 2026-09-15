import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AccountInfo, AuthenticationResult } from '@azure/msal-browser';
import { AccessTokenAcquisitionError, AuthenticationRequiredError } from './auth.errors';
import { MsalAuthenticationService } from './msal.service';

const config = {
  mode: 'entra' as const,
  clientId: 'frontend-client-id',
  authority: 'https://login.microsoftonline.com/tenant/v2.0',
  redirectUri: 'https://app.example.test/',
  postLogoutRedirectUri: 'https://app.example.test/login',
  apiScope: 'api://backend/access_as_user'
};

const account = {
  homeAccountId: 'home-id',
  environment: 'login.microsoftonline.com',
  tenantId: 'tenant',
  username: 'amina@sopal.test',
  localAccountId: 'local-id',
  name: 'Amina SOPAL',
  idTokenClaims: {
    oid: 'entra-user-id',
    name: 'Amina SOPAL',
    preferred_username: 'amina@sopal.test',
    roles: ['Admin', 'RoleInconnu']
  }
} as AccountInfo;

const result = (accessToken = 'access-token'): AuthenticationResult =>
  ({ account, accessToken }) as AuthenticationResult;

const createClient = () => {
  let activeAccount: AccountInfo | null = null;
  const client = {
    initialize: vi.fn().mockResolvedValue(undefined),
    handleRedirectPromise: vi.fn().mockResolvedValue(null),
    getActiveAccount: vi.fn(() => activeAccount),
    setActiveAccount: vi.fn((value: AccountInfo | null) => {
      activeAccount = value;
    }),
    getAllAccounts: vi.fn(() => [] as AccountInfo[]),
    loginRedirect: vi.fn().mockResolvedValue(undefined),
    logoutRedirect: vi.fn().mockResolvedValue(undefined),
    acquireTokenSilent: vi.fn().mockResolvedValue(result()),
    acquireTokenRedirect: vi.fn().mockResolvedValue(undefined)
  };
  return client;
};

describe('MSAL authentication service', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('initialise une seule fois et active le compte du retour de redirection', async () => {
    const client = createClient();
    client.handleRedirectPromise.mockResolvedValue(result());
    const service = new MsalAuthenticationService(config, client);

    await Promise.all([service.initialize(), service.initialize()]);

    expect(client.initialize).toHaveBeenCalledTimes(1);
    expect(client.handleRedirectPromise).toHaveBeenCalledTimes(1);
    expect(client.setActiveAccount).toHaveBeenCalledWith(account);
  });

  it('restaure le premier compte existant quand aucun retour de redirection n’existe', async () => {
    const client = createClient();
    client.getAllAccounts.mockReturnValue([account]);
    const service = new MsalAuthenticationService(config, client);
    await service.initialize();
    expect(client.setActiveAccount).toHaveBeenCalledWith(account);
  });

  it('reste non authentifié lorsqu’aucun compte n’existe', async () => {
    const service = new MsalAuthenticationService(config, createClient());
    await service.initialize();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.getAuthenticatedUser()).toBeNull();
  });

  it('utilise loginRedirect avec le seul scope API', async () => {
    const client = createClient();
    const service = new MsalAuthenticationService(config, client);
    await service.login();
    expect(client.loginRedirect).toHaveBeenCalledWith({ scopes: [config.apiScope] });
  });

  it('utilise logoutRedirect avec le compte actif', async () => {
    const client = createClient();
    client.getAllAccounts.mockReturnValue([account]);
    const service = new MsalAuthenticationService(config, client);
    await service.initialize();
    await service.logout();
    expect(client.logoutRedirect).toHaveBeenCalledWith({
      account,
      postLogoutRedirectUri: config.postLogoutRedirectUri
    });
  });

  it('acquiert silencieusement le token et déduplique les appels concurrents', async () => {
    const client = createClient();
    client.getAllAccounts.mockReturnValue([account]);
    let resolveToken!: (value: AuthenticationResult) => void;
    client.acquireTokenSilent.mockReturnValue(new Promise((resolve) => (resolveToken = resolve)));
    const service = new MsalAuthenticationService(config, client);
    await service.initialize();

    const first = service.acquireAccessToken();
    const second = service.acquireAccessToken();
    resolveToken(result('silent-token'));

    await expect(first).resolves.toBe('silent-token');
    await expect(second).resolves.toBe('silent-token');
    expect(client.acquireTokenSilent).toHaveBeenCalledTimes(1);
  });

  it('déclenche acquireTokenRedirect uniquement si une interaction est requise', async () => {
    const client = createClient();
    client.getAllAccounts.mockReturnValue([account]);
    client.acquireTokenSilent.mockRejectedValue({ errorCode: 'interaction_required' });
    const service = new MsalAuthenticationService(config, client);
    await service.initialize();

    await expect(service.acquireAccessToken()).rejects.toBeInstanceOf(AuthenticationRequiredError);
    expect(client.acquireTokenRedirect).toHaveBeenCalledWith({ account, scopes: [config.apiScope] });
  });

  it('ne transforme pas une erreur non interactive en redirection', async () => {
    const client = createClient();
    client.getAllAccounts.mockReturnValue([account]);
    client.acquireTokenSilent.mockRejectedValue(new Error('network'));
    const service = new MsalAuthenticationService(config, client);
    await service.initialize();

    await expect(service.acquireAccessToken()).rejects.toBeInstanceOf(AccessTokenAcquisitionError);
    expect(client.acquireTokenRedirect).not.toHaveBeenCalled();
  });

  it('refuse l’acquisition sans compte actif', async () => {
    const service = new MsalAuthenticationService(config, createClient());
    await service.initialize();
    await expect(service.acquireAccessToken()).rejects.toBeInstanceOf(AuthenticationRequiredError);
  });

  it('normalise uniquement les trois rôles applicatifs et ignore les inconnus', async () => {
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    const client = createClient();
    client.getAllAccounts.mockReturnValue([account]);
    const service = new MsalAuthenticationService(config, client);
    await service.initialize();
    expect(service.getRoles()).toEqual(['Admin']);
    expect(service.getAuthenticatedUser()).toMatchObject({
      id: 'entra-user-id',
      displayName: 'Amina SOPAL',
      roles: ['Admin'],
      authenticationMode: 'entra'
    });
  });
});
