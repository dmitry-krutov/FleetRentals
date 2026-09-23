using FleetRentals.Domain;
using FleetRentals.Domain.Common;

namespace FleetRentals.Api.EndpointResults;

public static class ErrorHttpStatusMapper
{
    public static int Map(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.VALIDATION => StatusCodes.Status400BadRequest,
            ErrorType.NOT_FOUND => StatusCodes.Status404NotFound,
            ErrorType.CONFLICT or ErrorType.ALREADY_EXISTS => StatusCodes.Status409Conflict,
            ErrorType.FORBIDDEN => StatusCodes.Status403Forbidden,
            ErrorType.TOO_MANY_REQUESTS => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status500InternalServerError
        };
}