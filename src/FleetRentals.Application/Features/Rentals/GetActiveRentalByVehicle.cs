using CSharpFunctionalExtensions;
using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Domain.Common;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Rentals;

public sealed record GetActiveRentalByVehicleQuery(Guid VehicleId);

public sealed class GetActiveRentalByVehicleQueryHandler(
    IVehicleRepository vehicleRepository,
    IRentalReadRepository rentalRepository)
{
    public async Task<Result<RentalDto, Error>> HandleAsync(
        GetActiveRentalByVehicleQuery query, CancellationToken cancellationToken)
    {
        var idResult = VehicleId.Create(query.VehicleId);
        if (idResult.IsFailure)
            return idResult.Error;

        var vehicle = await vehicleRepository.GetByIdAsync(idResult.Value, cancellationToken);
        if (vehicle is null)
            return Error.NotFound("vehicle.not_found", "Vehicle was not found.");

        var rental = await rentalRepository.GetActiveByVehicleIdAsync(idResult.Value, cancellationToken);
        return rental is null
            ? Error.NotFound("rental.active.not_found", "Vehicle has no active rental.")
            : RentalDto.From(rental);
    }
}
