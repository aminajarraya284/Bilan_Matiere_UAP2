using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts.Parametres;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Audit;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Parametres;

public interface IParametreApplicationService : IParametreApplicationProvider
{
    Task<IReadOnlyList<ParametreApplicationResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ParametreApplicationResponse> UpdateAsync(
        string cle,
        UpdateParametreApplicationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ParametreApplicationService(
    IParametreApplicationRepository repository,
    IUnitOfWork unitOfWork,
    IAuditService? auditService = null) : IParametreApplicationService
{
    public async Task<IReadOnlyList<ParametreApplicationResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var parameters = await repository.GetAllAsync(cancellationToken);
        return parameters.Select(ToResponse).ToArray();
    }

    public async Task<string?> GetValueAsync(string cle, CancellationToken cancellationToken = default)
    {
        var parameter = await repository.GetByCleAsync(BilanMetierRules.NormaliserCle(cle), cancellationToken);
        return string.IsNullOrWhiteSpace(parameter?.Valeur) ? null : parameter.Valeur.Trim();
    }

    public async Task<decimal?> GetDecimalAsync(string cle, CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(cle, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    public async Task<IReadOnlyList<string>> GetCsvValuesAsync(string cle, CancellationToken cancellationToken = default)
    {
        var value = await GetValueAsync(cle, cancellationToken);
        return string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(BilanMetierRules.NormaliserCle)
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    public async Task<ParametreApplicationResponse> UpdateAsync(
        string cle,
        UpdateParametreApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = BilanMetierRules.NormaliserCle(cle);
        var parameter = await repository.GetByCleForUpdateAsync(normalizedKey, cancellationToken)
            ?? throw new NotFoundException("Parametre application introuvable.");

        if (!EstModifiable(normalizedKey))
        {
            throw new ValidationException(
                [new ValidationFailure("cle", "Ce parametre est alimente par un referentiel dedie et ne peut pas etre modifie ici.")]);
        }

        var normalizedValue = ValiderEtNormaliserValeur(normalizedKey, request.Valeur);
        var ancienneValeur = parameter.Valeur;
        parameter.ModifierValeur(normalizedValue);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (auditService is not null && !string.Equals(ancienneValeur, parameter.Valeur, StringComparison.Ordinal))
        {
            await auditService.EnregistrerAsync(
                new AuditEntry(
                    ActionAudit.Modification,
                    DomaineAudit.Administration,
                    nameof(ParametreApplication),
                    normalizedKey,
                    normalizedKey,
                    nameof(ParametreApplicationService),
                    $"Modification du paramètre {normalizedKey}.",
                    new Dictionary<string, object?> { ["valeur"] = ancienneValeur },
                    new Dictionary<string, object?> { ["valeur"] = parameter.Valeur }),
                cancellationToken);
        }
        return ToResponse(parameter);
    }

    private static ParametreApplicationResponse ToResponse(ParametreApplication parameter)
        => new(
            parameter.Cle,
            parameter.Valeur,
            parameter.Description,
            EstModifiable(parameter.Cle),
            EstDecimal(parameter.Cle) ? "Decimal" : "Texte");

    private static bool EstModifiable(string cle)
        => !string.Equals(cle, ParametresApplicationCles.TypesMatiere, StringComparison.Ordinal);

    private static bool EstDecimal(string cle)
        => cle is ParametresApplicationCles.SeuilEcartFonderie or ParametresApplicationCles.SeuilEcartUap2;

    private static string ValiderEtNormaliserValeur(string cle, string? valeur)
    {
        var normalized = valeur?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ValidationException([new ValidationFailure("valeur", "La valeur est obligatoire.")]);
        }

        if (normalized.Length > 500)
        {
            throw new ValidationException([new ValidationFailure("valeur", "La valeur ne peut pas depasser 500 caracteres.")]);
        }

        if (EstDecimal(cle))
        {
            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) || parsed < 0m)
            {
                throw new ValidationException(
                    [new ValidationFailure("valeur", "La valeur doit etre un nombre decimal positif ou nul au format invariant.")]);
            }

            return parsed.ToString("0.############################", CultureInfo.InvariantCulture);
        }

        return normalized;
    }
}
