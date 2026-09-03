namespace BilanMatiereUAP2.Application.Features.ImportsX3;

public sealed class ImportX3Options
{
    public const string SectionName = "ImportX3";
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>Unites conservees lors du rapprochement X3 (les autres sont ignorees).</summary>
    public string[] UnitesConservees { get; set; } = ["UN", "KG", "BA", "M", "LI"];
}
