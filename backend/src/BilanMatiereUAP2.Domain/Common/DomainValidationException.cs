namespace BilanMatiereUAP2.Domain.Common;

public sealed class DomainValidationException(string message) : Exception(message);
