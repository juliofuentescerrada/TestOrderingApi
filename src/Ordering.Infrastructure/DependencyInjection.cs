using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Abstractions;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra repositorios y servicios de lectura. El DbContext se registra en Program.cs
    /// para que la cadena de conexión pueda sustituirse en los tests.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderReadService, OrderReadService>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
