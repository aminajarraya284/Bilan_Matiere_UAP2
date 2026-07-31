using BilanMatiereUAP2.Contracts.InventairesFonderie;
using FluentValidation;

namespace BilanMatiereUAP2.Application.Features.InventairesFonderie;

public sealed class CreateInventaireFonderieRequestValidator : AbstractValidator<CreateInventaireFonderieRequest>
{
    public CreateInventaireFonderieRequestValidator()
    {
        RuleFor(request => request.Periode)
            .NotEmpty().WithMessage("La periode est obligatoire.")
            .Matches(@"^\d{4}-(0[1-9]|1[0-2])$").WithMessage("La periode doit respecter le format YYYY-MM.");
        RuleFor(request => request.TypeMatiereId)
            .NotEmpty().WithMessage("Le type matiere est obligatoire.");
        RuleFor(request => request.StockDebutKg)
            .GreaterThanOrEqualTo(0).WithMessage("Le stock debut ne peut pas etre negatif.");
        RuleFor(request => request.StockFinKg)
            .GreaterThanOrEqualTo(0).WithMessage("Le stock fin ne peut pas etre negatif.");
        RuleFor(request => request.Observation)
            .MaximumLength(500).WithMessage("L'observation ne peut pas depasser 500 caracteres.");
    }
}

public sealed class UpdateInventaireFonderieRequestValidator : AbstractValidator<UpdateInventaireFonderieRequest>
{
    public UpdateInventaireFonderieRequestValidator()
    {
        RuleFor(request => request.Periode)
            .NotEmpty().WithMessage("La periode est obligatoire.")
            .Matches(@"^\d{4}-(0[1-9]|1[0-2])$").WithMessage("La periode doit respecter le format YYYY-MM.");
        RuleFor(request => request.TypeMatiereId)
            .NotEmpty().WithMessage("Le type matiere est obligatoire.");
        RuleFor(request => request.StockDebutKg)
            .GreaterThanOrEqualTo(0).WithMessage("Le stock debut ne peut pas etre negatif.");
        RuleFor(request => request.StockFinKg)
            .GreaterThanOrEqualTo(0).WithMessage("Le stock fin ne peut pas etre negatif.");
        RuleFor(request => request.Observation)
            .MaximumLength(500).WithMessage("L'observation ne peut pas depasser 500 caracteres.");
    }
}
