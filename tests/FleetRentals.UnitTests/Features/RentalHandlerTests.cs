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

    [Fact]
    public async Task Finish_rejects_empty_and_missing_rental_ids()
    {
        var session = new FakeSession();
        var factory = new FakeSessionFactory(session);
        var handler = new FinishRentalCommandHandler(factory, new FixedTimeProvider(StartedAt));

        var invalid = await handler.HandleAsync(new FinishRentalCommand(Guid.Empty), default);
        var missing = await handler.HandleAsync(new FinishRentalCommand(Guid.NewGuid()), default);

        Assert.Equal("rental.id.invalid", invalid.Error.Code);
        Assert.Equal("rental.not_found", missing.Error.Code);
        Assert.Equal(1, factory.OpenCalls);
        Assert.False(session.Committed);
    }

    [Fact]
    public async Task Finish_uses_supplied_clock_and_makes_vehicle_available()
    {
        var vehicle = NewVehicle();
        Assert.True(vehicle.MarkRented().IsSuccess);
        var driver = NewDriver();
        var rental = Rental.Start(RentalId.NewId(), vehicle.Id, driver.Id, StartedAt);
        var session = new FakeSession { Vehicle = vehicle, Driver = driver, ExistingRental = rental };
        var finishedAt = StartedAt.AddHours(1);

        var result = await new FinishRentalCommandHandler(
            new FakeSessionFactory(session), new FixedTimeProvider(finishedAt))
            .HandleAsync(new FinishRentalCommand(rental.Id.Value), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Finished", result.Value.Status);
        Assert.Equal(finishedAt, result.Value.FinishedAtUtc);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
        Assert.Equal(VehicleStatus.Rented, session.PreviousVehicleStatus);
        Assert.True(session.Committed);
    }

    [Fact]
    public async Task Repeated_finish_keeps_original_time_and_vehicle_state()
    {
        var vehicle = NewVehicle();
        var driver = NewDriver();
        var rental = Rental.Start(RentalId.NewId(), vehicle.Id, driver.Id, StartedAt);
        var originalFinish = StartedAt.AddMinutes(30);
        Assert.True(rental.Finish(originalFinish).IsSuccess);
        var session = new FakeSession { Vehicle = vehicle, Driver = driver, ExistingRental = rental };

        var result = await new FinishRentalCommandHandler(
            new FakeSessionFactory(session), new FixedTimeProvider(originalFinish.AddMinutes(30)))
            .HandleAsync(new FinishRentalCommand(rental.Id.Value), default);

        Assert.Equal("rental.already_finished", result.Error.Code);
        Assert.Equal(originalFinish, rental.FinishedAtUtc);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
        Assert.False(session.Committed);
    }

    [Fact]
    public async Task Finish_before_start_rolls_back_without_changing_vehicle()
    {
        var vehicle = NewVehicle();
        Assert.True(vehicle.MarkRented().IsSuccess);
        var driver = NewDriver();
        var rental = Rental.Start(RentalId.NewId(), vehicle.Id, driver.Id, StartedAt);
        var session = new FakeSession { Vehicle = vehicle, Driver = driver, ExistingRental = rental };

        var result = await new FinishRentalCommandHandler(
            new FakeSessionFactory(session), new FixedTimeProvider(StartedAt.AddSeconds(-1)))
            .HandleAsync(new FinishRentalCommand(rental.Id.Value), default);

        Assert.Equal("rental.finish_before_start", result.Error.Code);
        Assert.Null(rental.FinishedAtUtc);
        Assert.Equal(VehicleStatus.Rented, vehicle.Status);
        Assert.False(session.Committed);
    }

    [Fact]
    public async Task Finish_detects_rental_completed_while_waiting_for_locks()
    {
        var vehicle = NewVehicle();
        Assert.True(vehicle.MarkRented().IsSuccess);
        var driver = NewDriver();
        var active = Rental.Start(RentalId.NewId(), vehicle.Id, driver.Id, StartedAt);
        var finished = Rental.Restore(
            active.Id, vehicle.Id, driver.Id, StartedAt, StartedAt.AddMinutes(30)).Value;
        var session = new FakeSession
        {
            Vehicle = vehicle,
            Driver = driver,
            ExistingRental = active,
            LockedRental = finished,
        };

        var result = await new FinishRentalCommandHandler(
            new FakeSessionFactory(session), new FixedTimeProvider(StartedAt.AddHours(1)))
            .HandleAsync(new FinishRentalCommand(active.Id.Value), default);

        Assert.Equal("rental.already_finished", result.Error.Code);
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

    private sealed class FakeSessionFactory(FakeSession session) : IRentalSessionFactory
    {
        public int OpenCalls { get; private set; }

        public Task<IRentalSession> OpenAsync(CancellationToken cancellationToken)
        {
            OpenCalls++;
            return Task.FromResult<IRentalSession>(session);
        }
    }

    private sealed class FakeSession : IRentalSession
    {
        public Rental? ExistingRental { get; init; }

        public Rental? LockedRental { get; init; }

        public Vehicle? Vehicle { get; init; }

        public Driver? Driver { get; init; }

        public bool DriverBusy { get; init; }

        public RentalInsertOutcome InsertOutcome { get; init; } = RentalInsertOutcome.Inserted;

        public Rental? InsertedRental { get; private set; }

        public bool Committed { get; private set; }

        public VehicleStatus? PreviousVehicleStatus { get; private set; }

        public Task<Rental?> GetRentalAsync(RentalId id, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingRental?.Id == id ? ExistingRental : null);

        public Task<Rental?> GetRentalForUpdateAsync(RentalId id, CancellationToken cancellationToken) =>
            Task.FromResult((LockedRental ?? ExistingRental)?.Id == id ? LockedRental ?? ExistingRental : null);

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

        public Task<bool> UpdateRentalFinishAsync(Rental rental, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> UpdateVehicleStatusAsync(
            Vehicle vehicle, VehicleStatus expectedStatus, CancellationToken cancellationToken)
        {
            PreviousVehicleStatus = expectedStatus;
            return Task.FromResult(true);
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
