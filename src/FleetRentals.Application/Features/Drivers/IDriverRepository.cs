using FleetRentals.Domain.Drivers;

namespace FleetRentals.Application.Features.Drivers;

public interface IDriverRepository
{
    Task AddAsync(Driver driver, CancellationToken cancellationToken);

    Task<Driver?> GetByIdAsync(DriverId id, CancellationToken cancellationToken);
}
