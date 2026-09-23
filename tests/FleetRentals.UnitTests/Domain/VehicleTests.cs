using FleetRentals.Domain.Vehicles;
using Xunit;

namespace FleetRentals.UnitTests.Domain;

public sealed class VehicleTests
{
    [Fact]
    public void Registered_vehicle_is_available_and_can_be_rented_then_returned()
    {
        var vehicle = Vehicle.Register(VehicleId.NewId(), LicensePlate.Create(" ab-123 ").Value);

        Assert.Equal("AB-123", vehicle.LicensePlate.Value);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
        Assert.True(vehicle.MarkRented().IsSuccess);
        Assert.Equal(VehicleStatus.Rented, vehicle.Status);
        Assert.True(vehicle.MarkAvailable().IsSuccess);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
    }

    [Fact]
    public void Invalid_vehicle_transitions_do_not_change_state()
    {
        var vehicle = Vehicle.Register(VehicleId.NewId(), LicensePlate.Create("AB-123").Value);

        var prematureReturn = vehicle.MarkAvailable();
        Assert.True(prematureReturn.IsFailure);
        Assert.Equal("vehicle.already_available", prematureReturn.Error.Code);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);

        Assert.True(vehicle.MarkRented().IsSuccess);
        var duplicateRental = vehicle.MarkRented();
        Assert.True(duplicateRental.IsFailure);
        Assert.Equal("vehicle.already_rented", duplicateRental.Error.Code);
        Assert.Equal(VehicleStatus.Rented, vehicle.Status);
    }

    [Fact]
    public void Restore_rejects_unknown_vehicle_status()
    {
        var result = Vehicle.Restore(VehicleId.NewId(), LicensePlate.Create("AB-123").Value, (VehicleStatus)42);

        Assert.True(result.IsFailure);
        Assert.Equal("vehicle.status.invalid", result.Error.Code);
    }

    [Fact]
    public void License_plate_and_vehicle_id_reject_invalid_values()
    {
        Assert.Equal("vehicle.license_plate.required", LicensePlate.Create("  ").Error.Code);
        Assert.Equal("vehicle.license_plate.too_long", LicensePlate.Create(new string('A', 33)).Error.Code);
        Assert.Equal("vehicle.id.invalid", VehicleId.Create(Guid.Empty).Error.Code);
        Assert.Equal(LicensePlate.Create(" ab-123 ").Value, LicensePlate.Create("AB-123").Value);
    }
}
