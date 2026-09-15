import {
  InteractionRequiredAuthError,
  PublicClientApplication,
  type AccountInfo,
  type AuthenticationResult,
  type Configuration,
  type EndSessionRequest,
  type RedirectRequest,
  type SilentRequest
} from '@azure/msal-browser';
import { getAuthConfig, type EntraAuthConfig } from '@/config/auth.config';
import {
  AccessTokenAcquisitionError,
  AuthenticationRequiredError,
  AuthConfigurationError
} from './auth.errors';
import { getPrimaryRole, normalizeApplicationRoles } from './auth.roles';
import type { AuthenticatedUser, ApplicationRole } from './auth.types';

export interface MsalClient {
  initialize(): Promise<void>;
  handleRedirectPromise(): Promise<AuthenticationResult | null>;
  getActiveAccount(): AccountInfo | null;
  setActiveAccount(account: AccountInfo | null): void;
  getAllAccounts(): AccountInfo[];
  loginRedirect(request?: RedirectRequest): Promise<void>;
  logoutRedirect(request?: EndSessionRequest): Promise<void>;
  acquireTokenSilent(request: SilentRequest): Promise<AuthenticationResult>;
  acquireTokenRedirect(request: RedirectRequest): Promise<void>;
}

export const buildMsalConfiguration = (config: EntraAuthConfig): Configuration => ({
  auth: {
    clientId: config.clientId,
    authority: config.authority,
    redirectUri: config.redirectUri,
    postLogoutRedirectUri: config.postLogoutRedirectUri
  },
  cache: {
    cacheLocation: 'sessionStorage'
  }
});

const isInteractionRequired = (error: unknown): boolean =>
  error instanceof InteractionRequiredAuthError ||
  (typeof error === 'object' && error !== null && 'errorCode' in error &&
    ['interaction_required', 'login_required', 'consent_required'].includes(String(error.errorCode)));

export class MsalAuthenticationService {
  private initializationPromise: Promise<void> | null = null;
  private tokenPromise: Promise<string> | null = null;

  constructor(
    private readonly config: EntraAuthConfig,
    private readonly client: MsalClient = new PublicClientApplication(buildMsalConfiguration(config))
  ) {}

  initialize(): Promise<void> {
    if (!this.initializationPromise) {
      this.initializationPromise = this.initializeInternal().catch((error) => {
        this.initializationPromise = null;
        throw error;
      });
    }
    return this.initializationPromise;
  }

  private async initializeInternal(): Promise<void> {
    await this.client.initialize();
    await this.handleRedirect();
    if (!this.client.getActiveAccount()) {
      this.client.setActiveAccount(this.client.getAllAccounts()[0] ?? null);
    }
  }

  async handleRedirect(): Promise<AuthenticationResult | null> {
    const result = await this.client.handleRedirectPromise();
    if (result?.account) this.client.setActiveAccount(result.account);
    return result;
  }

  async login(): Promise<void> {
    await this.initialize();
    await this.client.loginRedirect({ scopes: [this.config.apiScope] });
  }

  async logout(): Promise<void> {
    await this.initialize();
    const account = this.getActiveAccount();
    this.client.setActiveAccount(null);
    await this.client.logoutRedirect({
      account: account ?? undefined,
      postLogoutRedirectUri: this.config.postLogoutRedirectUri
    });
  }

  getActiveAccount(): AccountInfo | null {
    return this.client.getActiveAccount();
  }

  setActiveAccount(account: AccountInfo | null): void {
    this.client.setActiveAccount(account);
  }

  getIdTokenClaims(): Record<string, unknown> | null {
    return (this.getActiveAccount()?.idTokenClaims as Record<string, unknown> | undefined) ?? null;
  }

  getRoles(): ApplicationRole[] {
    return normalizeApplicationRoles(this.getIdTokenClaims()?.roles);
  }

  isAuthenticated(): boolean {
    return this.getActiveAccount() !== null;
  }

  getAuthenticatedUser(): AuthenticatedUser | null {
    const account = this.getActiveAccount();
    if (!account) return null;

    const claims = (account.idTokenClaims ?? {}) as Record<string, unknown>;
    const displayName = String(claims.name || account.name || account.username || 'Utilisateur Microsoft');
    const emailValue = claims.preferred_username || claims.email || claims.upn || account.username;
    const email = typeof emailValue === 'string' && emailValue ? emailValue : undefined;
    const roles = this.getRoles();
    const idValue = claims.oid || claims.sub || account.localAccountId || account.homeAccountId;

    return {
      id: String(idValue),
      displayName,
      email,
      roles,
      authenticationMode: 'entra',
      fullName: displayName,
      username: email ?? account.username,
      role: getPrimaryRole(roles),
      status: 'Actif'
    };
  }

  acquireAccessToken(): Promise<string> {
    if (!this.tokenPromise) {
      this.tokenPromise = this.acquireAccessTokenInternal().finally(() => {
        this.tokenPromise = null;
      });
    }
    return this.tokenPromise;
  }

  private async acquireAccessTokenInternal(): Promise<string> {
    await this.initialize();
    const account = this.getActiveAccount();
    if (!account) throw new AuthenticationRequiredError();

    const request = { account, scopes: [this.config.apiScope] };
    try {
      const result = await this.client.acquireTokenSilent(request);
      if (!result.accessToken) throw new AccessTokenAcquisitionError();
      return result.accessToken;
    } catch (error: unknown) {
      if (isInteractionRequired(error)) {
        await this.client.acquireTokenRedirect(request);
        throw new AuthenticationRequiredError('Une interaction Microsoft est requise pour renouveler votre session.');
      }
      if (error instanceof AccessTokenAcquisitionError) throw error;
      throw new AccessTokenAcquisitionError(undefined, { cause: error });
    }
  }
}

let singleton: MsalAuthenticationService | null = null;

export const getMsalService = (): MsalAuthenticationService => {
  if (!singleton) {
    const config = getAuthConfig();
    if (config.mode !== 'entra') {
      throw new AuthConfigurationError("Le service MSAL n’est disponible qu’en mode entra.");
    }
    singleton = new MsalAuthenticationService(config);
  }
  return singleton;
};

export const __resetMsalServiceForTests = (): void => {
  singleton = null;
};
