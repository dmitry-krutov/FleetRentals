using FleetRentals.Application.Features.Drivers;
using FleetRentals.Application.Features.Rentals;
using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Domain.Vehicles;
using FleetRentals.Persistence;
using Xunit;

namespace FleetRentals.IntegrationTests;

public sealed class RentalStartTests
{
    [Fact]
    public async Task Start_creates_active_rental_and_marks_vehicle_rented()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var vehicleId = await RegisterVehicleAsync(database, "AB-123");
        var driverId = await RegisterDriverAsync(database, "Alex Driver");
        var handler = CreateStartHandler(database);

        var result = await handler.HandleAsync(new StartRentalCommand(vehicleId, driverId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(vehicleId, result.Value.VehicleId);
        Assert.Equal(driverId, result.Value.DriverId);
        Assert.Equal(TimeSpan.Zero, result.Value.StartedAtUtc.Offset);

        var readRepository = new RentalReadRepository(new NpgsqlConnectionFactory(database.DataSource));
        var loaded = await new GetRentalQueryHandler(readRepository)
            .HandleAsync(new GetRentalQuery(result.Value.Id), default);
        var current = await CreateActiveQueryHandler(database, readRepository)
            .HandleAsync(new GetActiveRentalByVehicleQuery(vehicleId), default);

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

        var first = await handler.HandleAsync(new StartRentalCommand(vehicleId, firstDriverId), default);
        var second = await handler.HandleAsync(new StartRentalCommand(vehicleId, secondDriverId), default);

        Assert.True(first.IsSuccess);
        Assert.Equal("vehicle.already_rented", second.Error.Code);
        var active = await new RentalReadRepository(new NpgsqlConnectionFactory(database.DataSource))
            .GetActiveByVehicleIdAsync(VehicleId.Create(vehicleId).Value, default);
        Assert.Equal(first.Value.Id, active?.Id.Value);
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

        var first = await handler.HandleAsync(new StartRentalCommand(firstVehicleId, driverId), default);
        var second = await handler.HandleAsync(new StartRentalCommand(secondVehicleId, driverId), default);

        Assert.True(first.IsSuccess);
        Assert.Equal("rental.driver.busy", second.Error.Code);
        Assert.Equal("Rented", (await GetVehicleAsync(database, firstVehicleId)).Status);
        Assert.Equal("Available", (await GetVehicleAsync(database, secondVehicleId)).Status);
        var active = await new RentalReadRepository(new NpgsqlConnectionFactory(database.DataSource))
            .GetActiveByVehicleIdAsync(VehicleId.Create(firstVehicleId).Value, default);
        Assert.Equal(first.Value.Id, active?.Id.Value);
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
            return await handler.HandleAsync(new StartRentalCommand(vehicleId, firstDriverId), default);
        });
        var secondTask = Task.Run(async () =>
        {
            await gate.Task;
            return await handler.HandleAsync(new StartRentalCommand(vehicleId, secondDriverId), default);
        });
        gate.SetResult();
        var results = await Task.WhenAll(firstTask, secondTask);

        var winner = Assert.Single(results, result => result.IsSuccess);
        var loser = Assert.Single(results, result => result.IsFailure);
        Assert.Equal("vehicle.already_rented", loser.Error.Code);
        var active = await new RentalReadRepository(new NpgsqlConnectionFactory(database.DataSource))
            .GetActiveByVehicleIdAsync(VehicleId.Create(vehicleId).Value, default);
        Assert.Equal(winner.Value.Id, active?.Id.Value);
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
            return await handler.HandleAsync(new StartRentalCommand(firstVehicleId, driverId), default);
        });
        var secondTask = Task.Run(async () =>
        {
            await gate.Task;
            return await handler.HandleAsync(new StartRentalCommand(secondVehicleId, driverId), default);
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

    private static StartRentalCommandHandler CreateStartHandler(IsolatedDatabase database) =>
        new(new RentalStartSessionFactory(new NpgsqlConnectionFactory(database.DataSource)), TimeProvider.System);

    private static GetActiveRentalByVehicleQueryHandler CreateActiveQueryHandler(
        IsolatedDatabase database, IRentalReadRepository readRepository) =>
        new(new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource)), readRepository);

    private static async Task<Guid> RegisterVehicleAsync(IsolatedDatabase database, string plate)
    {
        var repository = new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource));
        var result = await new RegisterVehicleCommandHandler(repository)
            .HandleAsync(new RegisterVehicleCommand(plate), default);
        Assert.True(result.IsSuccess);
        return result.Value.Id;
    }

    private static async Task<Guid> RegisterDriverAsync(IsolatedDatabase database, string name)
    {
        var repository = new DriverRepository(new NpgsqlConnectionFactory(database.DataSource));
        var result = await new RegisterDriverCommandHandler(repository)
            .HandleAsync(new RegisterDriverCommand(name), default);
        Assert.True(result.IsSuccess);
        return result.Value.Id;
    }

    private static async Task<VehicleDto> GetVehicleAsync(IsolatedDatabase database, Guid id)
    {
        var repository = new VehicleRepository(new NpgsqlConnectionFactory(database.DataSource));
        var result = await new GetVehicleQueryHandler(repository)
            .HandleAsync(new GetVehicleQuery(id), default);
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
