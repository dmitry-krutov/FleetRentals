using FleetRentals.Application.Features.Drivers;
using FleetRentals.Application.Features.Rentals;
using FleetRentals.Application.Features.Rentals.Common;
using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Application.Features.Vehicles.Common;
using FleetRentals.Domain.Vehicles;
using FleetRentals.Persistence;
using FleetRentals.Persistence.Repositories;
using Xunit;

namespace FleetRentals.IntegrationTests;

public sealed class RentalLifecycleTests
{
    [Fact]
    public async Task Start_reports_missing_vehicle_and_driver_from_foreign_keys()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var vehicleId = await RegisterVehicleAsync(database, "AB-123");
        var driverId = await RegisterDriverAsync(database, "Alex Driver");
        var handler = CreateStartHandler(database);

        var missingVehicle = await handler.Handle(
            new StartRentalCommand(Guid.NewGuid(), driverId), default);
        var missingDriver = await handler.Handle(
            new StartRentalCommand(vehicleId, Guid.NewGuid()), default);

        Assert.Equal("vehicle.not_found", missingVehicle.Error.Code);
        Assert.Equal("driver.not_found", missingDriver.Error.Code);
        Assert.Null(await new RentalRepository(new NpgsqlConnectionFactory(database.DataSource))
            .GetActiveByVehicleIdAsync(vehicleId, default));
        Assert.Equal("Available", (await GetVehicleAsync(database, vehicleId)).Status);
    }

    [Fact]
    public async Task Start_creates_active_rental_and_marks_vehicle_rented()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var vehicleId = await RegisterVehicleAsync(database, "AB-123");
        var driverId = await RegisterDriverAsync(database, "Alex Driver");
        var handler = CreateStartHandler(database);

        var result = await handler.Handle(new StartRentalCommand(vehicleId, driverId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(vehicleId, result.Value.VehicleId);
        Assert.Equal(driverId, result.Value.DriverId);
        Assert.Equal(TimeSpan.Zero, result.Value.StartedAtUtc.Offset);

        var rentalRepository = new RentalRepository(new NpgsqlConnectionFactory(database.DataSource));
        var loaded = await new GetRentalQueryHandler(rentalRepository)
            .Handle(new GetRentalQuery(result.Value.Id), default);
        var current = await CreateActiveQueryHandler(database, rentalRepository)
            .Handle(new GetActiveRentalByVehicleQuery(vehicleId), default);

        Assert.True(loaded.IsSuccess);
        Assert.Equal(result.Value.Id, loaded.Value.Id);
        Assert.True(current.IsSuccess);
        Assert.Equal(driverId, current.Value.DriverId);
        Assert.Equal("Rented", (await GetVehicleAsync(database, vehicleId)).Status);
    }

    [Fact]
    public async Task Second_rental_for_same_vehicle_is_refused_and_first_is_unchanged()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var vehicleId = await RegisterVehicleAsync(database, "AB-123");
        var firstDriverId = await RegisterDriverAsync(database, "Alex Driver");
        var secondDriverId = await RegisterDriverAsync(database, "Sam Driver");
        var handler = CreateStartHandler(database);

        var first = await handler.Handle(new StartRentalCommand(vehicleId, firstDriverId), default);
        var second = await handler.Handle(new StartRentalCommand(vehicleId, secondDriverId), default);

        Assert.True(first.IsSuccess);
        Assert.Equal("vehicle.already_rented", second.Error.Code);
        var active = await new RentalRepository(new NpgsqlConnectionFactory(database.DataSource))
            .GetActiveByVehicleIdAsync(vehicleId, default);
        Assert.Equal(first.Value.Id, active?.Id);
        Assert.Equal("Rented", (await GetVehicleAsync(database, vehicleId)).Status);
    }

    [Fact]
    public async Task Second_vehicle_for_busy_driver_is_refused_and_remains_available()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var firstVehicleId = await RegisterVehicleAsync(database, "AB-123");
        var secondVehicleId = await RegisterVehicleAsync(database, "CD-456");
        var driverId = await RegisterDriverAsync(database, "Alex Driver");
        var handler = CreateStartHandler(database);

        var first = await handler.Handle(new StartRentalCommand(firstVehicleId, driverId), default);
        var second = await handler.Handle(new StartRentalCommand(secondVehicleId, driverId), default);

        Assert.True(first.IsSuccess);
        Assert.Equal("rental.driver.busy", second.Error.Code);
        Assert.Equal("Rented", (await GetVehicleAsync(database, firstVehicleId)).Status);
        Assert.Equal("Available", (await GetVehicleAsync(database, secondVehicleId)).Status);
        var active = await new RentalRepository(new NpgsqlConnectionFactory(database.DataSource))
            .GetActiveByVehicleIdAsync(firstVehicleId, default);
        Assert.Equal(first.Value.Id, active?.Id);
    }

    [Fact]
    public async Task Parallel_starts_for_one_vehicle_allow_only_one_active_rental()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var vehicleId = await RegisterVehicleAsync(database, "AB-123");
        var firstDriverId = await RegisterDriverAsync(database, "Alex Driver");
        var secondDriverId = await RegisterDriverAsync(database, "Sam Driver");
        var handler = CreateStartHandler(database);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask = Task.Run(async () =>
        {
            await gate.Task;
            return await handler.Handle(new StartRentalCommand(vehicleId, firstDriverId), default);
        });
        var secondTask = Task.Run(async () =>
        {
            await gate.Task;
            return await handler.Handle(new StartRentalCommand(vehicleId, secondDriverId), default);
        });
        gate.SetResult();
        var results = await Task.WhenAll(firstTask, secondTask);

        var winner = Assert.Single(results, result => result.IsSuccess);
        var loser = Assert.Single(results, result => result.IsFailure);
        Assert.Equal("vehicle.already_rented", loser.Error.Code);
        var active = await new RentalRepository(new NpgsqlConnectionFactory(database.DataSource))
            .GetActiveByVehicleIdAsync(vehicleId, default);
        Assert.Equal(winner.Value.Id, active?.Id);
    }

    [Fact]
    public async Task Parallel_starts_for_one_driver_leave_other_vehicle_available()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var firstVehicleId = await RegisterVehicleAsync(database, "AB-123");
        var secondVehicleId = await RegisterVehicleAsync(database, "CD-456");
        var driverId = await RegisterDriverAsync(database, "Alex Driver");
        var handler = CreateStartHandler(database);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask = Task.Run(async () =>
        {
            await gate.Task;
            return await handler.Handle(new StartRentalCommand(firstVehicleId, driverId), default);
        });
        var secondTask = Task.Run(async () =>
        {
            await gate.Task;
            return await handler.Handle(new StartRentalCommand(secondVehicleId, driverId), default);
        });
        gate.SetResult();
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Single(results, result => result.IsSuccess);
        var loser = Assert.Single(results, result => result.IsFailure);
        Assert.Equal("rental.driver.busy", loser.Error.Code);
        var statuses = new[]
        {
            (await GetVehicleAsync(database, firstVehicleId)).Status,
            (await GetVehicleAsync(database, secondVehicleId)).Status,
        };
        Assert.Single(statuses, status => status == "Rented");
        Assert.Single(statuses, status => status == "Available");
    }

    [Fact]
    public async Task Finish_releases_vehicle_and_driver_for_a_new_rental()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var vehicleId = await RegisterVehicleAsync(database, "AB-123");
        var driverId = await RegisterDriverAsync(database, "Alex Driver");
        var startHandler = CreateStartHandler(database);
        var finishHandler = CreateFinishHandler(database);

        var first = await startHandler.Handle(new StartRentalCommand(vehicleId, driverId), default);
        Assert.True(first.IsSuccess);

        var finished = await finishHandler.Handle(new FinishRentalCommand(first.Value.Id), default);
        Assert.True(finished.IsSuccess);
        Assert.Equal("Finished", finished.Value.Status);
        Assert.NotNull(finished.Value.FinishedAtUtc);
        Assert.True(finished.Value.FinishedAtUtc >= finished.Value.StartedAtUtc);
        Assert.Equal("Available", (await GetVehicleAsync(database, vehicleId)).Status);

        var rentalRepository = new RentalRepository(new NpgsqlConnectionFactory(database.DataSource));
        var activeAfterFinish = await rentalRepository.GetActiveByVehicleIdAsync(
            vehicleId, default);
        Assert.Null(activeAfterFinish);
        var storedFirst = await new GetRentalQueryHandler(rentalRepository)
            .Handle(new GetRentalQuery(first.Value.Id), default);
        Assert.Equal(finished.Value.FinishedAtUtc, storedFirst.Value.FinishedAtUtc);

        var second = await startHandler.Handle(new StartRentalCommand(vehicleId, driverId), default);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value.Id, second.Value.Id);
        Assert.Equal("Rented", (await GetVehicleAsync(database, vehicleId)).Status);
    }

    [Fact]
    public async Task Repeating_old_finish_does_not_release_a_new_active_rental()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var vehicleId = await RegisterVehicleAsync(database, "AB-123");
        var firstDriverId = await RegisterDriverAsync(database, "Alex Driver");
        var secondDriverId = await RegisterDriverAsync(database, "Sam Driver");
        var startHandler = CreateStartHandler(database);
        var finishHandler = CreateFinishHandler(database);

        var first = await startHandler.Handle(new StartRentalCommand(vehicleId, firstDriverId), default);
        Assert.True(first.IsSuccess);
        var finished = await finishHandler.Handle(new FinishRentalCommand(first.Value.Id), default);
        Assert.True(finished.IsSuccess);
        var second = await startHandler.Handle(new StartRentalCommand(vehicleId, secondDriverId), default);
        Assert.True(second.IsSuccess);

        var repeated = await finishHandler.Handle(new FinishRentalCommand(first.Value.Id), default);

        Assert.Equal("rental.already_finished", repeated.Error.Code);
        Assert.Equal("Rented", (await GetVehicleAsync(database, vehicleId)).Status);
        var rentalRepository = new RentalRepository(new NpgsqlConnectionFactory(database.DataSource));
        var active = await rentalRepository.GetActiveByVehicleIdAsync(vehicleId, default);
        var storedFirst = await rentalRepository.GetByIdAsync(
            first.Value.Id, default);
        Assert.Equal(second.Value.Id, active?.Id);
        Assert.Equal(finished.Value.FinishedAtUtc, storedFirst?.FinishedAtUtc);
    }

    [Fact]
    public async Task Parallel_finish_requests_commit_only_once()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var vehicleId = await RegisterVehicleAsync(database, "AB-123");
        var driverId = await RegisterDriverAsync(database, "Alex Driver");
        var started = await CreateStartHandler(database)
            .Handle(new StartRentalCommand(vehicleId, driverId), default);
        Assert.True(started.IsSuccess);
        var finishHandler = CreateFinishHandler(database);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask = Task.Run(async () =>
        {
            await gate.Task;
            return await finishHandler.Handle(new FinishRentalCommand(started.Value.Id), default);
        });
        var secondTask = Task.Run(async () =>
        {
            await gate.Task;
            return await finishHandler.Handle(new FinishRentalCommand(started.Value.Id), default);
        });
        gate.SetResult();
        var results = await Task.WhenAll(firstTask, secondTask);

        var winner = Assert.Single(results, result => result.IsSuccess);
        var loser = Assert.Single(results, result => result.IsFailure);
        Assert.Equal("rental.already_finished", loser.Error.Code);
        Assert.Equal("Available", (await GetVehicleAsync(database, vehicleId)).Status);
        var stored = await new RentalRepository(new NpgsqlConnectionFactory(database.DataSource))
            .GetByIdAsync(started.Value.Id, default);
        Assert.Equal(winner.Value.FinishedAtUtc, stored?.FinishedAtUtc);
    }

    private static StartRentalCommandHandler CreateStartHandler(IsolatedDatabase database) =>
        new(new RentalRepository(new NpgsqlConnectionFactory(database.DataSource)), TimeProvider.System);

    private static FinishRentalCommandHandler CreateFinishHandler(IsolatedDatabase database) =>
        new(
            new RentalRepository(new NpgsqlConnectionFactory(database.DataSource)),
            TimeProvider.System);

    private static GetActiveRentalByVehicleQueryHandler CreateActiveQueryHandler(
        IsolatedDatabase database, IRentalRepository rentalRepository) =>
        new(new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource)), rentalRepository);

    private static async Task<Guid> RegisterVehicleAsync(IsolatedDatabase database, string plate)
    {
        var repository = new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource));
        var result = await new RegisterVehicleCommandHandler(repository)
            .Handle(new RegisterVehicleCommand(plate), default);
        Assert.True(result.IsSuccess);
        return result.Value.Id;
    }

    private static async Task<Guid> RegisterDriverAsync(IsolatedDatabase database, string name)
    {
        var repository = new DriverRepository(new NpgsqlConnectionFactory(database.DataSource));
        var result = await new RegisterDriverCommandHandler(repository)
            .Handle(new RegisterDriverCommand(name), default);
        Assert.True(result.IsSuccess);
        return result.Value.Id;
    }

    private static async Task<VehicleDto> GetVehicleAsync(IsolatedDatabase database, Guid id)
    {
        var repository = new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource));
        var result = await new GetVehicleQueryHandler(repository)
            .Handle(new GetVehicleQuery(id), default);
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
