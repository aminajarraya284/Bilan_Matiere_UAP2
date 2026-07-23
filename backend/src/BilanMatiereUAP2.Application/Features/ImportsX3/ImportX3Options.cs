namespace BilanMatiereUAP2.Application.Features.ImportsX3;

public sealed class ImportX3Options
{
    public const string SectionName = "ImportX3";
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
}
