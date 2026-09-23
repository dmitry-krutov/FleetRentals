using FleetRentals.Application.Features.Rentals;
using FleetRentals.Domain.Drivers;
using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;
using Xunit;

namespace FleetRentals.UnitTests.Features;

public sealed class RentalHandlerTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Invalid_identifiers_are_refused_before_opening_a_transaction()
    {
        var factory = new FakeSessionFactory(new FakeSession());
        var handler = new StartRentalCommandHandler(factory, new FixedTimeProvider(StartedAt));

        var invalidVehicle = await handler.HandleAsync(new StartRentalCommand(Guid.Empty, Guid.NewGuid()), default);
        var invalidDriver = await handler.HandleAsync(new StartRentalCommand(Guid.NewGuid(), Guid.Empty), default);

        Assert.Equal("vehicle.id.invalid", invalidVehicle.Error.Code);
        Assert.Equal("driver.id.invalid", invalidDriver.Error.Code);
        Assert.Equal(0, factory.OpenCalls);
    }

    [Fact]
    public async Task Missing_vehicle_and_driver_return_distinct_errors_without_committing()
    {
        var vehicle = NewVehicle();
        var missingVehicle = new FakeSession { Driver = NewDriver() };
        var missingDriver = new FakeSession { Vehicle = vehicle };

        var vehicleResult = await new StartRentalCommandHandler(
            new FakeSessionFactory(missingVehicle), new FixedTimeProvider(StartedAt))
            .HandleAsync(new StartRentalCommand(vehicle.Id.Value, Guid.NewGuid()), default);
        var driverResult = await new StartRentalCommandHandler(
            new FakeSessionFactory(missingDriver), new FixedTimeProvider(StartedAt))
            .HandleAsync(new StartRentalCommand(vehicle.Id.Value, Guid.NewGuid()), default);

        Assert.Equal("vehicle.not_found", vehicleResult.Error.Code);
        Assert.Equal("driver.not_found", driverResult.Error.Code);
        Assert.False(missingVehicle.Committed);
        Assert.False(missingDriver.Committed);
    }

    [Fact]
    public async Task Busy_driver_is_refused_without_changing_vehicle()
    {
        var vehicle = NewVehicle();
        var driver = NewDriver();
        var session = new FakeSession { Vehicle = vehicle, Driver = driver, DriverBusy = true };

        var result = await new StartRentalCommandHandler(
            new FakeSessionFactory(session), new FixedTimeProvider(StartedAt))
            .HandleAsync(new StartRentalCommand(vehicle.Id.Value, driver.Id.Value), default);

        Assert.Equal("rental.driver.busy", result.Error.Code);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
        Assert.False(session.Committed);
        Assert.Null(session.InsertedRental);
    }

    [Fact]
    public async Task Successful_start_uses_supplied_clock_and_commits_rental_and_vehicle()
    {
        var vehicle = NewVehicle();
        var driver = NewDriver();
        var session = new FakeSession { Vehicle = vehicle, Driver = driver };

        var result = await new StartRentalCommandHandler(
            new FakeSessionFactory(session), new FixedTimeProvider(StartedAt))
            .HandleAsync(new StartRentalCommand(vehicle.Id.Value, driver.Id.Value), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(StartedAt, result.Value.StartedAtUtc);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(VehicleStatus.Rented, vehicle.Status);
        Assert.Equal(result.Value.Id, session.InsertedRental?.Id.Value);
        Assert.True(session.Committed);
    }

    [Theory]
    [InlineData(RentalInsertOutcome.VehicleAlreadyRented, "vehicle.already_rented")]
    [InlineData(RentalInsertOutcome.DriverAlreadyRented, "rental.driver.busy")]
    public async Task Database_uniqueness_conflicts_are_reported_without_committing(
        RentalInsertOutcome outcome, string errorCode)
    {
        var vehicle = NewVehicle();
        var driver = NewDriver();
        var session = new FakeSession { Vehicle = vehicle, Driver = driver, InsertOutcome = outcome };

        var result = await new StartRentalCommandHandler(
            new FakeSessionFactory(session), new FixedTimeProvider(StartedAt))
            .HandleAsync(new StartRentalCommand(vehicle.Id.Value, driver.Id.Value), default);

        Assert.Equal(errorCode, result.Error.Code);
        Assert.False(session.Committed);
    }

    private static Vehicle NewVehicle() =>
        Vehicle.Register(VehicleId.NewId(), LicensePlate.Create("AB-123").Value);

    private static Driver NewDriver() =>
        Driver.Register(DriverId.NewId(), DriverName.Create("Alex Driver").Value);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeSessionFactory(FakeSession session) : IRentalStartSessionFactory
    {
        public int OpenCalls { get; private set; }

        public Task<IRentalStartSession> OpenAsync(CancellationToken cancellationToken)
        {
            OpenCalls++;
            return Task.FromResult<IRentalStartSession>(session);
        }
    }

    private sealed class FakeSession : IRentalStartSession
    {
        public Vehicle? Vehicle { get; init; }

        public Driver? Driver { get; init; }

        public bool DriverBusy { get; init; }

        public RentalInsertOutcome InsertOutcome { get; init; } = RentalInsertOutcome.Inserted;

        public Rental? InsertedRental { get; private set; }

        public bool Committed { get; private set; }

        public Task<Vehicle?> GetVehicleForUpdateAsync(VehicleId id, CancellationToken cancellationToken) =>
            Task.FromResult(Vehicle?.Id == id ? Vehicle : null);

        public Task<Driver?> GetDriverForUpdateAsync(DriverId id, CancellationToken cancellationToken) =>
            Task.FromResult(Driver?.Id == id ? Driver : null);

        public Task<bool> HasActiveRentalForDriverAsync(DriverId id, CancellationToken cancellationToken) =>
            Task.FromResult(DriverBusy);

        public Task<RentalInsertOutcome> TryInsertAsync(Rental rental, CancellationToken cancellationToken)
        {
            InsertedRental = rental;
            return Task.FromResult(InsertOutcome);
        }

        public Task<bool> UpdateVehicleStatusAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
