using System.Net;

namespace BilanMatiereUAP2.Api.IntegrationTests;

/// <summary>
/// Garde-fou de composition : chaque controleur doit pouvoir etre active par le conteneur.
///
/// Un service oublie dans DependencyInjection ne se voit ni a la compilation ni au demarrage :
/// il produit une InvalidOperationException 500 a la premiere requete, sur un ecran precis.
/// Ce test balaye les routes de lecture de chaque verticale pour detecter ce cas au plus tot.
/// </summary>
[Collection(ApiTestCollection.Name)]
public sealed class ControllersResolutionApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public static TheoryData<string> RoutesDeLecture() =>
    [
        "/api/parametres",
        "/api/v1/parametres",
        "/api/v1/postes-processus?page=1&pageSize=5",
        "/api/v1/postes-processus/non-mappes",
        "/api/v1/receptions-usinage2?page=1&pageSize=5",
        "/api/v1/receptions-usinage2/non-receptionnees?date=2026-06-19",
        "/api/v1/bilans/usinage2/2026-06",
        "/api/v1/rebuts-usinage2?periode=2026-06",
        "/api/v1/rapports-machine?page=1&pageSize=5",
        "/api/v1/mouvements-stock?page=1&pageSize=5",
        "/api/v1/inventaires-fonderie?periode=2026-06",
        "/api/v1/dechets-fonderie?periode=2026-06"
    ];

    [Theory]
    [MemberData(nameof(RoutesDeLecture))]
    public async Task ChaqueRouteDeLecture_EstActivableParLeConteneur(string route)
    {
        var response = await _client.GetAsync(route);

        // Le contenu metier n'est pas teste ici : seule compte l'absence d'erreur serveur.
        Assert.True(
            response.StatusCode != HttpStatusCode.InternalServerError,
            $"{route} renvoie 500 : {await response.Content.ReadAsStringAsync()}");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
