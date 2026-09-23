using FleetRentals.Api.EndpointResults;
using FleetRentals.Application.Features.Drivers;
using Microsoft.AspNetCore.Mvc;

namespace FleetRentals.Api.Controllers.Drivers;

[Route("drivers")]
public sealed class DriversController : ApplicationController
{
    [HttpPost]
    [ProducesResponseType(typeof(Envelope<DriverDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    public async Task<EndpointResult<DriverDto>> Register(
        [FromBody] RegisterDriverRequest request,
        [FromServices] RegisterDriverCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RegisterDriverCommand(request.Name), cancellationToken);

        if (result.IsFailure)
            return result;

        return EndpointResult<DriverDto>.Created($"/drivers/{result.Value.Id}", result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Envelope<DriverDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    public async Task<EndpointResult<DriverDto>> GetById(
        [FromRoute] Guid id,
        [FromServices] GetDriverQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(new GetDriverQuery(id), cancellationToken);
    }
}
