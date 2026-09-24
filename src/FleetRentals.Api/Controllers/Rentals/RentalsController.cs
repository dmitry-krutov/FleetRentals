using FleetRentals.Application.Features.Rentals;
using FleetRentals.Application.Features.Rentals.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FleetRentals.Api.Controllers.Rentals;

[Route("rentals")]
public sealed class RentalsController(ISender sender) : ApplicationController
{
    [HttpPost]
    [ProducesResponseType(typeof(Envelope<RentalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start(
        [FromBody] StartRentalRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StartRentalCommand(request.VehicleId, request.DriverId);
        var result = await sender.Send(command, cancellationToken);
        return CreatedResult(result, "rentals", rental => rental.Id);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Envelope<RentalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetRentalQuery(id);
        return FromResult(await sender.Send(query, cancellationToken));
    }

    [HttpPost("{id}/finish")]
    [ProducesResponseType(typeof(Envelope<RentalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Finish(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new FinishRentalCommand(id);
        return FromResult(await sender.Send(command, cancellationToken));
    }
}
