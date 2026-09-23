using FleetRentals.Api.EndpointResults;
using FleetRentals.Application.Features.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace FleetRentals.Api.Controllers.Vehicles;

[Route("vehicles")]
public sealed class VehiclesController : ApplicationController
{
    [HttpPost]
    [ProducesResponseType(typeof(Envelope<VehicleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    public async Task<EndpointResult<VehicleDto>> Register(
        [FromBody] RegisterVehicleRequest request,
        [FromServices] RegisterVehicleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegisterVehicleCommand(request.LicensePlate), cancellationToken);

        if (result.IsFailure)
            return result;

        return EndpointResult<VehicleDto>.Created($"/vehicles/{result.Value.Id}", result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Envelope<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    public async Task<EndpointResult<VehicleDto>> GetById(
        [FromRoute] Guid id,
        [FromServices] GetVehicleQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(new GetVehicleQuery(id), cancellationToken);
    }
}
