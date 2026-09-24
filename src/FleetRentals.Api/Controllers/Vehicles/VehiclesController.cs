using FleetRentals.Application.Features.Rentals;
using FleetRentals.Application.Features.Rentals.Common;
using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Application.Features.Vehicles.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FleetRentals.Api.Controllers.Vehicles;

[Route("vehicles")]
public sealed class VehiclesController(ISender sender) : ApplicationController
{
    [HttpPost]
    [ProducesResponseType(typeof(Envelope<VehicleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterVehicleCommand(request.LicensePlate);
        var result = await sender.Send(command, cancellationToken);
        return CreatedResult(result, "vehicles", vehicle => vehicle.Id);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Envelope<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetVehicleQuery(id);
        return FromResult(await sender.Send(query, cancellationToken));
    }

    [HttpGet("{id}/active-rental")]
    [ProducesResponseType(typeof(Envelope<RentalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveRental(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetActiveRentalByVehicleQuery(id);
        return FromResult(await sender.Send(query, cancellationToken));
    }
}
