using FleetRentals.Domain;
using FleetRentals.Domain.Common;

namespace FleetRentals.Api.EndpointResults;

public class ErrorsResult : IResult
{
    private readonly ErrorList _errors;

    public ErrorsResult(Error error)
    {
        _errors = error;
    }

    public ErrorsResult(ErrorList errors)
    {
        _errors = errors;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(_errors);

        if (!_errors.Any())
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            return httpContext.Response.WriteAsJsonAsync(Envelope.Error(_errors));
        }

        var distinctErrorTypes = _errors
            .Select(x => x.Type)
            .Distinct()
            .ToList();

        int statusCode = distinctErrorTypes.Count > 1
            ? StatusCodes.Status500InternalServerError
            : ErrorHttpStatusMapper.Map(distinctErrorTypes.First());

        var envelope = Envelope.Error(_errors);
        httpContext.Response.StatusCode = statusCode;

        return httpContext.Response.WriteAsJsonAsync(envelope);
    }

}
