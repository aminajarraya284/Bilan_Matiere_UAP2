using BilanMatiereUAP2.Contracts.TypesMatiere;
using FluentValidation;

namespace BilanMatiereUAP2.Application.Features.TypesMatiere;

public sealed class CreateTypeMatiereRequestValidator : AbstractValidator<CreateTypeMatiereRequest>
{
    public CreateTypeMatiereRequestValidator()
    {
        RuleFor(request => request.Code)
            .NotEmpty().WithMessage("Le code est obligatoire.")
            .MaximumLength(30).WithMessage("Le code ne peut pas dépasser 30 caractères.");

        RuleFor(request => request.Libelle)
            .NotEmpty().WithMessage("Le libellé est obligatoire.")
            .MaximumLength(100).WithMessage("Le libellé ne peut pas dépasser 100 caractères.");
    }
}

public sealed class UpdateTypeMatiereRequestValidator : AbstractValidator<UpdateTypeMatiereRequest>
{
    public UpdateTypeMatiereRequestValidator()
    {
        RuleFor(request => request.Libelle)
            .NotEmpty().WithMessage("Le libellé est obligatoire.")
            .MaximumLength(100).WithMessage("Le libellé ne peut pas dépasser 100 caractères.");
    }
}
