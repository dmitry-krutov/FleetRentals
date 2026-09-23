using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;
using FleetRentals.Domain.Drivers;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Domain.Rentals;

public sealed class Rental : Entity<RentalId>
{
    private Rental(
        RentalId id,
        VehicleId vehicleId,
        DriverId driverId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? finishedAtUtc)
        : base(id)
    {
        VehicleId = vehicleId;
        DriverId = driverId;
        StartedAtUtc = startedAtUtc;
        FinishedAtUtc = finishedAtUtc;
    }

    public VehicleId VehicleId { get; }

    public DriverId DriverId { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset? FinishedAtUtc { get; private set; }

    public RentalStatus Status => FinishedAtUtc is null ? RentalStatus.Active : RentalStatus.Finished;

    public static Rental Start(RentalId id, VehicleId vehicleId, DriverId driverId, DateTimeOffset startedAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(vehicleId);
        ArgumentNullException.ThrowIfNull(driverId);
        return new Rental(id, vehicleId, driverId, startedAt.ToUniversalTime(), null);
    }

    public static Result<Rental, Error> Restore(
        RentalId id,
        VehicleId vehicleId,
        DriverId driverId,
        DateTimeOffset startedAt,
        DateTimeOffset? finishedAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(vehicleId);
        ArgumentNullException.ThrowIfNull(driverId);

        var startedAtUtc = startedAt.ToUniversalTime();
        var finishedAtUtc = finishedAt?.ToUniversalTime();
        if (finishedAtUtc < startedAtUtc)
            return Error.Validation("rental.finish_before_start", "Rental cannot finish before it starts.");

        return new Rental(id, vehicleId, driverId, startedAtUtc, finishedAtUtc);
    }

    public UnitResult<Error> Finish(DateTimeOffset finishedAt)
    {
        if (FinishedAtUtc is not null)
            return UnitResult.Failure(Error.Conflict("rental.already_finished", "Rental is already finished."));

        var finishedAtUtc = finishedAt.ToUniversalTime();
        if (finishedAtUtc < StartedAtUtc)
            return UnitResult.Failure(Error.Validation("rental.finish_before_start", "Rental cannot finish before it starts."));

        FinishedAtUtc = finishedAtUtc;
        return UnitResult.Success<Error>();
    }
}
