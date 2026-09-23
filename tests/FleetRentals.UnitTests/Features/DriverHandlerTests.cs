using FleetRentals.Application.Features.Drivers;
using FleetRentals.Domain.Drivers;
using Xunit;

namespace FleetRentals.UnitTests.Features;

public sealed class DriverHandlerTests
{
    [Theory]
    [InlineData(null, "driver.name.required")]
    [InlineData("  ", "driver.name.required")]
    public async Task Registration_rejects_blank_name_without_writing(string? name, string errorCode)
    {
        var repository = new FakeDriverRepository();
        var result = await new RegisterDriverCommandHandler(repository)
            .HandleAsync(new RegisterDriverCommand(name), default);

        Assert.True(result.IsFailure);
        Assert.Equal(errorCode, result.Error.Code);
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task Registration_rejects_too_long_name_without_writing()
    {
        var repository = new FakeDriverRepository();
        var result = await new RegisterDriverCommandHandler(repository)
            .HandleAsync(new RegisterDriverCommand(new string('A', DriverName.MaxLength + 1)), default);

        Assert.True(result.IsFailure);
        Assert.Equal("driver.name.too_long", result.Error.Code);
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task Registration_and_lookup_return_normalized_driver_data()
    {
        var repository = new FakeDriverRepository();
        var registered = await new RegisterDriverCommandHandler(repository)
            .HandleAsync(new RegisterDriverCommand("  Alex Driver  "), default);

        Assert.True(registered.IsSuccess);
        var loaded = await new GetDriverQueryHandler(repository)
            .HandleAsync(new GetDriverQuery(registered.Value.Id), default);

        Assert.True(loaded.IsSuccess);
        Assert.Equal("Alex Driver", loaded.Value.Name);
        Assert.Equal(registered.Value.Id, loaded.Value.Id);
    }

    [Fact]
    public async Task Lookup_rejects_empty_id_and_reports_missing_driver()
    {
        var repository = new FakeDriverRepository();
        var handler = new GetDriverQueryHandler(repository);

        var empty = await handler.HandleAsync(new GetDriverQuery(Guid.Empty), default);
        var missing = await handler.HandleAsync(new GetDriverQuery(Guid.NewGuid()), default);

        Assert.Equal("driver.id.invalid", empty.Error.Code);
        Assert.Equal("driver.not_found", missing.Error.Code);
        Assert.Equal(1, repository.GetCalls);
    }

    private sealed class FakeDriverRepository : IDriverRepository
    {
        public int AddCalls { get; private set; }

        public int GetCalls { get; private set; }

        private Driver? LastAdded { get; set; }

        public Task AddAsync(Driver driver, CancellationToken cancellationToken)
        {
            AddCalls++;
            LastAdded = driver;
            return Task.CompletedTask;
        }

        public Task<Driver?> GetByIdAsync(DriverId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            return Task.FromResult(LastAdded?.Id == id ? LastAdded : null);
        }
    }
}
