using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;
using FleetRentals.Domain.Drivers;

namespace FleetRentals.Application.Features.Drivers;

public sealed record GetDriverQuery(Guid Id);

public sealed class GetDriverQueryHandler(IDriverRepository repository)
{
    public async Task<Result<DriverDto, Error>> HandleAsync(
        GetDriverQuery query, CancellationToken cancellationToken)
    {
        var idResult = DriverId.Create(query.Id);
        if (idResult.IsFailure)
            return idResult.Error;

        var driver = await repository.GetByIdAsync(idResult.Value, cancellationToken);
        return driver is null
            ? Error.NotFound("driver.not_found", "Driver was not found.")
            : DriverDto.From(driver);
    }
}
