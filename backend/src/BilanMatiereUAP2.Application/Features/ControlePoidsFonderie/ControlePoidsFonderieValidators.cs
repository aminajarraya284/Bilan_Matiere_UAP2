using BilanMatiereUAP2.Contracts.ControlePoidsFonderie;
using FluentValidation;

namespace BilanMatiereUAP2.Application.Features.ControlePoidsFonderie;

public sealed class CreateControlePoidsFonderieRequestValidator : AbstractValidator<CreateControlePoidsFonderieRequest>
{
    public CreateControlePoidsFonderieRequestValidator()
    {
        Include(new ControlePoidsFonderieRequestValidator<CreateControlePoidsFonderieRequest>(
            request => request.DateControle,
            request => request.CodeArticle,
            request => request.TypeMatiereId,
            request => request.PoidsPhysiqueUnitaireKg,
            request => request.NombreEchantillons,
            request => request.Observation));
    }
}

public sealed class UpdateControlePoidsFonderieRequestValidator : AbstractValidator<UpdateControlePoidsFonderieRequest>
{
    public UpdateControlePoidsFonderieRequestValidator()
    {
        Include(new ControlePoidsFonderieRequestValidator<UpdateControlePoidsFonderieRequest>(
            request => request.DateControle,
            request => request.CodeArticle,
            request => request.TypeMatiereId,
            request => request.PoidsPhysiqueUnitaireKg,
            request => request.NombreEchantillons,
            request => request.Observation));
    }
}

internal sealed class ControlePoidsFonderieRequestValidator<T> : AbstractValidator<T>
{
    public ControlePoidsFonderieRequestValidator(
        System.Linq.Expressions.Expression<Func<T, DateOnly>> dateControle,
        System.Linq.Expressions.Expression<Func<T, string>> codeArticle,
        System.Linq.Expressions.Expression<Func<T, Guid>> typeMatiereId,
        System.Linq.Expressions.Expression<Func<T, decimal>> poidsPhysiqueUnitaireKg,
        System.Linq.Expressions.Expression<Func<T, int?>> nombreEchantillons,
        System.Linq.Expressions.Expression<Func<T, string?>> observation)
    {
        RuleFor(dateControle).NotEqual(default(DateOnly)).WithMessage("La date de contrôle est obligatoire.");
        RuleFor(codeArticle).NotEmpty().WithMessage("Le code article est obligatoire.").MaximumLength(80);
        RuleFor(typeMatiereId).NotEmpty().WithMessage("Le type matière est obligatoire.");
        RuleFor(poidsPhysiqueUnitaireKg)
            .GreaterThan(0).WithMessage("Le poids physique unitaire doit être strictement positif.");
        RuleFor(nombreEchantillons)
            .GreaterThan(0).When(request => nombreEchantillons.Compile()(request).HasValue)
            .WithMessage("Le nombre d'échantillons doit être strictement positif.");
        RuleFor(observation).MaximumLength(500);
    }
}
