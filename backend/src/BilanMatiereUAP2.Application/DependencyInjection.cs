using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Application.Features.RebutsUsinage2;
using BilanMatiereUAP2.Application.Features.DechetsFonderie;
using BilanMatiereUAP2.Application.Features.ControlePoidsFonderie;
using BilanMatiereUAP2.Application.Features.EntreeOf;
using BilanMatiereUAP2.Application.Features.Imports;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Application.Features.InventairesFonderie;
using BilanMatiereUAP2.Application.Features.Parametres;
using BilanMatiereUAP2.Application.Features.PostesProcessus;
using BilanMatiereUAP2.Application.Features.ReceptionsUsinage2;
using BilanMatiereUAP2.Application.Features.TypesMatiere;
using BilanMatiereUAP2.Application.Services;
using BilanMatiereUAP2.Application.Abstractions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BilanMatiereUAP2.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ITypeMatiereService, TypeMatiereService>();
        services.AddScoped<IDechetFonderieService, DechetFonderieService>();
        services.AddScoped<IControlePoidsFonderieService, ControlePoidsFonderieService>();
        services.AddScoped<IInventaireFonderieService, InventaireFonderieService>();
        services.AddScoped<IInventaireComptageService, InventaireComptageService>();
        services.AddScoped<IX3ImportService, X3ImportService>();
        services.AddScoped<IManufacturingReferencesService, ManufacturingReferencesService>();
        services.AddScoped<IReportMachineService, ReportMachineService>();
        services.AddScoped<IOfContextResolver, OfContextResolver>();
        services.AddScoped<IReceptionUsinage2ContextService, ReceptionUsinage2ContextService>();
        services.AddScoped<IControlePoidsFonderieLookupService, ControlePoidsFonderieLookupService>();
        services.AddScoped<IImportArchiveService, ImportArchiveService>();
        services.AddScoped<IReceptionUsinage2Service, ReceptionUsinage2Service>();
        services.AddScoped<IEntreeOfImportService, EntreeOfImportService>();
        services.AddScoped<IRebutUsinage2Service, RebutUsinage2Service>();
        services.AddScoped<IAlerteNonReceptionService, AlerteNonReceptionService>();
        services.AddScoped<IBilanUsinage2Service, BilanUsinage2Service>();
        services.AddScoped<IBilanFonderieService, BilanFonderieService>();
        services.AddScoped<IBilanUap2Service, BilanUap2Service>();
        services.AddScoped<IImportDependencyRefreshService, ImportDependencyRefreshService>();
        services.AddScoped<IParametreApplicationService, ParametreApplicationService>();
        services.AddScoped<IParametreApplicationProvider>(provider =>
            provider.GetRequiredService<IParametreApplicationService>());
        services.AddScoped<IPosteProcessusService, PosteProcessusService>();
        return services;
    }
}
