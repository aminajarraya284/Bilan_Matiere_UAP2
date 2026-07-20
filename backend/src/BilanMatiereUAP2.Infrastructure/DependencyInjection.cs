using BilanMatiereUAP2.Infrastructure.Persistence;
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

        return services;
    }
}
