using FleetRentals.Domain.Common;

namespace FleetRentals.Application.Common.Validation;

public class Errors
{
    public static class Validation
    {
        public static Error ValueIsRequired(string? name = null)
        {
            var label = name ?? "value";
            return Error.Validation("value.is.required", $"{label} is required");
        }
    }
}