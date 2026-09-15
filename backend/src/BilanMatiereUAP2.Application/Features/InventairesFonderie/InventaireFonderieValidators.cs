using BilanMatiereUAP2.Contracts.InventairesFonderie;
using FluentValidation;

namespace BilanMatiereUAP2.Application.Features.InventairesFonderie;

public sealed class CreateInventaireConteneurValidator : AbstractValidator<CreateInventaireConteneurRequest>
{
    public CreateInventaireConteneurValidator()
    {
        RuleFor(x => x.DateInventaire)
            .NotEmpty().WithMessage("La date d'inventaire est obligatoire.");

        RuleFor(x => x.NumeroConteneur)
            .NotEmpty().WithMessage("Le numero de conteneur est obligatoire.")
            .MaximumLength(50).WithMessage("Le numero de conteneur ne peut pas depasser 50 caracteres.");

        RuleFor(x => x.NumeroOF)
            .NotEmpty().WithMessage("Le numero d'OF est obligatoire.")
            .MaximumLength(50).WithMessage("Le numero d'OF ne peut pas depasser 50 caracteres.");

        RuleFor(x => x.CodeTypeMatiere)
            .NotEmpty().WithMessage("Le code type matiere est obligatoire.")
            .MaximumLength(30).WithMessage("Le code type matiere ne peut pas depasser 30 caracteres.");

        RuleFor(x => x.PoidsReelKg)
            .GreaterThanOrEqualTo(0).WithMessage("Le poids reel ne peut pas etre negatif.");

        RuleFor(x => x.Observation)
            .MaximumLength(500).WithMessage("L'observation ne peut pas depasser 500 caracteres.");
    }
}

public sealed class UpdateInventaireConteneurValidator : AbstractValidator<UpdateInventaireConteneurRequest>
{
    public UpdateInventaireConteneurValidator()
    {
        RuleFor(x => x.DateInventaire)
            .NotEmpty().WithMessage("La date d'inventaire est obligatoire.");

        RuleFor(x => x.NumeroConteneur)
            .NotEmpty().WithMessage("Le numero de conteneur est obligatoire.")
            .MaximumLength(50).WithMessage("Le numero de conteneur ne peut pas depasser 50 caracteres.");

        RuleFor(x => x.NumeroOF)
            .NotEmpty().WithMessage("Le numero d'OF est obligatoire.")
            .MaximumLength(50).WithMessage("Le numero d'OF ne peut pas depasser 50 caracteres.");

        RuleFor(x => x.CodeTypeMatiere)
            .NotEmpty().WithMessage("Le code type matiere est obligatoire.")
            .MaximumLength(30).WithMessage("Le code type matiere ne peut pas depasser 30 caracteres.");

        RuleFor(x => x.PoidsReelKg)
            .GreaterThanOrEqualTo(0).WithMessage("Le poids reel ne peut pas etre negatif.");

        RuleFor(x => x.Observation)
            .MaximumLength(500).WithMessage("L'observation ne peut pas depasser 500 caracteres.");
    }
}

public sealed class CreateInventaireCategorieValidator : AbstractValidator<CreateInventaireCategorieRequest>
{
    public CreateInventaireCategorieValidator()
    {
        RuleFor(x => x.DateInventaire)
            .NotEmpty().WithMessage("La date d'inventaire est obligatoire.");

        RuleFor(x => x.Categorie)
            .IsInEnum().WithMessage("La categorie d'inventaire est invalide.");

        RuleFor(x => x.CodeTypeMatiere)
            .NotEmpty().WithMessage("Le code type matiere est obligatoire.")
            .MaximumLength(30).WithMessage("Le code type matiere ne peut pas depasser 30 caracteres.");

        RuleFor(x => x.PoidsReelKg)
            .GreaterThanOrEqualTo(0).WithMessage("Le poids reel ne peut pas etre negatif.");

        RuleFor(x => x.Commentaire)
            .MaximumLength(500).WithMessage("Le commentaire ne peut pas depasser 500 caracteres.");
    }
}

public sealed class UpdateInventaireCategorieValidator : AbstractValidator<UpdateInventaireCategorieRequest>
{
    public UpdateInventaireCategorieValidator()
    {
        RuleFor(x => x.DateInventaire)
            .NotEmpty().WithMessage("La date d'inventaire est obligatoire.");

        RuleFor(x => x.Categorie)
            .IsInEnum().WithMessage("La categorie d'inventaire est invalide.");

        RuleFor(x => x.CodeTypeMatiere)
            .NotEmpty().WithMessage("Le code type matiere est obligatoire.")
            .MaximumLength(30).WithMessage("Le code type matiere ne peut pas depasser 30 caracteres.");

        RuleFor(x => x.PoidsReelKg)
            .GreaterThanOrEqualTo(0).WithMessage("Le poids reel ne peut pas etre negatif.");

        RuleFor(x => x.Commentaire)
            .MaximumLength(500).WithMessage("Le commentaire ne peut pas depasser 500 caracteres.");
    }
}

public sealed class CreateInventaireFourValidator : AbstractValidator<CreateInventaireFourRequest>
{
    public CreateInventaireFourValidator()
    {
        RuleFor(x => x.DateInventaire)
            .NotEmpty().WithMessage("La date d'inventaire est obligatoire.");

        RuleFor(x => x.CodeFour)
            .NotEmpty().WithMessage("Le code du four est obligatoire.")
            .MaximumLength(50).WithMessage("Le code du four ne peut pas depasser 50 caracteres.");

        RuleFor(x => x.CodeTypeMatiere)
            .NotEmpty().WithMessage("Le code type matiere est obligatoire.")
            .MaximumLength(30).WithMessage("Le code type matiere ne peut pas depasser 30 caracteres.");

        RuleFor(x => x.QuantiteKg)
            .GreaterThanOrEqualTo(0).WithMessage("La quantite ne peut pas etre negative.");

        RuleFor(x => x.Observation)
            .MaximumLength(500).WithMessage("L'observation ne peut pas depasser 500 caracteres.");
    }
}

public sealed class UpdateInventaireFourValidator : AbstractValidator<UpdateInventaireFourRequest>
{
    public UpdateInventaireFourValidator()
    {
        RuleFor(x => x.DateInventaire)
            .NotEmpty().WithMessage("La date d'inventaire est obligatoire.");

        RuleFor(x => x.CodeFour)
            .NotEmpty().WithMessage("Le code du four est obligatoire.")
            .MaximumLength(50).WithMessage("Le code du four ne peut pas depasser 50 caracteres.");

        RuleFor(x => x.CodeTypeMatiere)
            .NotEmpty().WithMessage("Le code type matiere est obligatoire.")
            .MaximumLength(30).WithMessage("Le code type matiere ne peut pas depasser 30 caracteres.");

        RuleFor(x => x.QuantiteKg)
            .GreaterThanOrEqualTo(0).WithMessage("La quantite ne peut pas etre negative.");

        RuleFor(x => x.Observation)
            .MaximumLength(500).WithMessage("L'observation ne peut pas depasser 500 caracteres.");
    }
}

// Rétrocompatibilité
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
