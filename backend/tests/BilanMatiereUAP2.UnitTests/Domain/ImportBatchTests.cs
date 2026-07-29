using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class ImportBatchTests
{
    [Fact]
    public void TerminerImport_PersisteLesCompteursCoherents()
    {
        var start = DateTime.UtcNow;
        var batch = ImportBatch.Demarrer(new string('A', 64), TypeImport.RapportMachine, "YRJMCH_V1", Guid.NewGuid(), "admin", start);

        batch.TerminerImport(StatutImport.Termine, start.AddSeconds(1), 10, 8, 2, 1, 5, 2, 3);

        Assert.Equal(10, batch.ReadCount);
        Assert.Equal(8, batch.ValidCount);
        Assert.Equal(2, batch.InsertedCount);
        Assert.Equal(1, batch.UpdatedCount);
        Assert.Equal(5, batch.UnchangedCount);
        Assert.Equal(2, batch.RejectedCount);
    }

    [Fact]
    public void TerminerImport_RefuseDesEquationsIncoherentes()
    {
        var start = DateTime.UtcNow;
        var batch = ImportBatch.Demarrer(new string('B', 64), TypeImport.MvtEntreeOF, "MVT_ENTREE_OF_V1", null, null, start);

        Assert.Throws<DomainValidationException>(() =>
            batch.TerminerImport(StatutImport.Termine, start.AddSeconds(1), 5, 4, 1, 1, 1, 1, 0));
    }

    [Fact]
    public void SignalerErreurArchive_ConserveDuplicateMaisStockeErreur()
    {
        var start = DateTime.UtcNow;
        var batch = ImportBatch.Demarrer(new string('C', 64), TypeImport.RapportMachine, "YRJMCH_V1", null, null, start);
        batch.TerminerImport(StatutImport.Duplicate, start.AddSeconds(1), 0, 0, 0, 0, 0, 0, 0);

        batch.SignalerErreurArchive("Stockage indisponible");

        Assert.Equal(StatutImport.Duplicate, batch.Statut);
        Assert.Equal("Stockage indisponible", batch.ArchiveErrorMessage);
    }
}
