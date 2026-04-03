using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessCloud.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 1. Registro automático de todos los validadores de FluentValidation
        // Esta línea busca todas las clases que heredan de AbstractValidator en este proyecto
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());


        return services;
    }
}