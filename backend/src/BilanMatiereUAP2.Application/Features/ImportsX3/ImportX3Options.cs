namespace BilanMatiereUAP2.Application.Features.ImportsX3;

public sealed class ImportX3Options
{
    public const string SectionName = "ImportX3";
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>Codes de types matiere retenus comme sorties matiere lors du rapprochement X3.</summary>
    public string[] TypesMatiereAutorises { get; set; } = ["M3000L0", "M3010L0", "M3060L0"];

    /// <summary>Unites conservees lors du rapprochement X3 (les autres sont ignorees).</summary>
    public string[] UnitesConservees { get; set; } = ["UN", "KG", "BA", "M", "LI"];
}
