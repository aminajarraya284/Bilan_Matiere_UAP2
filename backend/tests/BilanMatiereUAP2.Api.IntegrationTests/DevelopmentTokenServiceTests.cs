using BilanMatiereUAP2.Api.Authentication;
using Microsoft.Extensions.Configuration;

namespace BilanMatiereUAP2.Api.IntegrationTests;

/// <summary>
/// Emission et validation des tokens de connexion locale Development.
/// </summary>
public sealed class DevelopmentTokenServiceTests
{
    private static readonly DevelopmentAccount Account = new(
        Id: 1,
        UserId: Guid.Parse("de000000-0000-0000-0000-000000000001"),
        Username: "admin",
        FullName: "Administrateur SOPAL",
        Email: "admin@sopal.com",
        Role: "Admin");

    /// <summary>
    /// Valeur de test uniquement, sans rapport avec un quelconque secret reel : les mots de
    /// passe de developpement vivent dans les User Secrets du poste, jamais dans le depot.
    /// </summary>
    private const string MotDePasseDeTest = "valeur-de-test-non-secrete";

    private static IDevelopmentAccountAuthenticator BuildAuthenticator(
        params (string Username, string? Password)[] comptesConfigures)
    {
        var settings = comptesConfigures.ToDictionary(
            compte => $"{DevelopmentAccounts.ConfigurationSection}:{compte.Username}:Password",
            compte => compte.Password);

        return new ConfigurationDevelopmentAccountAuthenticator(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    private static DevelopmentTokenService BuildService(
        TimeProvider timeProvider,
        string? signingKey = "cle-de-test-uniquement-pour-les-tests",
        int? lifetimeMinutes = 60)
    {
        var settings = new Dictionary<string, string?>
        {
            [DevelopmentTokenService.SigningKeyConfigurationPath] = signingKey,
            [DevelopmentTokenService.TokenLifetimeConfigurationPath] = lifetimeMinutes?.ToString()
        };

        return new DevelopmentTokenService(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            timeProvider);
    }

    [Fact]
    public void Issue_PuisValidate_RestitueLesClaimsDuCompte()
    {
        var service = BuildService(TimeProvider.System);

        var (token, expiresAtUtc) = service.Issue(Account);
        var payload = service.Validate(token);

        Assert.NotNull(payload);
        Assert.Equal(Account.UserId, payload!.UserId);
        Assert.Equal("admin", payload.Username);
        Assert.Equal("Admin", payload.Role);
        Assert.Equal("Administrateur SOPAL", payload.FullName);
        Assert.True(expiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void Validate_ApresExpiration_RetourneNull()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 7, 27, 8, 0, 0, TimeSpan.Zero));
        var service = BuildService(clock, lifetimeMinutes: 30);

        var (token, _) = service.Issue(Account);
        Assert.NotNull(service.Validate(token));

        clock.Advance(TimeSpan.FromMinutes(31));

        Assert.Null(service.Validate(token));
    }

    [Fact]
    public void Validate_AvecSignatureAlteree_RetourneNull()
    {
        var service = BuildService(TimeProvider.System);
        var (token, _) = service.Issue(Account);

        var tampered = string.Concat(token.AsSpan(0, token.IndexOf('.') + 1), "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

        Assert.Null(service.Validate(tampered));
    }

    [Fact]
    public void Validate_AvecChargeUtileModifiee_RetourneNull()
    {
        var service = BuildService(TimeProvider.System);
        var (token, _) = service.Issue(Account);
        var signature = token[(token.IndexOf('.') + 1)..];

        // Charge utile reecrite pour tenter une elevation de privilege : la signature ne correspond plus.
        var forgedPayload = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes("""{"sub":"de000000-0000-0000-0000-000000000009","usr":"pirate","nam":"Pirate","rol":"Admin","exp":99999999999}"""))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        Assert.Null(service.Validate($"{forgedPayload}.{signature}"));
    }

    [Fact]
    public void Validate_AvecCleDeSignatureDifferente_RetourneNull()
    {
        var (token, _) = BuildService(TimeProvider.System, signingKey: "premiere-cle").Issue(Account);
        var autreService = BuildService(TimeProvider.System, signingKey: "seconde-cle");

        Assert.Null(autreService.Validate(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sans-separateur")]
    [InlineData(".")]
    [InlineData("!!!.???")]
    public void Validate_AvecTokenMalforme_RetourneNull(string? token)
    {
        var service = BuildService(TimeProvider.System);

        Assert.Null(service.Validate(token));
    }

    [Theory]
    [InlineData("admin", "Admin")]
    [InlineData("ADMIN", "Admin")]
    [InlineData("fonderie", "ResponsableFonderie")]
    [InlineData("usinage2", "ResponsableUsinage2")]
    public void Authenticate_CompteConfigureEtBonMotDePasse_RetourneLeCompte(string username, string role)
    {
        var authenticator = BuildAuthenticator(
            ("admin", MotDePasseDeTest),
            ("fonderie", MotDePasseDeTest),
            ("usinage2", MotDePasseDeTest));

        var account = authenticator.Authenticate(username, MotDePasseDeTest);

        Assert.NotNull(account);
        Assert.Equal(role, account!.Role);
    }

    [Theory]
    [InlineData("admin", "VALEUR-DE-TEST-NON-SECRETE")]  // sensible a la casse
    [InlineData("admin", "")]
    [InlineData("admin", "mauvais")]
    [InlineData("inconnu", "valeur-de-test-non-secrete")]
    [InlineData("", "valeur-de-test-non-secrete")]
    [InlineData(null, null)]
    public void Authenticate_IdentifiantsInvalides_RetourneNull(string? username, string? password)
    {
        var authenticator = BuildAuthenticator(("admin", MotDePasseDeTest));

        Assert.Null(authenticator.Authenticate(username, password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Authenticate_MotDePasseAbsentDeLaConfiguration_NeReplieSurAucuneValeur(string? configure)
    {
        var authenticator = BuildAuthenticator(("admin", configure));

        // Ni l'identifiant seul, ni une valeur vide, ni l'ancien defaut historique
        // ne doivent ouvrir la session : un compte non configure est indisponible.
        Assert.Null(authenticator.Authenticate("admin", "admin"));
        Assert.Null(authenticator.Authenticate("admin", string.Empty));
        Assert.Null(authenticator.Authenticate("admin", MotDePasseDeTest));
        Assert.False(authenticator.EstConfigure(Account));
    }

    [Fact]
    public void Accounts_NExposentAucunMotDePasse()
    {
        var proprietes = typeof(DevelopmentAccount)
            .GetProperties()
            .Select(propriete => propriete.Name)
            .ToArray();

        Assert.DoesNotContain(proprietes, nom => nom.Contains("Password", StringComparison.OrdinalIgnoreCase)
            && !nom.Contains("ConfigurationKey", StringComparison.OrdinalIgnoreCase));
        Assert.All(DevelopmentAccounts.All, compte =>
            Assert.Equal($"DevelopmentAccounts:{compte.Username}:Password", compte.PasswordConfigurationKey));
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
