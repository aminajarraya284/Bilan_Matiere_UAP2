namespace BilanMatiereUAP2.Application.Abstractions;

public interface IImportHashCalculator
{
    string CalculateSha256(byte[] content);
    string CalculateCoupleHash(string firstHash, string secondHash);
}
