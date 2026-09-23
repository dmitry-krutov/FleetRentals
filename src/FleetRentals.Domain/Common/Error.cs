namespace FleetRentals.Domain.Common;

public record Error
{
    public const string SEPARATOR = "||";

    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }
    public string? InvalidField { get; }

    private Error(string code, string message, ErrorType type, string? invalidField = null)
    {
        Code = code;
        Message = message;
        Type = type;
        InvalidField = invalidField;
    }

    public static Error Validation(string code, string message, string? invalidField = null)
        => new(code, message, ErrorType.VALIDATION, invalidField);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NOT_FOUND);
    public static Error AlreadyExist(string code, string message) => new(code, message, ErrorType.ALREADY_EXISTS);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.FAILURE);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.CONFLICT);
    public static Error Internal(string code, string message) => new(code, message, ErrorType.INTERNAL);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.FORBIDDEN);
    public static Error TooManyRequests(string code, string message) => new(code, message, ErrorType.TOO_MANY_REQUESTS);

    public string Serialize()
    {
        return string.Join(SEPARATOR, Code, Message, Type);
    }

    public static Error Deserialize(string serialized)
    {
        var parts = serialized.Split(SEPARATOR);

        if (parts.Length < 3)
        {
            throw new ArgumentException("Invalid serialized format");
        }

        if (Enum.TryParse<ErrorType>(parts[2], out var type) == false)
        {
            throw new ArgumentException("Invalid serialized format");
        }

        return new Error(parts[0], parts[1], type);
    }

    public ErrorList ToErrorList() => new([this]);
}

public enum ErrorType
{
    VALIDATION,
    NOT_FOUND,
    FAILURE,
    CONFLICT,
    INTERNAL,
    ALREADY_EXISTS,
    FORBIDDEN,
    TOO_MANY_REQUESTS,
}