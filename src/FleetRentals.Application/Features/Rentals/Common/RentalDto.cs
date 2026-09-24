using FleetRentals.Domain.Rentals;

namespace FleetRentals.Application.Features.Rentals.Common;

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
            rental.Id,
            rental.VehicleId,
            rental.DriverId,
            rental.StartedAtUtc,
            rental.FinishedAtUtc,
            rental.Status.ToString());
}
