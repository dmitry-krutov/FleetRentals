using FleetRentals.Domain.Drivers;
using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Rentals;

public interface IRentalStartSessionFactory
{
    Task<IRentalStartSession> OpenAsync(CancellationToken cancellationToken);
}

public interface IRentalStartSession : IAsyncDisposable
{
    Task<Vehicle?> GetVehicleForUpdateAsync(VehicleId id, CancellationToken cancellationToken);

    Task<Driver?> GetDriverForUpdateAsync(DriverId id, CancellationToken cancellationToken);

    Task<bool> HasActiveRentalForDriverAsync(DriverId id, CancellationToken cancellationToken);

    Task<RentalInsertOutcome> TryInsertAsync(Rental rental, CancellationToken cancellationToken);

    Task<bool> UpdateVehicleStatusAsync(Vehicle vehicle, CancellationToken cancellationToken);

    Task CommitAsync(CancellationToken cancellationToken);
}

public enum RentalInsertOutcome
{
    Inserted,
    VehicleAlreadyRented,
    DriverAlreadyRented,
}
