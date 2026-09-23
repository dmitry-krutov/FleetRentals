using FleetRentals.Domain;
using FleetRentals.Domain.Common;

namespace FleetRentals.Api.EndpointResults;

public class SuccessResult<TValue> : IResult
{
    private readonly TValue? _value;
    private readonly int _statusCode;
    private readonly string? _location;

    public SuccessResult(TValue value, int statusCode = StatusCodes.Status200OK, string? location = null)
    {
        _value = value;
        _statusCode = statusCode;
        _location = location;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Response.StatusCode = _statusCode;

        if (!string.IsNullOrWhiteSpace(_location))
            httpContext.Response.Headers.Location = _location;

        return httpContext.Response.WriteAsJsonAsync(Envelope.Ok(_value));
    }
}