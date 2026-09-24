using CSharpFunctionalExtensions;
using FleetRentals.Application.Common;
using FleetRentals.Application.Features.Vehicles.Common;
using FluentValidation;
using MediatR;

namespace FleetRentals.Application.Features.Vehicles;

public sealed record GetVehicleQuery(Guid Id) : IRequest<Result<VehicleDto, Error>>;

public sealed class GetVehicleValidator : AbstractValidator<GetVehicleQuery>
{
    public GetVehicleValidator() => RuleFor(x => x.Id)
        .NotEmpty().WithErrorCode("vehicle.id.invalid").WithMessage("Vehicle id cannot be empty.");
}

public sealed class GetVehicleQueryHandler(IVehicleRepository repository)
    : IRequestHandler<GetVehicleQuery, Result<VehicleDto, Error>>
{
    public async Task<Result<VehicleDto, Error>> Handle(
        GetVehicleQuery query, CancellationToken cancellationToken)
    {
        var vehicle = await repository.GetByIdAsync(query.Id, cancellationToken);

        return vehicle is null
            ? Error.NotFound("vehicle.not_found", "Vehicle was not found.")
            : VehicleDto.From(vehicle);
    }
}
