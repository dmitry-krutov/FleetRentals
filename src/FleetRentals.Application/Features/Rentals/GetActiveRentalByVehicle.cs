using CSharpFunctionalExtensions;
using FleetRentals.Application.Common;
using FleetRentals.Application.Features.Rentals.Common;
using FleetRentals.Application.Features.Vehicles;
using FleetRentals.Application.Features.Vehicles.Common;
using FluentValidation;
using MediatR;

namespace FleetRentals.Application.Features.Rentals;

public sealed record GetActiveRentalByVehicleQuery(Guid VehicleId) : IRequest<Result<RentalDto, Error>>;

public sealed class GetActiveRentalByVehicleValidator : AbstractValidator<GetActiveRentalByVehicleQuery>
{
    public GetActiveRentalByVehicleValidator() => RuleFor(x => x.VehicleId)
        .NotEmpty().WithErrorCode("vehicle.id.invalid").WithMessage("Vehicle id cannot be empty.");
}

public sealed class GetActiveRentalByVehicleQueryHandler(
    IVehicleRepository vehicleRepository,
    IRentalRepository rentalRepository) : IRequestHandler<GetActiveRentalByVehicleQuery, Result<RentalDto, Error>>
{
    public async Task<Result<RentalDto, Error>> Handle(
        GetActiveRentalByVehicleQuery query, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(query.VehicleId, cancellationToken);
        if (vehicle is null)
            return Error.NotFound("vehicle.not_found", "Vehicle was not found.");

        var rental = await rentalRepository.GetActiveByVehicleIdAsync(query.VehicleId, cancellationToken);
        return rental is null
            ? Error.NotFound("rental.active.not_found", "Vehicle has no active rental.")
            : RentalDto.From(rental);
    }
}
