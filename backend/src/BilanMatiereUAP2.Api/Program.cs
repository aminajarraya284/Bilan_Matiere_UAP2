using System.Text.Json;
using BilanMatiereUAP2.Api.Authentication;
using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Api.Health;
using BilanMatiereUAP2.Api.Middleware;
using BilanMatiereUAP2.Api.Services;
using BilanMatiereUAP2.Application;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Serilog;

const string frontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

// Augmenter les limites de taille pour les uploads (défaut: 30MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
    options.MemoryBufferThreshold = 104857600; // 100 MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 104857600; // 100 MB
});

builder.Host.UseSerilog((_, loggerConfiguration) =>
    loggerConfiguration
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<ImportX3Options>(builder.Configuration.GetSection(ImportX3Options.SectionName));
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddScoped<HttpAuditContext>();
builder.Services.AddScoped<ICurrentUserContext>(provider => provider.GetRequiredService<HttpAuditContext>());
builder.Services.AddScoped<ICorrelationIdAccessor>(provider => provider.GetRequiredService<HttpAuditContext>());

// Garde de securite : DevelopmentAuthenticationHandler (header X-Development-Role) ne doit
// jamais etre enregistre ni actif en dehors de Development. Hors Development, seule
// l'authentification JWT/OIDC Entra ID est acceptee ; si elle n'est pas configuree, le
// demarrage echoue explicitement plutot que de tourner sans authentification reelle.
var isDevelopmentAuthAllowed = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Authentication:DevelopmentMode");
var jwtOptions = builder.Configuration
    .GetSection(EntraIdAuthenticationOptions.SectionName)
    .Get<EntraIdAuthenticationOptions>() ?? new EntraIdAuthenticationOptions();

if (isDevelopmentAuthAllowed)
{
    // Emetteur/validateur des tokens de connexion locale (AuthController + handler ci-dessous).
    // Enregistre sous la meme condition que le schema Development : aucune connexion locale
    // n'est donc possible hors Development.
    builder.Services.TryAddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<IDevelopmentTokenService, DevelopmentTokenService>();
    // Resout les mots de passe de developpement depuis la configuration (User Secrets).
    // Enregistre sous la meme condition : hors Development, ce service n'existe pas.
    builder.Services.AddSingleton<IDevelopmentAccountAuthenticator, ConfigurationDevelopmentAccountAuthenticator>();

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            DevelopmentAuthenticationHandler.SchemeName,
            _ => { });
}
else if (!jwtOptions.EstConfigure)
{
    throw new InvalidOperationException(
        $"Authentification de production non configuree pour l'environnement '{builder.Environment.EnvironmentName}'. " +
        "DevelopmentAuthenticationHandler (header X-Development-Role) est reserve a Development avec Authentication:DevelopmentMode=true. " +
        $"Renseignez {EntraIdAuthenticationOptions.SectionName}:Authority ET " +
        $"{EntraIdAuthenticationOptions.SectionName}:Audience (Entra ID) avant de demarrer hors Development. " +
        "Voir docs/AuthentificationEntraId.md.");
}
else
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Conserver les noms Entra (roles, oid, name) attendus par les policies.
            options.MapInboundClaims = false;
            options.Authority = jwtOptions.Authority;
            options.Audience = jwtOptions.Audience;
            // Un jeton non signe ou d'un autre emetteur ne doit jamais etre accepte.
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                // Entra ID publie les app roles dans la revendication « roles ».
                RoleClaimType = jwtOptions.RoleClaimType,
                NameClaimType = jwtOptions.NameClaimType,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
            if (jwtOptions.ValidIssuers.Length > 0)
            {
                options.TokenValidationParameters.ValidIssuers = jwtOptions.ValidIssuers;
            }
        });

    // Les app roles Entra ID ne portent pas forcement les memes libelles que les roles
    // applicatifs. La correspondance est configurable, sans quoi le nom du role Entra
    // est utilise tel quel.
    if (jwtOptions.CorrespondanceRoles.Count > 0)
    {
        builder.Services.AddSingleton<IClaimsTransformation>(
            new EntraIdRoleMappingTransformation(jwtOptions.CorrespondanceRoles, jwtOptions.RoleClaimType));
    }
}
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.ReadTypesMatiere, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(AuthorizationPolicies.ManageTypesMatiere, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.ManageDechetsFonderie, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie"));
    options.AddPolicy(AuthorizationPolicies.ReadControlesPoidsFonderie, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(AuthorizationPolicies.ManageControlesPoidsFonderie, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie"));
    options.AddPolicy(AuthorizationPolicies.ManageImportsX3, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(AuthorizationPolicies.ManageReferences, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.ReadReferences, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(AuthorizationPolicies.ReadReceptionsUsinage2, policy =>
        policy.RequireRole("Admin", "ResponsableUsinage2"));
    options.AddPolicy(AuthorizationPolicies.ManageReceptionsUsinage2, policy =>
        policy.RequireRole("Admin", "ResponsableUsinage2"));
    options.AddPolicy(AuthorizationPolicies.ValidateReceptionsUsinage2, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.ReadInventairesFonderie, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie"));
    options.AddPolicy(AuthorizationPolicies.ManageInventairesFonderie, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie"));
    options.AddPolicy(AuthorizationPolicies.ManageAdministration, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.ManagePeriodArchives, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(DashboardAuthorizationPolicies.Read, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(AlertesAuthorizationPolicies.Read, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(AlertesAuthorizationPolicies.Manage, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(RapportsAuthorizationPolicies.Read, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuditAuthorizationPolicies.Read, policy =>
        policy.RequireRole("Admin"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck<SqlServerHealthCheck>("sql-server", tags: ["ready"]);
builder.Services.AddProblemDetails();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
// Rapproche l'identite Entra avec le profil local (creation au premier login, refus si
// desactive ou sans role reconnu) AVANT que les policies metier ne s'appliquent.
app.UseMiddleware<EntraUserProvisioningMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        }));
    }
});

app.Run();

public partial class Program;
