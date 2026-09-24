using FleetRentals.Domain.Drivers;
using Xunit;

namespace FleetRentals.UnitTests.Domain;

public sealed class DriverTests
{
    [Fact]
    public void Driver_holds_flat_data()
    {
        var id = Guid.NewGuid();
        var driver = new Driver(id, "Alex Driver");
        Assert.Equal(id, driver.Id);
        Assert.Equal("Alex Driver", driver.Name);
    }
}
