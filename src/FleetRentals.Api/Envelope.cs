using FleetRentals.Application.Common;

namespace FleetRentals.Api;

public sealed record Envelope(object? Result, IReadOnlyList<Error>? Errors, DateTimeOffset TimeGenerated)
{
    public bool IsError => Errors is { Count: > 0 };

    public static Envelope Ok(object? result = null) => new(result, null, DateTimeOffset.UtcNow);
    public static Envelope Failure(IEnumerable<Error> errors) =>
        new(null, errors.ToArray(), DateTimeOffset.UtcNow);
}

public sealed record Envelope<T>(T? Result, IReadOnlyList<Error>? Errors, DateTimeOffset TimeGenerated)
{
    public bool IsError => Errors is { Count: > 0 };

    public static Envelope<T> Ok(T result) => new(result, null, DateTimeOffset.UtcNow);
}
