using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;

namespace FleetRentals.Domain.Vehicles;

public sealed class LicensePlate : ComparableValueObject
{
    public const int MaxLength = 32;

    private LicensePlate(string value) => Value = value;

    public string Value { get; }

    public static Result<LicensePlate, Error> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("vehicle.license_plate.required", "License plate is required.");

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > MaxLength)
            return Error.Validation("vehicle.license_plate.too_long", $"License plate cannot exceed {MaxLength} characters.");

        return new LicensePlate(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }
}
