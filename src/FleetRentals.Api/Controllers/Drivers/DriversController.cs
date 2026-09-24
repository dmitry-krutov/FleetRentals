using FleetRentals.Application.Features.Drivers;
using FleetRentals.Application.Features.Drivers.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FleetRentals.Api.Controllers.Drivers;

[Route("drivers")]
public sealed class DriversController(ISender sender) : ApplicationController
{
    [HttpPost]
    [ProducesResponseType(typeof(Envelope<DriverDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDriverRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterDriverCommand(request.Name);
        var result = await sender.Send(command, cancellationToken);
        return CreatedResult(result, "drivers", driver => driver.Id);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Envelope<DriverDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetDriverQuery(id);
        return FromResult(await sender.Send(query, cancellationToken));
    }
}
