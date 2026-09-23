using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Domain.Vehicles;
using Xunit;

namespace FleetRentals.UnitTests.Features;

public sealed class VehicleHandlerTests
{
    [Fact]
    public async Task Registration_rejects_invalid_plate_without_writing()
    {
        var repository = new FakeVehicleRepository();
        var handler = new RegisterVehicleCommandHandler(repository);

        var result = await handler.HandleAsync(new RegisterVehicleCommand("  "), default);

        Assert.True(result.IsFailure);
        Assert.Equal("vehicle.license_plate.required", result.Error.Code);
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task Registration_maps_duplicate_plate_to_conflict()
    {
        var repository = new FakeVehicleRepository { AddResult = false };
        var handler = new RegisterVehicleCommandHandler(repository);

        var result = await handler.HandleAsync(new RegisterVehicleCommand(" ab-123 "), default);

        Assert.True(result.IsFailure);
        Assert.Equal("vehicle.license_plate.exists", result.Error.Code);
        Assert.Equal("AB-123", repository.LastAdded?.LicensePlate.Value);
    }

    [Fact]
    public async Task Registration_and_lookup_return_vehicle_data()
    {
        var repository = new FakeVehicleRepository();
        var registered = await new RegisterVehicleCommandHandler(repository)
            .HandleAsync(new RegisterVehicleCommand(" ab-123 "), default);

        Assert.True(registered.IsSuccess);
        var loaded = await new GetVehicleQueryHandler(repository)
            .HandleAsync(new GetVehicleQuery(registered.Value.Id), default);

        Assert.True(loaded.IsSuccess);
        Assert.Equal("AB-123", loaded.Value.LicensePlate);
        Assert.Equal("Available", loaded.Value.Status);
    }

    [Fact]
    public async Task Lookup_rejects_empty_id_and_reports_missing_vehicle()
    {
        var repository = new FakeVehicleRepository();
        var handler = new GetVehicleQueryHandler(repository);

        var empty = await handler.HandleAsync(new GetVehicleQuery(Guid.Empty), default);
        var missing = await handler.HandleAsync(new GetVehicleQuery(Guid.NewGuid()), default);

        Assert.Equal("vehicle.id.invalid", empty.Error.Code);
        Assert.Equal("vehicle.not_found", missing.Error.Code);
        Assert.Equal(1, repository.GetCalls);
    }

    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        public bool AddResult { get; init; } = true;

        public int AddCalls { get; private set; }

        public int GetCalls { get; private set; }

        public Vehicle? LastAdded { get; private set; }

        public Task<bool> AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            AddCalls++;
            LastAdded = vehicle;
            return Task.FromResult(AddResult);
        }

        public Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            return Task.FromResult(LastAdded?.Id == id ? LastAdded : null);
        }
    }
}
