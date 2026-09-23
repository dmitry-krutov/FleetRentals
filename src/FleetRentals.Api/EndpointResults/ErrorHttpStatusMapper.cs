using FleetRentals.Domain.Common;

namespace FleetRentals.Api.EndpointResults;

public static class ErrorHttpStatusMapper
{
    public static int Map(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.VALIDATION => StatusCodes.Status400BadRequest,
            ErrorType.NOT_FOUND => StatusCodes.Status404NotFound,
            ErrorType.CONFLICT => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
}
