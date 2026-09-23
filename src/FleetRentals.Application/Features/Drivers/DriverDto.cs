using FleetRentals.Domain.Drivers;

namespace FleetRentals.Application.Features.Drivers;

public sealed record DriverDto(Guid Id, string Name)
{
    public static DriverDto From(Driver driver) =>
        new(driver.Id.Value, driver.Name.Value);
}
