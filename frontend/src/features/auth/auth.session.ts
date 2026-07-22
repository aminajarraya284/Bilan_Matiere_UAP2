export const AUTH_TOKEN_STORAGE_KEY = 'sopal_auth_token';
export const AUTH_USER_STORAGE_KEY = 'sopal_auth_user';

let invalidateReactiveSession: (() => void) | undefined;

export const registerAuthSessionInvalidator = (invalidator: () => void): void => {
  invalidateReactiveSession = invalidator;
};

export const clearPersistedAuthSession = (): void => {
  localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
  localStorage.removeItem(AUTH_USER_STORAGE_KEY);
};

export const expireAuthSession = (): void => {
  clearPersistedAuthSession();
  invalidateReactiveSession?.();
};
