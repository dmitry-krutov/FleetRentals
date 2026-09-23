using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;

namespace FleetRentals.Domain.Drivers;

public sealed class DriverName : ComparableValueObject
{
    public const int MaxLength = 200;

    private DriverName(string value) => Value = value;

    public string Value { get; }

    public static Result<DriverName, Error> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("driver.name.required", "Driver name is required.");

        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
            return Error.Validation("driver.name.too_long", $"Driver name cannot exceed {MaxLength} characters.");

        return new DriverName(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }
}
