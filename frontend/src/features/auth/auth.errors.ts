export class AuthConfigurationError extends Error {
  constructor(message = 'Configuration Microsoft Entra ID incomplète.') {
    super(message);
    this.name = 'AuthConfigurationError';
  }
}

export class AuthenticationRequiredError extends Error {
  constructor(message = 'Votre session a expiré. Veuillez vous reconnecter.') {
    super(message);
    this.name = 'AuthenticationRequiredError';
  }
}

export class AccessTokenAcquisitionError extends Error {
  constructor(message = "Impossible de récupérer le jeton d’accès.", options?: ErrorOptions) {
    super(message, options);
    this.name = 'AccessTokenAcquisitionError';
  }
}

export class MissingApplicationRoleError extends Error {
  constructor(message = 'Votre compte ne possède pas de rôle autorisé.') {
    super(message);
    this.name = 'MissingApplicationRoleError';
  }
}

export class AuthorizationError extends Error {
  constructor(message = "Vous n’avez pas l’autorisation d’accéder à cet écran.") {
    super(message);
    this.name = 'AuthorizationError';
  }
}

export const toAuthUserMessage = (error: unknown): string => {
  if (error instanceof AuthConfigurationError) return error.message;
  if (error instanceof AuthenticationRequiredError) return error.message;
  if (error instanceof AccessTokenAcquisitionError) return error.message;
  if (error instanceof MissingApplicationRoleError) return error.message;
  if (error instanceof AuthorizationError) return error.message;
  return "Une erreur d’authentification est survenue.";
};
