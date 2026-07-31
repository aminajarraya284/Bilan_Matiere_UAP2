using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public interface IParametreApplicationRepository
{
    Task<IReadOnlyList<ParametreApplication>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ParametreApplication?> GetByCleAsync(string cle, CancellationToken cancellationToken = default);
    Task<ParametreApplication?> GetByCleForUpdateAsync(string cle, CancellationToken cancellationToken = default);
}

public interface IParametreApplicationProvider
{
    Task<string?> GetValueAsync(string cle, CancellationToken cancellationToken = default);
    Task<decimal?> GetDecimalAsync(string cle, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetCsvValuesAsync(string cle, CancellationToken cancellationToken = default);
}
