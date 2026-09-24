using FleetRentals.Application.Features.Rentals;
using FleetRentals.Application.Features.Rentals.Common;
using FleetRentals.Domain.Rentals;
using FluentValidation;
using Xunit;

namespace FleetRentals.UnitTests.Features;

public sealed class RentalHandlerTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Invalid_start_identifiers_are_refused_before_writing()
    {
        var repository = new FakeRentalRepository();
        var validator = new StartRentalValidator();

        var invalidVehicle = await Assert.ThrowsAsync<ValidationException>(() =>
            validator.ValidateAndThrowAsync(new StartRentalCommand(Guid.Empty, Guid.NewGuid())));
        var invalidDriver = await Assert.ThrowsAsync<ValidationException>(() =>
            validator.ValidateAndThrowAsync(new StartRentalCommand(Guid.NewGuid(), Guid.Empty)));

        Assert.Contains(invalidVehicle.Errors, error => error.ErrorCode == "vehicle.id.invalid");
        Assert.Contains(invalidDriver.Errors, error => error.ErrorCode == "driver.id.invalid");
        Assert.Null(repository.InsertedRental);
    }

    [Theory]
    [InlineData(RentalInsertOutcome.VehicleNotFound, "vehicle.not_found")]
    [InlineData(RentalInsertOutcome.DriverNotFound, "driver.not_found")]
    [InlineData(RentalInsertOutcome.VehicleAlreadyRented, "vehicle.already_rented")]
    [InlineData(RentalInsertOutcome.DriverAlreadyRented, "rental.driver.busy")]
    public async Task Start_maps_database_outcomes_to_application_errors(
        RentalInsertOutcome outcome, string errorCode)
    {
        var repository = new FakeRentalRepository { InsertOutcome = outcome };
        var result = await new StartRentalCommandHandler(repository, new FixedTimeProvider(StartedAt))
            .Handle(new StartRentalCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        Assert.Equal(errorCode, result.Error.Code);
    }

    [Fact]
    public async Task Successful_start_uses_supplied_clock_and_writes_one_rental()
    {
        var repository = new FakeRentalRepository();
        var vehicleId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var result = await new StartRentalCommandHandler(repository, new FixedTimeProvider(StartedAt))
            .Handle(new StartRentalCommand(vehicleId, driverId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(StartedAt, result.Value.StartedAtUtc);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(vehicleId, repository.InsertedRental?.VehicleId);
        Assert.Equal(driverId, repository.InsertedRental?.DriverId);
    }

    [Fact]
    public async Task Finish_rejects_empty_and_missing_rental_ids()
    {
        var repository = new FakeRentalRepository();
        var handler = new FinishRentalCommandHandler(repository, new FixedTimeProvider(StartedAt));
        var validator = new FinishRentalValidator();

        var invalid = await Assert.ThrowsAsync<ValidationException>(() =>
            validator.ValidateAndThrowAsync(new FinishRentalCommand(Guid.Empty)));
        var missing = await handler.Handle(new FinishRentalCommand(Guid.NewGuid()), default);

        Assert.Contains(invalid.Errors, error => error.ErrorCode == "rental.id.invalid");
        Assert.Equal("rental.not_found", missing.Error.Code);
        Assert.Null(repository.FinishedId);
    }

    [Fact]
    public async Task Successful_finish_uses_supplied_clock()
    {
        var rental = NewRental();
        var repository = new FakeRentalRepository { Rental = rental };
        var finishedAt = StartedAt.AddHours(1);

        var result = await new FinishRentalCommandHandler(
                repository, new FixedTimeProvider(finishedAt))
            .Handle(new FinishRentalCommand(rental.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Finished", result.Value.Status);
        Assert.Equal(finishedAt, result.Value.FinishedAtUtc);
        Assert.Equal(rental.Id, repository.FinishedId);
        Assert.Equal(finishedAt, repository.FinishedAt);
    }

    [Fact]
    public async Task Repeated_finish_keeps_original_time()
    {
        var rental = NewRental();
        var originalFinish = StartedAt.AddMinutes(30);
        rental.FinishedAtUtc = originalFinish;
        var repository = new FakeRentalRepository { Rental = rental };

        var result = await new FinishRentalCommandHandler(
                repository,
                new FixedTimeProvider(originalFinish.AddMinutes(30)))
            .Handle(new FinishRentalCommand(rental.Id), default);

        Assert.Equal("rental.already_finished", result.Error.Code);
        Assert.Equal(originalFinish, rental.FinishedAtUtc);
        Assert.Null(repository.FinishedId);
    }

    [Fact]
    public async Task Finish_before_start_does_not_write()
    {
        var rental = NewRental();
        var repository = new FakeRentalRepository { Rental = rental };

        var result = await new FinishRentalCommandHandler(
                repository,
                new FixedTimeProvider(StartedAt.AddSeconds(-1)))
            .Handle(new FinishRentalCommand(rental.Id), default);

        Assert.Equal("rental.finish_before_start", result.Error.Code);
        Assert.Null(rental.FinishedAtUtc);
        Assert.Null(repository.FinishedId);
    }

    [Fact]
    public async Task Finish_detects_rental_completed_after_read()
    {
        var rental = NewRental();
        var repository = new FakeRentalRepository { Rental = rental, FinishResult = false };

        var result = await new FinishRentalCommandHandler(
                repository,
                new FixedTimeProvider(StartedAt.AddHours(1)))
            .Handle(new FinishRentalCommand(rental.Id), default);

        Assert.Equal("rental.already_finished", result.Error.Code);
        Assert.Null(rental.FinishedAtUtc);
    }

    private static Rental NewRental() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), StartedAt);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeRentalRepository : IRentalRepository
    {
        public Rental? Rental { get; init; }

        public RentalInsertOutcome InsertOutcome { get; init; } = RentalInsertOutcome.Inserted;

        public bool FinishResult { get; init; } = true;

        public Rental? InsertedRental { get; private set; }

        public Guid? FinishedId { get; private set; }

        public DateTimeOffset? FinishedAt { get; private set; }

        public Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Rental?.Id == id ? Rental : null);

        public Task<Rental?> GetActiveByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken) =>
            Task.FromResult<Rental?>(null);

        public Task<RentalInsertOutcome> TryAddAsync(Rental rental, CancellationToken cancellationToken)
        {
            InsertedRental = rental;
            return Task.FromResult(InsertOutcome);
        }

        public Task<bool> TryFinishAsync(Guid id, DateTimeOffset finishedAtUtc, CancellationToken cancellationToken)
        {
            FinishedId = id;
            FinishedAt = finishedAtUtc;
            return Task.FromResult(FinishResult);
        }
    }
}
