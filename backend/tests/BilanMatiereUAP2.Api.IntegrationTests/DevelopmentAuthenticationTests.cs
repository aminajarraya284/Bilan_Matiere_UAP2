using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BilanMatiereUAP2.Api.Authentication;
using BilanMatiereUAP2.Api.Controllers;
using BilanMatiereUAP2.Contracts.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BilanMatiereUAP2.Api.IntegrationTests;

/// <summary>
/// Connexion locale Development (POST /api/auth/login) et prise en compte du token
/// par DevelopmentAuthenticationHandler. Ne couvre pas l'authentification de production
/// (Entra ID), qui reste un lot separe.
/// </summary>
public sealed class DevelopmentAuthenticationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private const string LoginUrl = "/api/auth/login";

    // Endpoint protege par ReadReceptionsUsinage2 = Admin | ResponsableUsinage2.
    private const string AdminOrUsinage2Url = "/api/v1/receptions-usinage2";

    private async Task<LoginResponse> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync(LoginUrl, new { username, password });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        return payload!;
    }

    // 1 + 2. Compte valide : 200, avec utilisateur et role exploitables par le frontend.
    [Fact]
    public async Task Login_AvecCompteDeveloppementValide_Retourne200EtUtilisateurAvecRole()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(LoginUrl, new { username = "admin", password = ApiWebApplicationFactory.DevelopmentPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("admin", body.User.Username);
        Assert.Equal("Admin", body.User.Role);
        Assert.Equal("Administrateur SOPAL", body.User.FullName);
        Assert.Equal("Actif", body.User.Status);
        Assert.Equal(1, body.User.Id);
        Assert.True(body.ExpiresAtUtc > DateTime.UtcNow);
    }

    // 3. Mot de passe incorrect -> 401.
    [Fact]
    public async Task Login_AvecMotDePasseIncorrect_Retourne401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(LoginUrl, new { username = "admin", password = "mauvais" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 4. Utilisateur inconnu -> 401 (meme reponse que 3 : pas d'enumeration de comptes).
    [Fact]
    public async Task Login_AvecUtilisateurInconnu_Retourne401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(LoginUrl, new { username = "inconnu", password = "admin" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 5. Mot de passe absent -> 400 (validation du contrat, pas 401).
    [Fact]
    public async Task Login_SansMotDePasse_Retourne400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(LoginUrl, new { username = "admin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 6 + 7. Le token emis est accepte sur un endpoint protege, avec le role Admin.
    [Fact]
    public async Task Token_DUnCompteAdmin_EstAccepteSurUnEndpointProtege()
    {
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var login = await LoginAsync(client, "admin", ApiWebApplicationFactory.DevelopmentPassword);

        var request = new HttpRequestMessage(HttpMethod.Get, AdminOrUsinage2Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // 8. Role insuffisant -> 403 (et non 401 : le token est valide, c'est le droit qui manque).
    [Fact]
    public async Task Token_DUnRoleInsuffisant_Retourne403()
    {
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var login = await LoginAsync(client, "fonderie", ApiWebApplicationFactory.DevelopmentPassword);
        Assert.Equal("ResponsableFonderie", login.User.Role);

        var request = new HttpRequestMessage(HttpMethod.Get, AdminOrUsinage2Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // 9. Hors Development (ou DevelopmentMode desactive), l'endpoint local n'est pas expose.
    //    Teste directement sur le controleur : hors Development, l'application ne demarre
    //    meme pas (voir ProductionAuthenticationGuardTests), ce garde-fou couvre le cas ou
    //    l'authentification de production sera branchee et l'application demarrable.
    [Theory]
    [InlineData("Production", true)]
    [InlineData("Staging", true)]
    [InlineData("Development", false)]
    public void Login_HorsDeveloppement_NEstPasExpose(string environmentName, bool developmentMode)
    {
        var controller = new AuthController(
            new StubTokenService(),
            new ConfigurationDevelopmentAccountAuthenticator(new ConfigurationBuilder().Build()),
            new StubHostEnvironment(environmentName),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:DevelopmentMode"] = developmentMode.ToString()
                })
                .Build(),
            NullLogger<AuthController>.Instance);

        var result = controller.Login(new DevelopmentLoginRequest { Username = "admin", Password = "admin" });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // 10. Aucun mot de passe ne doit apparaitre dans la reponse, sous quelque forme que ce soit.
    [Fact]
    public async Task Login_NeRenvoieJamaisLeMotDePasse()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(LoginUrl, new { username = "usinage2", password = ApiWebApplicationFactory.DevelopmentPassword });
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("motdepasse", raw, StringComparison.OrdinalIgnoreCase);

        // Le token ne doit pas non plus transporter le mot de passe en clair.
        var body = JsonDocument.Parse(raw).RootElement;
        var token = body.GetProperty("token").GetString()!;
        var payloadSegment = token[..token.IndexOf('.')];
        var decoded = DecodeBase64Url(payloadSegment);
        Assert.DoesNotContain("password", decoded, StringComparison.OrdinalIgnoreCase);
    }

    // Un token altere doit etre rejete (401) et non silencieusement replie sur le role Admin.
    [Fact]
    public async Task Token_Altere_Retourne401()
    {
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var login = await LoginAsync(client, "admin", ApiWebApplicationFactory.DevelopmentPassword);

        // On remplace la signature par une autre valeur base64url valide.
        var tampered = string.Concat(login.Token.AsSpan(0, login.Token.IndexOf('.') + 1), "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

        var request = new HttpRequestMessage(HttpMethod.Get, AdminOrUsinage2Url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tampered);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string DecodeBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding is 2 or 3)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }

        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }

    private sealed class StubHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class StubTokenService : IDevelopmentTokenService
    {
        public (string Token, DateTime ExpiresAtUtc) Issue(DevelopmentAccount account) =>
            ("stub", DateTime.UtcNow.AddHours(1));

        public DevelopmentTokenPayload? Validate(string? token) => null;
    }
}
