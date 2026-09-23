using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Vehicles;

public sealed record GetVehicleQuery(Guid Id);

public sealed class GetVehicleQueryHandler(IVehicleRepository repository)
{
    public async Task<Result<VehicleDto, Error>> HandleAsync(
        GetVehicleQuery query, CancellationToken cancellationToken)
    {
        var idResult = VehicleId.Create(query.Id);
        if (idResult.IsFailure)
            return idResult.Error;

        var vehicle = await repository.GetByIdAsync(idResult.Value, cancellationToken);
        return vehicle is null
            ? Error.NotFound("vehicle.not_found", "Vehicle was not found.")
            : VehicleDto.From(vehicle);
    }
}
