namespace FleetRentals.Domain.Vehicles;

public sealed class Vehicle(Guid id, string licensePlate, VehicleStatus status)
{
    public Guid Id { get; } = id;

    public string LicensePlate { get; } = licensePlate;

    public VehicleStatus Status { get; } = status;
}
