namespace FleetRentals.Domain.Rentals;

public sealed class Rental(
    Guid id,
    Guid vehicleId,
    Guid driverId,
    DateTimeOffset startedAtUtc,
    DateTimeOffset? finishedAtUtc = null)
{
    public Guid Id { get; } = id;

    public Guid VehicleId { get; } = vehicleId;

    public Guid DriverId { get; } = driverId;

    public DateTimeOffset StartedAtUtc { get; } = startedAtUtc;

    public DateTimeOffset? FinishedAtUtc { get; set; } = finishedAtUtc;

    public RentalStatus Status => FinishedAtUtc is null ? RentalStatus.Active : RentalStatus.Finished;
}