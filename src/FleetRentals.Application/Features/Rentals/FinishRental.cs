using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;
using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Rentals;

public sealed record FinishRentalCommand(Guid Id);

public sealed class FinishRentalCommandHandler(
    IRentalSessionFactory sessionFactory,
    TimeProvider timeProvider)
{
    public async Task<Result<RentalDto, Error>> HandleAsync(
        FinishRentalCommand command, CancellationToken cancellationToken)
    {
        var idResult = RentalId.Create(command.Id);
        if (idResult.IsFailure)
            return idResult.Error;

        await using var session = await sessionFactory.OpenAsync(cancellationToken);
        var existing = await session.GetRentalAsync(idResult.Value, cancellationToken);
        if (existing is null)
            return Error.NotFound("rental.not_found", "Rental was not found.");
        if (existing.Status == RentalStatus.Finished)
            return Error.Conflict("rental.already_finished", "Rental is already finished.");

        var vehicle = await session.GetVehicleForUpdateAsync(existing.VehicleId, cancellationToken);
        if (vehicle is null)
            return Error.NotFound("vehicle.not_found", "Vehicle was not found.");

        var driver = await session.GetDriverForUpdateAsync(existing.DriverId, cancellationToken);
        if (driver is null)
            return Error.NotFound("driver.not_found", "Driver was not found.");

        var rental = await session.GetRentalForUpdateAsync(idResult.Value, cancellationToken);
        if (rental is null)
            return Error.NotFound("rental.not_found", "Rental was not found.");

        var finishResult = rental.Finish(timeProvider.GetUtcNow());
        if (finishResult.IsFailure)
            return finishResult.Error;

        var availableResult = vehicle.MarkAvailable();
        if (availableResult.IsFailure)
            return availableResult.Error;

        if (!await session.UpdateRentalFinishAsync(rental, cancellationToken))
            return Error.Conflict("rental.already_finished", "Rental is already finished.");

        if (!await session.UpdateVehicleStatusAsync(
                vehicle, VehicleStatus.Rented, cancellationToken))
            return Error.Conflict("vehicle.already_available", "Vehicle is already available.");

        await session.CommitAsync(cancellationToken);
        return RentalDto.From(rental);
    }
}
