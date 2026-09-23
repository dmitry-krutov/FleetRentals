using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Vehicles;

public sealed record RegisterVehicleCommand(string? LicensePlate);

public sealed class RegisterVehicleCommandHandler(IVehicleRepository repository)
{
    public async Task<Result<VehicleDto, Error>> HandleAsync(
        RegisterVehicleCommand command, CancellationToken cancellationToken)
    {
        var plateResult = LicensePlate.Create(command.LicensePlate);
        if (plateResult.IsFailure)
            return plateResult.Error;

        var vehicle = Vehicle.Register(VehicleId.NewId(), plateResult.Value);
        if (!await repository.AddAsync(vehicle, cancellationToken))
            return Error.Conflict("vehicle.license_plate.exists", "A vehicle with this license plate already exists.");

        return VehicleDto.From(vehicle);
    }
}
