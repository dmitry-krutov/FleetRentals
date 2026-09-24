using FleetRentals.Application.Features.Drivers;
using FleetRentals.Persistence;
using FleetRentals.Persistence.Repositories;
using Xunit;

namespace FleetRentals.IntegrationTests;

public sealed class DriverRepositoryTests
{
    [Fact]
    public async Task Registered_driver_can_be_loaded_from_postgresql()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var repository = new DriverRepository(new NpgsqlConnectionFactory(database.DataSource));
        var register = new RegisterDriverCommandHandler(repository);

        var created = await register.Handle(new RegisterDriverCommand("  Alex Driver  "), default);
        Assert.True(created.IsSuccess);

        var loaded = await new GetDriverQueryHandler(repository)
            .Handle(new GetDriverQuery(created.Value.Id), default);

        Assert.True(loaded.IsSuccess);
        Assert.Equal(created.Value.Id, loaded.Value.Id);
        Assert.Equal("Alex Driver", loaded.Value.Name);
    }

    [Fact]
    public async Task Database_accepts_different_drivers_with_the_same_name()
    {
        await using var database = await IsolatedDatabase.CreateAsync();
        var repository = new DriverRepository(new NpgsqlConnectionFactory(database.DataSource));
        var register = new RegisterDriverCommandHandler(repository);

        var first = await register.Handle(new RegisterDriverCommand("Alex Driver"), default);
        var second = await register.Handle(new RegisterDriverCommand("Alex Driver"), default);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value.Id, second.Value.Id);
    }
}
