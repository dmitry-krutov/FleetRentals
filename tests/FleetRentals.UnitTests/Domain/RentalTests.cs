using FleetRentals.Domain.Rentals;
using Xunit;

namespace FleetRentals.UnitTests.Domain;

public sealed class RentalTests
{
    [Fact]
    public void Status_is_derived_from_finish_time()
    {
        var started = DateTimeOffset.UtcNow;
        var rental = new Rental(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), started);
        Assert.Equal(RentalStatus.Active, rental.Status);
        rental.FinishedAtUtc = started.AddHours(1);
        Assert.Equal(RentalStatus.Finished, rental.Status);
    }
}
