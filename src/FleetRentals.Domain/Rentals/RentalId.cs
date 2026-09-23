using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;

namespace FleetRentals.Domain.Rentals;

public sealed class RentalId : ComparableValueObject
{
    private RentalId(Guid value) => Value = value;

    public Guid Value { get; }

    public static RentalId NewId() => new(Guid.NewGuid());

    public static Result<RentalId, Error> Create(Guid value) => value == Guid.Empty
        ? Error.Validation("rental.id.invalid", "Rental id cannot be empty.")
        : new RentalId(value);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }
}
