using CSharpFunctionalExtensions;

namespace FleetRentals.Domain.Drivers;

public sealed class Driver : Entity<DriverId>
{
    private Driver(DriverId id, DriverName name)
        : base(id)
    {
        Name = name;
    }

    public DriverName Name { get; }

    public static Driver Register(DriverId id, DriverName name)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);
        return new Driver(id, name);
    }
}
