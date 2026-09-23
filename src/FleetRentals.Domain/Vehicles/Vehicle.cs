using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;

namespace FleetRentals.Domain.Vehicles;

public sealed class Vehicle : Entity<VehicleId>
{
    private Vehicle(VehicleId id, LicensePlate licensePlate, VehicleStatus status)
        : base(id)
    {
        LicensePlate = licensePlate;
        Status = status;
    }

    public LicensePlate LicensePlate { get; }

    public VehicleStatus Status { get; private set; }

    public static Vehicle Register(VehicleId id, LicensePlate licensePlate)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(licensePlate);
        return new Vehicle(id, licensePlate, VehicleStatus.Available);
    }

    public static Result<Vehicle, Error> Restore(VehicleId id, LicensePlate licensePlate, VehicleStatus status)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(licensePlate);

        if (!Enum.IsDefined(status))
            return Error.Validation("vehicle.status.invalid", "Vehicle status is invalid.");

        return new Vehicle(id, licensePlate, status);
    }

    public UnitResult<Error> MarkRented()
    {
        if (Status == VehicleStatus.Rented)
            return UnitResult.Failure(Error.Conflict("vehicle.already_rented", "Vehicle already has an active rental."));

        Status = VehicleStatus.Rented;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAvailable()
    {
        if (Status == VehicleStatus.Available)
            return UnitResult.Failure(Error.Conflict("vehicle.already_available", "Vehicle is already available."));

        Status = VehicleStatus.Available;
        return UnitResult.Success<Error>();
    }
}
