using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Vehicles.Common;

public interface IVehicleRepository
{
    Task<bool> AddAsync(Vehicle vehicle, CancellationToken cancellationToken);

    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
