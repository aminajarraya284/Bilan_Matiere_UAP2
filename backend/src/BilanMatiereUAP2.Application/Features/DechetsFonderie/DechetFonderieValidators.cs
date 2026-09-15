using BilanMatiereUAP2.Contracts.DechetsFonderie;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;

namespace BilanMatiereUAP2.Application.Features.DechetsFonderie;

public sealed class CreateDechetFonderieRequestValidator : AbstractValidator<CreateDechetFonderieRequest>
{
    public CreateDechetFonderieRequestValidator()
    {
        Include(new DechetFonderieRequestValidator<CreateDechetFonderieRequest>(
            request => request.DateDeclaration,
            request => request.TypeMatiereId,
            request => request.TypeDechet,
            request => request.PoidsKg,
            request => request.Observation,
            request => request.Origine));
    }
}

public sealed class UpdateDechetFonderieRequestValidator : AbstractValidator<UpdateDechetFonderieRequest>
{
    public UpdateDechetFonderieRequestValidator()
    {
        Include(new DechetFonderieRequestValidator<UpdateDechetFonderieRequest>(
            request => request.DateDeclaration,
            request => request.TypeMatiereId,
            request => request.TypeDechet,
            request => request.PoidsKg,
            request => request.Observation,
            request => request.Origine));
    }
}

internal sealed class DechetFonderieRequestValidator<T> : AbstractValidator<T>
{
    public DechetFonderieRequestValidator(
        System.Linq.Expressions.Expression<Func<T, DateOnly>> dateDeclaration,
        System.Linq.Expressions.Expression<Func<T, Guid>> typeMatiereId,
        System.Linq.Expressions.Expression<Func<T, string>> typeDechet,
        System.Linq.Expressions.Expression<Func<T, decimal>> poidsKg,
        System.Linq.Expressions.Expression<Func<T, string?>> observation,
        System.Linq.Expressions.Expression<Func<T, string>> origine)
    {
        RuleFor(dateDeclaration)
            .NotEqual(default(DateOnly)).WithMessage("La date de déclaration est obligatoire.");
        RuleFor(typeMatiereId)
            .NotEmpty().WithMessage("Le type matière est obligatoire.");
        RuleFor(typeDechet)
            .NotEmpty().WithMessage("Le type de déchet est obligatoire.")
            .Must(BeValidTypeDechet).WithMessage("Le type de déchet doit être Bavure ou Poudre.");
        RuleFor(poidsKg)
            .GreaterThan(0).WithMessage("Le poids doit être supérieur à zéro.");
        RuleFor(observation)
            .MaximumLength(500).WithMessage("L’observation ne peut pas dépasser 500 caractères.");
        RuleFor(origine)
            .NotEmpty().WithMessage("L'origine est obligatoire.")
            .Must(BeValidOrigine).WithMessage("L'origine doit etre Fonderie, SCI, EMR, TRN ou U2.");
    }

    private static bool BeValidTypeDechet(string value)
    {
        return Enum.TryParse<TypeDechet>(value, true, out var parsed) && Enum.IsDefined(parsed);
    }

    private static bool BeValidOrigine(string value)
    {
        return Enum.TryParse<OrigineDechet>(value, true, out var parsed) && Enum.IsDefined(parsed);
    }
}
