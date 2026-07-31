using System.Security.Cryptography;
using System.Text;
using BilanMatiereUAP2.Application.Abstractions;

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class ImportHashCalculator : IImportHashCalculator
{
    public string CalculateSha256(byte[] content)
        => Convert.ToHexString(SHA256.HashData(content));

    public string CalculateCoupleHash(string firstHash, string secondHash)
        => CalculateSha256(Encoding.UTF8.GetBytes($"{firstHash}:{secondHash}"));
}
