using FleetRentals.Domain.Drivers;
using Xunit;

namespace FleetRentals.UnitTests.Domain;

public sealed class DriverTests
{
    [Fact]
    public void Driver_registration_preserves_normalized_name()
    {
        var id = DriverId.NewId();
        var driver = Driver.Register(id, DriverName.Create("  Alex Driver  ").Value);

        Assert.Equal(id, driver.Id);
        Assert.Equal("Alex Driver", driver.Name.Value);
    }

    [Fact]
    public void Driver_name_and_id_reject_invalid_values()
    {
        Assert.Equal("driver.name.required", DriverName.Create(null).Error.Code);
        Assert.Equal("driver.name.too_long", DriverName.Create(new string('A', 201)).Error.Code);
        Assert.Equal("driver.id.invalid", DriverId.Create(Guid.Empty).Error.Code);
    }
}
