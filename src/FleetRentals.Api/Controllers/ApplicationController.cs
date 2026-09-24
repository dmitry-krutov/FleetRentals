using CSharpFunctionalExtensions;
using FleetRentals.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FleetRentals.Api.Controllers;

[ApiController]
public abstract class ApplicationController : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T, Error> result)
    {
        if (result.IsSuccess)
            return Ok(Envelope<T>.Ok(result.Value));

        var statusCode = result.Error.Type switch
        {
            ErrorType.VALIDATION => StatusCodes.Status400BadRequest,
            ErrorType.NOT_FOUND => StatusCodes.Status404NotFound,
            ErrorType.CONFLICT => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };
        return StatusCode(statusCode, Envelope.Failure([result.Error]));
    }

    protected IActionResult CreatedResult<T>(Result<T, Error> result, string route, Func<T, Guid> id)
    {
        if (result.IsFailure)
            return FromResult(result);

        Response.Headers.Location = $"/{route}/{id(result.Value)}";
        return StatusCode(StatusCodes.Status201Created, Envelope<T>.Ok(result.Value));
    }
}
