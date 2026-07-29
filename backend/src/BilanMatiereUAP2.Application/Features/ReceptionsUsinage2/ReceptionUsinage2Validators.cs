using BilanMatiereUAP2.Contracts.ReceptionsUsinage2;
using FluentValidation;

namespace BilanMatiereUAP2.Application.Features.ReceptionsUsinage2;

public sealed class CreateReceptionUsinage2RequestValidator : AbstractValidator<CreateReceptionUsinage2Request>
{
    public CreateReceptionUsinage2RequestValidator()
    {
        RuleFor(request => request.NumeroOF).NotEmpty().WithMessage("Le numéro d'OF est obligatoire.").MaximumLength(50);
        RuleFor(request => request.DateReception).NotEqual(default(DateOnly)).WithMessage("La date de réception est obligatoire.");
        RuleFor(request => request.NombrePiecesReceptionnees).GreaterThan(0).WithMessage("Le nombre de pièces réceptionnées doit être strictement positif.");
        RuleFor(request => request.PoidsTotalReceptionneKg).GreaterThanOrEqualTo(0)
            .When(request => request.PoidsTotalReceptionneKg is not null)
            .WithMessage("Le poids total réceptionné ne peut pas être négatif.");
        RuleFor(request => request.Observation).MaximumLength(500);
    }
}

public sealed class UpdateReceptionUsinage2RequestValidator : AbstractValidator<UpdateReceptionUsinage2Request>
{
    public UpdateReceptionUsinage2RequestValidator()
    {
        RuleFor(request => request.NombrePiecesReceptionnees).GreaterThan(0).WithMessage("Le nombre de pièces réceptionnées doit être strictement positif.");
        RuleFor(request => request.PoidsTotalReceptionneKg).GreaterThanOrEqualTo(0)
            .When(request => request.PoidsTotalReceptionneKg is not null)
            .WithMessage("Le poids total réceptionné ne peut pas être négatif.");
        RuleFor(request => request.Observation).MaximumLength(500);
    }
}
