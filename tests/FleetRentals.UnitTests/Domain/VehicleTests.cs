using FleetRentals.Domain.Vehicles;
using Xunit;

namespace FleetRentals.UnitTests.Domain;

public sealed class VehicleTests
{
    [Fact]
    public void Vehicle_holds_status_snapshot_from_lookup()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), "AB-123", VehicleStatus.Rented);
        Assert.Equal("AB-123", vehicle.LicensePlate);
        Assert.Equal(VehicleStatus.Rented, vehicle.Status);
    }
}
