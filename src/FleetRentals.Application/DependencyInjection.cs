using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace FleetRentals.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(classes => classes.Where(type =>
                type.Namespace?.StartsWith("FleetRentals.Application.Features.", StringComparison.Ordinal) == true &&
                (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal) ||
                 type.Name.EndsWith("QueryHandler", StringComparison.Ordinal))))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsSelf()
            .WithScopedLifetime());

        return services;
    }
}
