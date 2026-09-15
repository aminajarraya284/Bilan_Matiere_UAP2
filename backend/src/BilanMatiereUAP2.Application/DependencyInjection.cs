using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Application.Features.Dashboard;
using BilanMatiereUAP2.Application.Features.Alertes;
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
using BilanMatiereUAP2.Application.Features.Rapports;
using BilanMatiereUAP2.Application.Features.TypesMatiere;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Application.Features.Administration;
using BilanMatiereUAP2.Application.Features.Archivage;
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
        services.AddScoped<X3ImportService>();
        services.AddScoped<IX3ImportService, AuditedX3ImportService>();
        services.AddScoped<ManufacturingReferencesService>();
        services.AddScoped<IManufacturingReferencesService, AuditedManufacturingReferencesService>();
        services.AddScoped<ReportMachineService>();
        services.AddScoped<IReportMachineService, AuditedReportMachineService>();
        services.AddScoped<IOfContextResolver, OfContextResolver>();
        services.AddScoped<IReceptionUsinage2ContextService, ReceptionUsinage2ContextService>();
        services.AddScoped<IControlePoidsFonderieLookupService, ControlePoidsFonderieLookupService>();
        services.AddScoped<IImportArchiveService, ImportArchiveService>();
        services.AddScoped<IReceptionUsinage2Service, ReceptionUsinage2Service>();
        services.AddScoped<EntreeOfImportService>();
        services.AddScoped<IEntreeOfImportService, AuditedEntreeOfImportService>();
        services.AddScoped<IRebutUsinage2Service, RebutUsinage2Service>();
        services.AddScoped<IAlerteNonReceptionService, AlerteNonReceptionService>();
        services.AddScoped<IBilanUsinage2Service, BilanUsinage2Service>();
        services.AddScoped<IBilanFonderieService, BilanFonderieService>();
        services.AddScoped<IBilanFonderieIntervalleService, BilanFonderieService>();
        services.AddScoped<IBilanUap2Service, BilanUap2Service>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAlerteDetectionService, AlerteDetectionService>();
        services.AddScoped<IAlerteGenerationService, AlerteGenerationService>();
        services.AddScoped<IAlerteApplicationService, AlerteApplicationService>();
        services.AddScoped<IRapportsService, RapportsService>();
        services.AddScoped<IAuditDataSanitizer, AuditDataSanitizer>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IAdministrationService, AdministrationService>();
        services.AddScoped<IEntraUserProvisioningService, EntraUserProvisioningService>();
        services.AddScoped<IPeriodeBilanArchiveService, PeriodeBilanArchiveService>();
        services.AddScoped<IImportDependencyRefreshService, ImportDependencyRefreshService>();
        services.AddScoped<IParametreApplicationService, ParametreApplicationService>();
        services.AddScoped<IParametreApplicationProvider>(provider =>
            provider.GetRequiredService<IParametreApplicationService>());
        services.AddScoped<IPosteProcessusService, PosteProcessusService>();
        return services;
    }
}
