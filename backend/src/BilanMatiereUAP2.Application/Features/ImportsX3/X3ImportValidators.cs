using BilanMatiereUAP2.Application.Abstractions;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace BilanMatiereUAP2.Application.Features.ImportsX3;

public sealed class ImportSortiesMatiereX3CommandValidator : AbstractValidator<ImportSortiesMatiereX3Command>
{
    public ImportSortiesMatiereX3CommandValidator(IOptions<ImportX3Options> options)
    {
        var maxFileSize = options.Value.MaxFileSizeBytes;
        RuleFor(command => command.EncoursProductFile)
            .NotNull().WithMessage("Le fichier EncoursProduct est obligatoire.")
            .SetValidator(new X3ImportFileContentValidator(maxFileSize)!);
        RuleFor(command => command.MouvementsStockFile)
            .NotNull().WithMessage("Le fichier MVTSTOTRSVCR est obligatoire.")
            .SetValidator(new X3ImportFileContentValidator(maxFileSize)!);
    }
}

internal sealed class X3ImportFileContentValidator : AbstractValidator<X3ImportFileContent?>
{
    public X3ImportFileContentValidator(long maxFileSize)
    {
        When(file => file is not null, () =>
        {
            RuleFor(file => file!.FileName)
                .NotEmpty().WithMessage("Le nom du fichier est obligatoire.")
                .Must(name => Path.GetExtension(name) is ".xls" or ".xlsx" ||
                              Path.GetExtension(name).Equals(".xls", StringComparison.OrdinalIgnoreCase) ||
                              Path.GetExtension(name).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Seuls les fichiers .xls et .xlsx sont autorises.");
            RuleFor(file => file!.Length)
                .GreaterThan(0).WithMessage("Le fichier est vide.")
                .LessThanOrEqualTo(maxFileSize)
                .WithMessage($"Le fichier depasse la taille maximale de {maxFileSize} octets.");
            RuleFor(file => file!.Content)
                .NotEmpty().WithMessage("Le fichier est vide.");
        });
    }
}
