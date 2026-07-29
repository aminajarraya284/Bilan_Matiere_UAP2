using System.Text.Json;
using BilanMatiereUAP2.Api.Authentication;
using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Api.Health;
using BilanMatiereUAP2.Api.Middleware;
using BilanMatiereUAP2.Api.Services;
using BilanMatiereUAP2.Application;
using BilanMatiereUAP2.Application.Options;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
builder.Services.Configure<ManufacturingReferencesOptions>(builder.Configuration.GetSection(ManufacturingReferencesOptions.SectionName));
builder.Services.Configure<ReceptionUsinage2Options>(builder.Configuration.GetSection(ReceptionUsinage2Options.SectionName));
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();

// Garde de securite : DevelopmentAuthenticationHandler (header X-Development-Role) ne doit
// jamais etre enregistre ni actif en dehors de Development. Tant qu'aucune authentification
// de production (JWT/OIDC Entra ID) n'est branchee, le demarrage echoue explicitement dans
// tout autre environnement plutot que de tourner silencieusement sans authentification reelle.
// TODO_METIER: brancher ici l'authentification JWT/OIDC Entra ID SOPAL (Authority/Audience)
// une fois la configuration tenant fournie ; ce garde-fou devra alors accepter ce schema
// en plus du mode Development, sans jamais reintroduire DevelopmentAuthenticationHandler hors Dev.
var isDevelopmentAuthAllowed = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Authentication:DevelopmentMode");
var jwtAuthorityConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Jwt:Authority"]);

if (isDevelopmentAuthAllowed)
{
    // Emetteur/validateur des tokens de connexion locale (AuthController + handler ci-dessous).
    // Enregistre sous la meme condition que le schema Development : aucune connexion locale
    // n'est donc possible hors Development.
    builder.Services.TryAddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<IDevelopmentTokenService, DevelopmentTokenService>();

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
else if (!jwtAuthorityConfigured)
{
    throw new InvalidOperationException(
        $"Authentification de production non configuree pour l'environnement '{builder.Environment.EnvironmentName}'. " +
        "DevelopmentAuthenticationHandler (header X-Development-Role) est reserve a Development avec Authentication:DevelopmentMode=true. " +
        "Configurez Authentication:Jwt:Authority (JWT/OIDC Entra ID) avant de demarrer l'application hors Development. " +
        "Voir TODO_METIER Entra ID dans Program.cs.");
}
else
{
    // TODO_METIER: implementation JWT/OIDC Entra ID en attente de la configuration tenant SOPAL.
    throw new InvalidOperationException(
        "Authentication:Jwt:Authority est configure mais l'authentification JWT/OIDC n'est pas encore implementee " +
        "(TODO_METIER Entra ID). Ne pas demarrer en production sans authentification reelle.");
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

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
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
