using FleetRentals.Domain.Rentals;

namespace FleetRentals.Application.Features.Rentals.Common;

public interface IRentalRepository
{
    Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Rental?> GetActiveByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<RentalInsertOutcome> TryAddAsync(Rental rental, CancellationToken cancellationToken);

    Task<bool> TryFinishAsync(Guid id, DateTimeOffset finishedAtUtc, CancellationToken cancellationToken);
}

public enum RentalInsertOutcome
{
    Inserted,
    VehicleNotFound,
    DriverNotFound,
    VehicleAlreadyRented,
    DriverAlreadyRented,
}
