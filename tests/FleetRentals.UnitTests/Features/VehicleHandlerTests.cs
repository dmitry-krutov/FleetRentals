using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Application.Features.Vehicles.Common;
using FleetRentals.Domain.Vehicles;
using FluentValidation;
using Xunit;

namespace FleetRentals.UnitTests.Features;

public sealed class VehicleHandlerTests
{
    [Fact]
    public async Task Registration_rejects_invalid_plate_without_writing()
    {
        var repository = new FakeVehicleRepository();
        var validator = new RegisterVehicleValidator();
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            validator.ValidateAndThrowAsync(new RegisterVehicleCommand("  ")));
        Assert.Contains(exception.Errors, error => error.ErrorCode == "vehicle.license_plate.required");
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task Registration_maps_duplicate_plate_to_conflict()
    {
        var repository = new FakeVehicleRepository { AddResult = false };
        var handler = new RegisterVehicleCommandHandler(repository);

        var result = await handler.Handle(new RegisterVehicleCommand(" ab-123 "), default);

        Assert.True(result.IsFailure);
        Assert.Equal("vehicle.license_plate.exists", result.Error.Code);
        Assert.Equal("AB-123", repository.LastAdded?.LicensePlate);
    }

    [Fact]
    public async Task Registration_and_lookup_return_vehicle_data()
    {
        var repository = new FakeVehicleRepository();
        var registered = await new RegisterVehicleCommandHandler(repository)
            .Handle(new RegisterVehicleCommand(" ab-123 "), default);

        Assert.True(registered.IsSuccess);
        var loaded = await new GetVehicleQueryHandler(repository)
            .Handle(new GetVehicleQuery(registered.Value.Id), default);

        Assert.True(loaded.IsSuccess);
        Assert.Equal("AB-123", loaded.Value.LicensePlate);
        Assert.Equal("Available", loaded.Value.Status);
    }

    [Fact]
    public async Task Lookup_rejects_empty_id_and_reports_missing_vehicle()
    {
        var repository = new FakeVehicleRepository();
        var handler = new GetVehicleQueryHandler(repository);

        var validator = new GetVehicleValidator();
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            validator.ValidateAndThrowAsync(new GetVehicleQuery(Guid.Empty)));
        var missing = await handler.Handle(new GetVehicleQuery(Guid.NewGuid()), default);

        Assert.Contains(exception.Errors, error => error.ErrorCode == "vehicle.id.invalid");
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

        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            GetCalls++;
            return Task.FromResult(LastAdded?.Id == id ? LastAdded : null);
        }
    }
}
