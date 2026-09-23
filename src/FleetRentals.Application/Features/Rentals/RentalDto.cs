using FleetRentals.Domain.Rentals;

namespace FleetRentals.Application.Features.Rentals;

public sealed record RentalDto(
    Guid Id,
    Guid VehicleId,
    Guid DriverId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string Status)
{
    public static RentalDto From(Rental rental) =>
        new(
            rental.Id.Value,
            rental.VehicleId.Value,
            rental.DriverId.Value,
            rental.StartedAtUtc,
            rental.FinishedAtUtc,
            rental.Status.ToString());
}
