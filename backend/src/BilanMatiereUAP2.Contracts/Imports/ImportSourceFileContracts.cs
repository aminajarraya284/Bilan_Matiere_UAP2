namespace BilanMatiereUAP2.Contracts.Imports;

public sealed record ImportSourceFileResponse(
    Guid Id,
    Guid ImportBatchId,
    string TypeImport,
    string? ProfilImport,
    string NomFichierOriginal,
    string Extension,
    long TailleOctets,
    string HashSha256,
    DateTime DateUploadUtc,
    string? UploadeParNom);
