using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BilanMatiereUAP2.Api.Authentication;

/// <summary>
/// Charge utile signee d'un token Development. Noms courts pour garder le token compact.
/// </summary>
public sealed record DevelopmentTokenPayload
{
    [JsonPropertyName("sub")] public Guid UserId { get; init; }
    [JsonPropertyName("usr")] public string Username { get; init; } = string.Empty;
    [JsonPropertyName("nam")] public string FullName { get; init; } = string.Empty;
    [JsonPropertyName("rol")] public string Role { get; init; } = string.Empty;
    [JsonPropertyName("exp")] public long ExpiresAtUnixSeconds { get; init; }
}

public interface IDevelopmentTokenService
{
    (string Token, DateTime ExpiresAtUtc) Issue(DevelopmentAccount account);
    DevelopmentTokenPayload? Validate(string? token);
}

/// <summary>
/// Emission et validation de tokens opaques signes (HMAC-SHA256), reservees a Development.
///
/// Ce n'est volontairement PAS un JWT : l'authentification de production passera par
/// Entra ID (voir TODO_METIER Entra ID dans Program.cs) et introduira alors le schema
/// JwtBearer. Un format distinct evite toute confusion entre un token de demonstration
/// et un jeton de production, et n'ajoute aucune dependance NuGet.
///
/// Cle de signature : Authentication:Development:SigningKey si presente (User Secrets ou
/// configuration Development), sinon une cle aleatoire regeneree a chaque demarrage.
/// Aucun secret n'est donc committe ; le seul effet du repli est que les tokens emis
/// avant un redemarrage du backend deviennent invalides (comportement acceptable en Dev).
/// </summary>
public sealed class DevelopmentTokenService : IDevelopmentTokenService
{
    public const string SigningKeyConfigurationPath = "Authentication:Development:SigningKey";
    public const string TokenLifetimeConfigurationPath = "Authentication:Development:TokenLifetimeMinutes";
    private const int DefaultLifetimeMinutes = 480;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly byte[] _signingKey;
    private readonly TimeSpan _lifetime;
    private readonly TimeProvider _timeProvider;

    public DevelopmentTokenService(IConfiguration configuration, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        var configuredKey = configuration[SigningKeyConfigurationPath];
        _signingKey = string.IsNullOrWhiteSpace(configuredKey)
            ? RandomNumberGenerator.GetBytes(32)
            : Encoding.UTF8.GetBytes(configuredKey);

        var lifetimeMinutes = configuration.GetValue<int?>(TokenLifetimeConfigurationPath) ?? DefaultLifetimeMinutes;
        _lifetime = TimeSpan.FromMinutes(lifetimeMinutes > 0 ? lifetimeMinutes : DefaultLifetimeMinutes);
    }

    public (string Token, DateTime ExpiresAtUtc) Issue(DevelopmentAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        var expiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.Add(_lifetime);
        var payload = new DevelopmentTokenPayload
        {
            UserId = account.UserId,
            Username = account.Username,
            FullName = account.FullName,
            Role = account.Role,
            ExpiresAtUnixSeconds = new DateTimeOffset(expiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds()
        };

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);
        var encodedPayload = Base64UrlEncode(payloadBytes);
        var signature = Base64UrlEncode(ComputeSignature(payloadBytes));

        return ($"{encodedPayload}.{signature}", expiresAtUtc);
    }

    public DevelopmentTokenPayload? Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var separatorIndex = token.IndexOf('.');
        if (separatorIndex <= 0 || separatorIndex == token.Length - 1)
        {
            return null;
        }

        byte[] payloadBytes;
        byte[] providedSignature;
        try
        {
            payloadBytes = Base64UrlDecode(token[..separatorIndex]);
            providedSignature = Base64UrlDecode(token[(separatorIndex + 1)..]);
        }
        catch (FormatException)
        {
            return null;
        }

        // Comparaison a temps constant : evite de fuiter la signature attendue.
        if (!CryptographicOperations.FixedTimeEquals(ComputeSignature(payloadBytes), providedSignature))
        {
            return null;
        }

        DevelopmentTokenPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<DevelopmentTokenPayload>(payloadBytes, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (payload is null)
        {
            return null;
        }

        var nowUnixSeconds = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        return payload.ExpiresAtUnixSeconds <= nowUnixSeconds ? null : payload;
    }

    private byte[] ComputeSignature(byte[] payloadBytes) => HMACSHA256.HashData(_signingKey, payloadBytes);

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding is 2 or 3)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }
        else if (padding == 1)
        {
            throw new FormatException("Longueur base64url invalide.");
        }

        return Convert.FromBase64String(normalized);
    }
}
