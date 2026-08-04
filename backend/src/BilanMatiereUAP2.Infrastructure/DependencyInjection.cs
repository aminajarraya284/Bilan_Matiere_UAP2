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
            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.CommandTimeout(180)));
        services.AddScoped<ITypeMatiereRepository, TypeMatiereRepository>();
        services.AddScoped<IDechetFonderieRepository, DechetFonderieRepository>();
        services.AddScoped<IControlePoidsFonderieRepository, ControlePoidsFonderieRepository>();
        services.AddScoped<IInventaireFonderieRepository, InventaireFonderieRepository>();
        services.AddScoped<IInventaireComptageRepository, InventaireComptageRepository>();
        services.AddScoped<IRebutUsinage2Repository, RebutUsinage2Repository>();
        services.AddScoped<IX3ImportRepository, X3ImportRepository>();
        services.AddScoped<IOfContextDataRepository, OfContextDataRepository>();
        services.AddScoped<IParametreApplicationRepository, ParametreApplicationRepository>();
        services.AddScoped<IPosteProcessusRepository, PosteProcessusRepository>();
        services.AddScoped<IEncoursProductReader, EncoursProductExcelReader>();
        services.AddScoped<IMouvementStockX3Reader, MouvementsStockX3ExcelReader>();
        services.AddScoped<ManufacturingReferencesRepository>();
        services.AddScoped<IManufacturingReferencesRepository>(provider =>
            provider.GetRequiredService<ManufacturingReferencesRepository>());
        services.AddScoped<IOfContextReferencesRepository>(provider =>
            provider.GetRequiredService<ManufacturingReferencesRepository>());
        services.AddScoped<IGammeFabricationReader, GammeFabricationExcelReader>();
        services.AddScoped<INomenclatureReader, NomenclatureExcelReader>();
        services.AddScoped<ISortieMpMaterialRepository, SortieMpMaterialRepository>();
        services.AddScoped<IBilanFonderieRepository, BilanFonderieRepository>();
        services.AddScoped<IBilanUap2Repository, BilanUap2Repository>();
        services.AddScoped<IReportMachineRepository, ReportMachineRepository>();
        services.AddScoped<IReportMachineReader, RapportMachineExcelReader>();
        services.AddScoped<IImportSourceFileRepository, ImportSourceFileRepository>();
        services.AddScoped<IReceptionUsinage2Repository, ReceptionUsinage2Repository>();
        services.AddScoped<IEntreeOfRepository, EntreeOfRepository>();
        services.AddSingleton<IImportFileStorage, LocalImportFileStorage>();
        services.AddSingleton<IImportHashCalculator, ImportHashCalculator>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<BilanMatiereUAP2DbContext>());
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
