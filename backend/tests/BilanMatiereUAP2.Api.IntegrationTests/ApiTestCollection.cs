namespace BilanMatiereUAP2.Api.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiTestCollection : ICollectionFixture<ApiWebApplicationFactory>
{
    public const string Name = "API integration tests";
}
