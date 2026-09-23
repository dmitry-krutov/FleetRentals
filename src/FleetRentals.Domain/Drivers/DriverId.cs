using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;

namespace FleetRentals.Domain.Drivers;

public sealed class DriverId : ComparableValueObject
{
    private DriverId(Guid value) => Value = value;

    public Guid Value { get; }

    public static DriverId NewId() => new(Guid.NewGuid());

    public static Result<DriverId, Error> Create(Guid value) => value == Guid.Empty
        ? Error.Validation("driver.id.invalid", "Driver id cannot be empty.")
        : new DriverId(value);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }
}
