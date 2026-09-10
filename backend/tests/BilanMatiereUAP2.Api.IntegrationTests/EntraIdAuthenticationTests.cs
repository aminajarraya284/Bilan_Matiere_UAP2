using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BilanMatiereUAP2.Api.IntegrationTests;

/// <summary>
/// Verrouille le comportement de demarrage hors Development (RM-SEC-003) : l'application
/// demarre avec une configuration Entra ID complete, et seulement dans ce cas.
/// </summary>
public sealed class EntraIdAuthenticationTests
{
    [Theory]
    [InlineData("Admin", true, "roles")]
    [InlineData("ResponsableFonderie", true, "roles")]
    [InlineData("SOPAL.Admin", true, "roles")]
    [InlineData("SOPAL.Admin", true, "app_roles")]
    [InlineData("RoleInconnu", false, "roles")]
    public async Task JetonValide_LesRolesBrutsEtMappesSontUtilisesParLesPolicies(string role, bool autorise, string roleClaimType)
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Authority"] = "https://login.microsoftonline.com/test/v2.0",
            ["Authentication:Jwt:Audience"] = "audit-local",
            ["Authentication:Jwt:RoleClaimType"] = roleClaimType,
            ["Authentication:Jwt:CorrespondanceRoles:SOPAL.Admin"] = "Admin"
        });
        var options = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var key = new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var objectId = Guid.NewGuid();
        var token = new JwtSecurityToken("audit-local", "audit-local",
            [new Claim(roleClaimType, role), new Claim("oid", objectId.ToString()),
             new Claim("name", "Utilisateur test"), new Claim("email", "test@example.invalid")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        // Meme configuration/handler que la production, avec une cle locale pour
        // verifier les roles sans appeler les serveurs Microsoft.
        var validation = options.TokenValidationParameters.Clone();
        validation.ValidIssuer = "audit-local";
        validation.ValidAudience = "audit-local";
        validation.IssuerSigningKey = key;
        var result = await options.TokenHandlers[0].ValidateTokenAsync(
            new JwtSecurityTokenHandler().WriteToken(token), validation);
        Assert.True(result.IsValid);
        var principal = new ClaimsPrincipal(result.ClaimsIdentity);
        var mapping = factory.Services.GetRequiredService<IClaimsTransformation>();
        await mapping.TransformAsync(principal);
        await mapping.TransformAsync(principal);

        Assert.Equal(objectId.ToString(), principal.FindFirstValue("oid"));
        Assert.Single(principal.FindAll(roleClaimType), claim => claim.Value == role);
        if (role == "SOPAL.Admin") Assert.Single(principal.FindAll(roleClaimType), claim => claim.Value == "Admin");
        var authorization = factory.Services.GetRequiredService<IAuthorizationService>();
        var access = await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.ReadTypesMatiere);
        Assert.Equal(autorise, access.Succeeded);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } };
        var currentUser = new HttpCurrentUserService(accessor);
        Assert.Equal(objectId, currentUser.UserId);
        Assert.Equal("Utilisateur test", currentUser.UserName);
        if (autorise) Assert.Equal(role == "SOPAL.Admin" ? "Admin" : role, currentUser.Role);
        var audit = new HttpAuditContext(accessor);
        Assert.Equal("test@example.invalid", audit.Email);
        if (autorise) Assert.Contains(currentUser.Role!, audit.Roles);
    }

    [Fact]
    public void Demarrage_HorsDevelopment_AvecAuthorityEtAudience_Reussit()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "false",
            ["Authentication:Jwt:Authority"] = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
            ["Authentication:Jwt:Audience"] = "api://bilan-matiere-uap2"
        });

        var exception = Record.Exception(() => factory.Services.GetService(typeof(object)));

        Assert.Null(exception);
    }

    // Une autorite sans audience laisserait passer des jetons emis pour une autre
    // application du meme tenant : la configuration doit etre complete ou absente.
    [Fact]
    public void Demarrage_HorsDevelopment_AvecAuthoritySansAudience_EchoueExplicitement()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "false",
            ["Authentication:Jwt:Authority"] = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0"
        });

        var exception = Record.Exception(() => factory.Services.GetService(typeof(object)));

        Assert.NotNull(exception);
        Assert.Contains("Authentification de production non configuree", exception.ToString());
    }

    // Le mode Development ne doit jamais pouvoir etre reactive hors Development.
    [Fact]
    public void Demarrage_HorsDevelopment_AvecDevelopmentModeForce_EchoueQuandMeme()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "true"
        });

        var exception = Record.Exception(() => factory.Services.GetService(typeof(object)));

        Assert.NotNull(exception);
        Assert.Contains("Authentification de production non configuree", exception.ToString());
    }

    [Fact]
    public async Task RequeteSansJeton_HorsDevelopment_EstRefusee()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "false",
            ["Authentication:Jwt:Authority"] = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
            ["Authentication:Jwt:Audience"] = "api://bilan-matiere-uap2"
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/types-matiere");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // L'en-tete de developpement ne doit plus rien signifier hors Development.
    [Fact]
    public async Task EnTeteDeveloppement_HorsDevelopment_NeDonneAucunAcces()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "false",
            ["Authentication:Jwt:Authority"] = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
            ["Authentication:Jwt:Audience"] = "api://bilan-matiere-uap2"
        });
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/types-matiere");
        request.Headers.Add("X-Development-Role", "Admin");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class ConfiguredProductionFactory(Dictionary<string, string?> configuration)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            // UseSetting alimente la configuration de l'hote AVANT l'execution de Program.cs,
            // contrairement a ConfigureAppConfiguration qui interviendrait trop tard pour
            // les lectures faites au demarrage.
            foreach (var (cle, valeur) in configuration)
            {
                builder.UseSetting(cle, valeur);
            }
        }
    }
}
