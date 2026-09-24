using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Vehicles.Common;

public sealed record VehicleDto(Guid Id, string LicensePlate, string Status)
{
    public static VehicleDto From(Vehicle vehicle) =>
        new(vehicle.Id, vehicle.LicensePlate, vehicle.Status.ToString());
}
