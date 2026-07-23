using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Contracts.ImportsX3;
using BilanMatiereUAP2.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class X3ImportServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ImportAsync_FiltersMovementsByAllowedMaterialTypes()
    {
        var encoursRows = new List<EncoursProductRow>
        {
            new(
                NumeroLigneSource: 2,
                NumeroOF: "OF001",
                Article: "ART001",
                DesignationArticle: "Article test",
                SituationOF: "En cours",
                Atelier: "Fonderie",
                NumeroOperation: "10",
                CodePoste: "POSTE1",
                TypePoste: "MO",
                NumeroPoste: "1",
                EffectifPoste: "1",
                Cad: "10",
                EffectifOperation: "1",
                QuantitePrevue: 100m,
                QuantiteRestante: 0m,
                DateOperation: new DateOnly(2026, 6, 1),
                DateObjectifOF: new DateOnly(2026, 6, 30),
                Statut: "OK"),
            new(
                NumeroLigneSource: 3,
                NumeroOF: "PASOF001",
                Article: "ART002",
                DesignationArticle: "Article sans OF",
                SituationOF: "En cours",
                Atelier: "Fonderie",
                NumeroOperation: "10",
                CodePoste: "POSTE1",
                TypePoste: "MO",
                NumeroPoste: "1",
                EffectifPoste: "1",
                Cad: "10",
                EffectifOperation: "1",
                QuantitePrevue: 100m,
                QuantiteRestante: 0m,
                DateOperation: new DateOnly(2026, 6, 1),
                DateObjectifOF: new DateOnly(2026, 6, 30),
                Statut: "OK")
        };

        var mouvementRows = new List<MouvementStockX3Row>
        {
            new(
                NumeroLigneSource: 2,
                DateImputation: new DateOnly(2026, 6, 15),
                Code: "M3000L0",
                Designation: "Matière M3000L0",
                DateCreation: new DateOnly(2026, 6, 15),
                HeureCreation: "10:00",
                Utilisateur: "USER1",
                QuantiteX3Signee: 50m,
                Unite: "KG",
                Transaction: "Sortie OF",
                TypePiece: "Suivi matière",
                Lot: "LOT1",
                PieceOrigine: "OF001",
                NumeroOFNormalise: "OF001",
                DesignationMouvement: "Sortie MP",
                Emplacement: "EMP1",
                Statut: "OK"),

            new(
                NumeroLigneSource: 3,
                DateImputation: new DateOnly(2026, 6, 15),
                Code: "AUTRE99",
                Designation: "Matière Autre Non Autorisée",
                DateCreation: new DateOnly(2026, 6, 15),
                HeureCreation: "10:05",
                Utilisateur: "USER1",
                QuantiteX3Signee: 100m,
                Unite: "KG",
                Transaction: "Sortie OF",
                TypePiece: "Suivi matière",
                Lot: "LOT2",
                PieceOrigine: "OF001",
                NumeroOFNormalise: "OF001",
                DesignationMouvement: "Sortie MP Autre",
                Emplacement: "EMP1",
                Statut: "OK"),

            new(
                NumeroLigneSource: 4,
                DateImputation: new DateOnly(2026, 6, 16),
                Code: "M3060L0",
                Designation: "Matière M3060L0",
                DateCreation: new DateOnly(2026, 6, 16),
                HeureCreation: "11:00",
                Utilisateur: "USER1",
                QuantiteX3Signee: 30m,
                Unite: "KG",
                Transaction: "Sortie OF",
                TypePiece: "Suivi matière",
                Lot: "LOT3",
                PieceOrigine: "OF001",
                NumeroOFNormalise: "OF001",
                DesignationMouvement: "Sortie MP 3060",
                Emplacement: "EMP1",
                Statut: "OK"),

            new(
                NumeroLigneSource: 5,
                DateImputation: new DateOnly(2026, 6, 15),
                Code: "M3000L0",
                Designation: "Matière M3000L0",
                DateCreation: new DateOnly(2026, 6, 15),
                HeureCreation: "10:00",
                Utilisateur: "USER1",
                QuantiteX3Signee: 50m,
                Unite: "KG",
                Transaction: "Mauvaise Transac",
                TypePiece: "Suivi matière",
                Lot: "LOT1",
                PieceOrigine: "OF001",
                NumeroOFNormalise: "OF001",
                DesignationMouvement: "Sortie MP",
                Emplacement: "EMP1",
                Statut: "OK"),

            new(
                NumeroLigneSource: 6,
                DateImputation: new DateOnly(2026, 6, 15),
                Code: "M3000L0",
                Designation: "Matière M3000L0",
                DateCreation: new DateOnly(2026, 6, 15),
                HeureCreation: "10:00",
                Utilisateur: "USER1",
                QuantiteX3Signee: 50m,
                Unite: "KG",
                Transaction: "Sortie OF",
                TypePiece: "Mauvais Type",
                Lot: "LOT1",
                PieceOrigine: "OF001",
                NumeroOFNormalise: "OF001",
                DesignationMouvement: "Sortie MP",
                Emplacement: "EMP1",
                Statut: "OK"),

            new(
                NumeroLigneSource: 7,
                DateImputation: new DateOnly(2026, 6, 15),
                Code: "M3000L0",
                Designation: "Matière M3000L0",
                DateCreation: new DateOnly(2026, 6, 15),
                HeureCreation: "10:00",
                Utilisateur: "USER1",
                QuantiteX3Signee: 50m,
                Unite: "KG",
                Transaction: "Sortie OF",
                TypePiece: "Suivi matière",
                Lot: "LOT1",
                PieceOrigine: "PASOF001",
                NumeroOFNormalise: "PASOF001",
                DesignationMouvement: "Sortie MP",
                Emplacement: "EMP1",
                Statut: "OK")
        };

        var encoursReader = new FakeEncoursReader(encoursRows);
        var mouvementReader = new FakeMouvementReader(mouvementRows);
        var repository = new FakeX3ImportRepository();
        var currentUser = new FakeCurrentUserService();
        var dateTimeProvider = new FakeDateTimeProvider();
        var validator = new FakeValidator();

        var service = new X3ImportService(
            encoursReader,
            mouvementReader,
            repository,
            currentUser,
            dateTimeProvider,
            validator);

        var command = new ImportSortiesMatiereX3Command(
            new X3ImportFileContent("Encours.xlsx", 100, [1, 2, 3]),
            new X3ImportFileContent("Mouvements.xlsx", 100, [4, 5, 6]));

        var response = await service.ImportAsync(command, CancellationToken.None);

        Assert.Equal(2, response.SortiesGenerees);
        Assert.Equal(2, response.Consommations.Count);
        Assert.Contains(response.Consommations, c => c.CodeComposant == "M3000L0");
        Assert.Contains(response.Consommations, c => c.CodeComposant == "M3060L0");
        Assert.DoesNotContain(response.Consommations, c => c.CodeComposant == "AUTRE99");
    }

    private sealed class FakeEncoursReader(IReadOnlyList<EncoursProductRow> rows) : IEncoursProductReader
    {
        public Task<X3ReadResult<EncoursProductRow>> ReadAsync(X3ImportFileContent file, CancellationToken cancellationToken)
            => Task.FromResult(new X3ReadResult<EncoursProductRow>(rows, [], rows.Count));
    }

    private sealed class FakeMouvementReader(IReadOnlyList<MouvementStockX3Row> rows) : IMouvementStockX3Reader
    {
        public Task<X3ReadResult<MouvementStockX3Row>> ReadAsync(X3ImportFileContent file, CancellationToken cancellationToken)
            => Task.FromResult(new X3ReadResult<MouvementStockX3Row>(rows, [], rows.Count));
    }

    private sealed class FakeX3ImportRepository : IX3ImportRepository
    {
        public Task<ImportX3Batch?> GetBatchByPairHashAsync(string pairHash, CancellationToken cancellationToken) => Task.FromResult<ImportX3Batch?>(null);
        public Task<ImportX3Batch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<ImportX3Batch?>(null);
        public Task<IReadOnlyList<ImportX3Anomaly>> GetAnomaliesAsync(Guid batchId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ImportX3Anomaly>>([]);
        public Task<IReadOnlyDictionary<string, X3OrdreFabrication>> GetOrdresByNumeroAsync(IReadOnlyCollection<string> numerosOF, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<string, X3OrdreFabrication>>(new Dictionary<string, X3OrdreFabrication>());
        public Task<X3OrdreFabrication?> GetOrdreContexteAsync(string numeroOF, CancellationToken cancellationToken) => Task.FromResult<X3OrdreFabrication?>(null);
        public Task<IReadOnlyDictionary<string, Composant>> GetComposantsByCodeAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<string, Composant>>(new Dictionary<string, Composant>());
        public Task<PagedResult<ConsommationComposant>> GetConsommationsAsync(ConsommationsComposantsFilter filter, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<ConsommationComposant>([], 0));
        public Task AddBatchAsync(ImportX3Batch batch, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddOrdresAsync(IEnumerable<X3OrdreFabrication> ordres, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddComposantsAsync(IEnumerable<Composant> composants, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => Now;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("10000000-0000-0000-0000-000000000001");
        public string UserName => "Test User";
        public string Role => "Admin";
        public bool IsAuthenticated => true;
    }

    private sealed class FakeValidator : IValidator<ImportSortiesMatiereX3Command>
    {
        public ValidationResult Validate(ImportSortiesMatiereX3Command instance) => new();
        public ValidationResult Validate(IValidationContext context) => new();
        public Task<ValidationResult> ValidateAsync(ImportSortiesMatiereX3Command instance, CancellationToken cancellation = default) => Task.FromResult(new ValidationResult());
        public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default) => Task.FromResult(new ValidationResult());
        public IValidatorDescriptor CreateDescriptor() => throw new NotImplementedException();
        public bool CanValidateInstancesOfType(Type type) => true;
    }
}
