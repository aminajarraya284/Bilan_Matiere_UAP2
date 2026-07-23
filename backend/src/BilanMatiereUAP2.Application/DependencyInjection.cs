using BilanMatiereUAP2.Application.Features.DechetsFonderie;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Application.Features.TypesMatiere;
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
        services.AddScoped<IX3ImportService, X3ImportService>();
        return services;
    }
}
