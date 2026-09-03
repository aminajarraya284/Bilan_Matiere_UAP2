using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BilanMatiereUAP2.Contracts.Administration;

namespace BilanMatiereUAP2.Api.IntegrationTests;

/// <summary>
/// Endpoints d'administration des accès : périmètre Admin, absence de création manuelle
/// et de gestion de rôle, et non-exposition de tout secret.
/// </summary>
[Collection(ApiTestCollection.Name)]
public sealed class AdministrationApiTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private const string BaseUrl = "/api/v1/admin/utilisateurs";

    private HttpClient Client(string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Role", role);
        return client;
    }

    // --- Périmètre (§31, §46) ------------------------------------------------

    [Fact]
    public async Task GetUtilisateurs_Admin_Retourne200EtUneReponsePaginee()
    {
        await factory.ResetDatabaseAsync();

        var response = await Client("Admin").GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedUtilisateursResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.Page);
        Assert.Equal(25, body.PageSize);
    }

    [Theory]
    [InlineData("ResponsableFonderie")]
    [InlineData("ResponsableUsinage2")]
    public async Task GetUtilisateurs_RoleNonAdmin_RetourneForbidden(string role)
    {
        var response = await Client(role).GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("ResponsableFonderie")]
    [InlineData("ResponsableUsinage2")]
    public async Task ChangerActivation_RoleNonAdmin_RetourneForbidden(string role)
    {
        var response = await Client(role)
            .PutAsJsonAsync($"{BaseUrl}/1/activation", new ToggleUtilisateurApplicationRequest(false));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("ResponsableFonderie")]
    [InlineData("ResponsableUsinage2")]
    public async Task ComptesDeveloppement_RoleNonAdmin_RetourneForbidden(string role)
    {
        var response = await Client(role).GetAsync($"{BaseUrl}/developpement");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Pagination et validation (§30) --------------------------------------

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    public async Task GetUtilisateurs_PaginationInvalide_RetourneBadRequest(string query)
    {
        var response = await Client("Admin").GetAsync(BaseUrl + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUtilisateur_Inexistant_RetourneNotFound()
    {
        await factory.ResetDatabaseAsync();

        var response = await Client("Admin").GetAsync($"{BaseUrl}/424242");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Comptes de développement (§13, §14, §16) ----------------------------

    [Fact]
    public async Task ComptesDeveloppement_Admin_RetourneLesTroisComptesSansMotDePasse()
    {
        var response = await Client("Admin").GetAsync($"{BaseUrl}/developpement");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var comptes = await response.Content.ReadFromJsonAsync<IReadOnlyList<CompteDeveloppementResponse>>();
        Assert.NotNull(comptes);
        Assert.Equal(3, comptes!.Count);
        Assert.Equal(
            ["Admin", "ResponsableFonderie", "ResponsableUsinage2"],
            comptes.Select(compte => compte.Role).Order().ToArray());

        // Le contrat ne porte aucun champ de mot de passe, et la charge JSON non plus.
        var brut = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", brut, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("motdepasse", brut, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ApiWebApplicationFactory.DevelopmentPassword, brut, StringComparison.Ordinal);
    }

    // --- Contrat : ni création manuelle, ni changement de rôle (§26, §28) ----

    [Fact]
    public async Task PostUtilisateur_NExistePas()
    {
        var response = await Client("Admin")
            .PostAsJsonAsync(BaseUrl, new { entraObjectId = "OID-1", role = "Admin" });

        // Le provisionnement est automatique : aucune route de création manuelle.
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task PutUtilisateur_SansSegmentActivation_NExistePas()
    {
        var response = await Client("Admin")
            .PutAsJsonAsync($"{BaseUrl}/1", new { role = "Admin" });

        // Le rôle est géré dans Entra ID : la route existe en lecture seule, PUT est refusé.
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task ReponsePaginee_NeContientAucunChampSensible()
    {
        await factory.ResetDatabaseAsync();

        var brut = await Client("Admin").GetStringAsync(BaseUrl);

        using var document = JsonDocument.Parse(brut);
        Assert.True(document.RootElement.TryGetProperty("total", out _));
        foreach (var interdit in new[] { "password", "motDePasse", "passwordHash", "accessToken", "refreshToken", "secret" })
        {
            Assert.DoesNotContain(interdit, brut, StringComparison.OrdinalIgnoreCase);
        }
    }
}
