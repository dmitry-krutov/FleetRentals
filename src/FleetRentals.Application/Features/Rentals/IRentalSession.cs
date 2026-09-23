using FleetRentals.Domain.Drivers;
using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Rentals;

public interface IRentalSessionFactory
{
    Task<IRentalSession> OpenAsync(CancellationToken cancellationToken);
}

public interface IRentalSession : IAsyncDisposable
{
    Task<Rental?> GetRentalAsync(RentalId id, CancellationToken cancellationToken);

    Task<Rental?> GetRentalForUpdateAsync(RentalId id, CancellationToken cancellationToken);

    Task<Vehicle?> GetVehicleForUpdateAsync(VehicleId id, CancellationToken cancellationToken);

    Task<Driver?> GetDriverForUpdateAsync(DriverId id, CancellationToken cancellationToken);

    Task<bool> HasActiveRentalForDriverAsync(DriverId id, CancellationToken cancellationToken);

    Task<RentalInsertOutcome> TryInsertAsync(Rental rental, CancellationToken cancellationToken);

    Task<bool> UpdateRentalFinishAsync(Rental rental, CancellationToken cancellationToken);

    Task<bool> UpdateVehicleStatusAsync(
        Vehicle vehicle, VehicleStatus expectedStatus, CancellationToken cancellationToken);

    Task CommitAsync(CancellationToken cancellationToken);
}

public enum RentalInsertOutcome
{
    Inserted,
    VehicleAlreadyRented,
    DriverAlreadyRented,
}
