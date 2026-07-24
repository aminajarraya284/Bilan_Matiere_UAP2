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
using Serilog;

const string frontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, loggerConfiguration) =>
    loggerConfiguration
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<ImportX3Options>(builder.Configuration.GetSection(ImportX3Options.SectionName));
builder.Services.Configure<ManufacturingReferencesOptions>(builder.Configuration.GetSection(ManufacturingReferencesOptions.SectionName));
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
        DevelopmentAuthenticationHandler.SchemeName,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.ReadTypesMatiere, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(AuthorizationPolicies.ManageTypesMatiere, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.ManageDechetsFonderie, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie"));
    options.AddPolicy(AuthorizationPolicies.ManageImportsX3, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(AuthorizationPolicies.ManageReferences, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.ReadReferences, policy =>
        policy.RequireRole("Admin", "ResponsableFonderie", "ResponsableUsinage2"));
    options.AddPolicy(AuthorizationPolicies.ReadReceptionsUsinage2, policy =>
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
