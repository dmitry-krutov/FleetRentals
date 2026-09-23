using FleetRentals.Api.EndpointResults;
using FleetRentals.Application.Features.Rentals;
using Microsoft.AspNetCore.Mvc;

namespace FleetRentals.Api.Controllers.Rentals;

[Route("rentals")]
public sealed class RentalsController : ApplicationController
{
    [HttpPost]
    [ProducesResponseType(typeof(Envelope<RentalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    public async Task<EndpointResult<RentalDto>> Start(
        [FromBody] StartRentalRequest request,
        [FromServices] StartRentalCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new StartRentalCommand(request.VehicleId, request.DriverId), cancellationToken);

        if (result.IsFailure)
            return result;

        return EndpointResult<RentalDto>.Created($"/rentals/{result.Value.Id}", result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Envelope<RentalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    public async Task<EndpointResult<RentalDto>> GetById(
        [FromRoute] Guid id,
        [FromServices] GetRentalQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(new GetRentalQuery(id), cancellationToken);
    }

    [HttpPost("{id}/finish")]
    [ProducesResponseType(typeof(Envelope<RentalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    public async Task<EndpointResult<RentalDto>> Finish(
        [FromRoute] Guid id,
        [FromServices] FinishRentalCommandHandler handler,
        CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(new FinishRentalCommand(id), cancellationToken);
    }
}
