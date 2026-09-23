using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Vehicles;

public interface IVehicleRepository
{
    Task<bool> AddAsync(Vehicle vehicle, CancellationToken cancellationToken);

    Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken cancellationToken);
}
