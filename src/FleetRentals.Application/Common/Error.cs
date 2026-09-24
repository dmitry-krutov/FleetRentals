namespace FleetRentals.Application.Common;

public sealed record Error(string Code, string Message, ErrorType Type, string? InvalidField = null)
{
    public static Error Validation(string code, string message, string? invalidField = null) =>
        new(code, message, ErrorType.VALIDATION, invalidField);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NOT_FOUND);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.CONFLICT);
    public static Error Internal(string code, string message) => new(code, message, ErrorType.INTERNAL);
}

public enum ErrorType
{
    VALIDATION,
    NOT_FOUND,
    CONFLICT,
    INTERNAL,
}
