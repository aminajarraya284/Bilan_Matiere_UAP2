using BilanMatiereUAP2.Contracts;

namespace BilanMatiereUAP2.Application.Abstractions;

public interface IManufacturingReferencesService
{
    Task<ImportReferenceResponse> ImportGammesAsync(Stream fileStream, string profilImport, string sourceSystem, string nomFichierOriginal, bool isCompleteExport = false, CancellationToken cancellationToken = default);
    Task<ImportReferenceResponse> ImportNomenclaturesAsync(Stream fileStream, string profilImport, string sourceSystem, string nomFichierOriginal, bool isCompleteExport = false, CancellationToken cancellationToken = default);
}
