using FleetRentals.Application.Features.Drivers;
using FleetRentals.Application.Features.Drivers.Common;
using FleetRentals.Application.Features.Rentals;
using FleetRentals.Application.Features.Rentals.Common;
using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Application.Features.Vehicles.Common;
using FleetRentals.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FleetRentals.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FleetRentals")
            ?? throw new InvalidOperationException("ConnectionStrings:FleetRentals is required.");

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<NpgsqlConnectionFactory>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IRentalRepository, RentalRepository>();

        return services;
    }
}
