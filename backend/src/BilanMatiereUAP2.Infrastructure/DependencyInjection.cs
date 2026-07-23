using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Infrastructure.Persistence;
using BilanMatiereUAP2.Infrastructure.Persistence.Repositories;
using BilanMatiereUAP2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BilanMatiereUAP2.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "La chaine de connexion 'DefaultConnection' est absente.");

        services.AddDbContext<BilanMatiereUAP2DbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<ITypeMatiereRepository, TypeMatiereRepository>();
        services.AddScoped<IDechetFonderieRepository, DechetFonderieRepository>();
        services.AddScoped<IX3ImportRepository, InMemoryX3ImportRepository>();
        services.AddScoped<IEncoursProductReader, EncoursProductExcelReader>();
        services.AddScoped<IMouvementStockX3Reader, MouvementsStockX3ExcelReader>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<BilanMatiereUAP2DbContext>());
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
