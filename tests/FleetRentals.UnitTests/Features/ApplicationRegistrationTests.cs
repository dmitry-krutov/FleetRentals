using FleetRentals.Application;
using FleetRentals.Application.Features.Drivers;
using FleetRentals.Application.Features.Drivers.Common;
using FleetRentals.Domain.Drivers;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FleetRentals.UnitTests.Features;

public sealed class ApplicationRegistrationTests
{
    [Fact]
    public async Task Sender_validates_before_writing()
    {
        var repository = new FakeDriverRepository();
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IDriverRepository>(repository);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new RegisterDriverCommand(" "), default));

        Assert.Contains(exception.Errors, error => error.ErrorCode == "driver.name.required");
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task Sender_dispatches_valid_request_to_handler()
    {
        var repository = new FakeDriverRepository();
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IDriverRepository>(repository);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new RegisterDriverCommand("  Alex Driver  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Alex Driver", result.Value.Name);
        Assert.Equal(1, repository.AddCalls);
    }

    private sealed class FakeDriverRepository : IDriverRepository
    {
        public int AddCalls { get; private set; }

        public Task AddAsync(Driver driver, CancellationToken cancellationToken)
        {
            AddCalls++;
            return Task.CompletedTask;
        }

        public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Driver?>(null);
    }
}
