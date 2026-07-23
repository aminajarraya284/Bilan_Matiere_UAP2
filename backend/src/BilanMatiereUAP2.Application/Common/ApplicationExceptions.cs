namespace BilanMatiereUAP2.Application.Common;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);

public sealed class ImportFileException(string message) : Exception(message);
