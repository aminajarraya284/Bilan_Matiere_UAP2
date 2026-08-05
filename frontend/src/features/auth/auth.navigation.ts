const RETURN_PATH_KEY = 'sopal_auth_return_path';

const isSafeInternalPath = (path: string): boolean => path.startsWith('/') && !path.startsWith('//');

export const rememberAuthReturnPath = (path: string): void => {
  if (isSafeInternalPath(path) && path !== '/login' && path !== '/forbidden') {
    sessionStorage.setItem(RETURN_PATH_KEY, path);
  }
};

export const peekAuthReturnPath = (): string | null => sessionStorage.getItem(RETURN_PATH_KEY);

export const consumeAuthReturnPath = (): string | null => {
  const path = sessionStorage.getItem(RETURN_PATH_KEY);
  sessionStorage.removeItem(RETURN_PATH_KEY);
  return path && isSafeInternalPath(path) ? path : null;
};

export const clearAuthReturnPath = (): void => sessionStorage.removeItem(RETURN_PATH_KEY);
