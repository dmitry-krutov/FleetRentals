using CSharpFunctionalExtensions;
using FleetRentals.Application.Common;
using FleetRentals.Application.Features.Vehicles.Common;
using FleetRentals.Domain.Vehicles;
using FluentValidation;
using MediatR;

namespace FleetRentals.Application.Features.Vehicles;

public sealed record RegisterVehicleCommand(string? LicensePlate) : IRequest<Result<VehicleDto, Error>>;

public sealed class RegisterVehicleValidator : AbstractValidator<RegisterVehicleCommand>
{
    public RegisterVehicleValidator()
    {
        RuleFor(x => x.LicensePlate)
            .NotEmpty().WithErrorCode("vehicle.license_plate.required").WithMessage("License plate is required.")
            .Must(x => x is null || x.Trim().Length <= 32)
            .WithErrorCode("vehicle.license_plate.too_long")
            .WithMessage("License plate cannot exceed 32 characters.");
    }
}

public sealed class RegisterVehicleCommandHandler(IVehicleRepository repository)
    : IRequestHandler<RegisterVehicleCommand, Result<VehicleDto, Error>>
{
    public async Task<Result<VehicleDto, Error>> Handle(
        RegisterVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle(
            Guid.NewGuid(),
            command.LicensePlate!.Trim().ToUpperInvariant(),
            VehicleStatus.Available);

        if (!await repository.AddAsync(vehicle, cancellationToken))
            return Error.Conflict("vehicle.license_plate.exists", "A vehicle with this license plate already exists.");

        return VehicleDto.From(vehicle);
    }
}
