using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;

namespace FleetRentals.Domain.Vehicles;

public sealed class VehicleId : ComparableValueObject
{
    private VehicleId(Guid value) => Value = value;

    public Guid Value { get; }

    public static VehicleId NewId() => new(Guid.NewGuid());

    public static Result<VehicleId, Error> Create(Guid value) => value == Guid.Empty
        ? Error.Validation("vehicle.id.invalid", "Vehicle id cannot be empty.")
        : new VehicleId(value);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }
}
