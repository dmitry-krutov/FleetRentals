using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Rentals;

public interface IRentalReadRepository
{
    Task<Rental?> GetByIdAsync(RentalId id, CancellationToken cancellationToken);

    Task<Rental?> GetActiveByVehicleIdAsync(VehicleId vehicleId, CancellationToken cancellationToken);
}
