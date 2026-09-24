using FleetRentals.Domain.Drivers;

namespace FleetRentals.Application.Features.Drivers.Common;

public interface IDriverRepository
{
    Task AddAsync(Driver driver, CancellationToken cancellationToken);

    Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
