using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Domain.Vehicles;
using FleetRentals.Persistence;
using Xunit;

namespace FleetRentals.IntegrationTests;

public sealed class VehicleRepositoryTests
{
    [Fact]
    public async Task Registered_vehicle_can_be_loaded_from_postgresql()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var repository = new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource));
        var register = new RegisterVehicleCommandHandler(repository);

        var created = await register.HandleAsync(new RegisterVehicleCommand(" ab-123 "), default);
        var loaded = await new GetVehicleQueryHandler(repository)
            .HandleAsync(new GetVehicleQuery(created.Value.Id), default);

        Assert.True(created.IsSuccess);
        Assert.True(loaded.IsSuccess);
        Assert.Equal(created.Value.Id, loaded.Value.Id);
        Assert.Equal("AB-123", loaded.Value.LicensePlate);
        Assert.Equal("Available", loaded.Value.Status);
    }

    [Fact]
    public async Task Database_refuses_same_plate_with_different_case()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var repository = new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource));
        var register = new RegisterVehicleCommandHandler(repository);

        var first = await register.HandleAsync(new RegisterVehicleCommand("ab-123"), default);
        var duplicate = await register.HandleAsync(new RegisterVehicleCommand("AB-123"), default);

        Assert.True(first.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal("vehicle.license_plate.exists", duplicate.Error.Code);
        Assert.NotNull(await repository.GetByIdAsync(VehicleId.Create(first.Value.Id).Value, default));
    }
}
