using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;
using FleetRentals.Domain.Drivers;
using FleetRentals.Domain.Rentals;
using FleetRentals.Domain.Vehicles;

namespace FleetRentals.Application.Features.Rentals;

public sealed record StartRentalCommand(Guid VehicleId, Guid DriverId);

public sealed class StartRentalCommandHandler(
    IRentalStartSessionFactory sessionFactory,
    TimeProvider timeProvider)
{
    public async Task<Result<RentalDto, Error>> HandleAsync(
        StartRentalCommand command, CancellationToken cancellationToken)
    {
        var vehicleIdResult = VehicleId.Create(command.VehicleId);
        if (vehicleIdResult.IsFailure)
            return vehicleIdResult.Error;

        var driverIdResult = DriverId.Create(command.DriverId);
        if (driverIdResult.IsFailure)
            return driverIdResult.Error;

        await using var session = await sessionFactory.OpenAsync(cancellationToken);
        var vehicle = await session.GetVehicleForUpdateAsync(vehicleIdResult.Value, cancellationToken);
        if (vehicle is null)
            return Error.NotFound("vehicle.not_found", "Vehicle was not found.");

        var driver = await session.GetDriverForUpdateAsync(driverIdResult.Value, cancellationToken);
        if (driver is null)
            return Error.NotFound("driver.not_found", "Driver was not found.");

        if (await session.HasActiveRentalForDriverAsync(driver.Id, cancellationToken))
            return Error.Conflict("rental.driver.busy", "Driver already has an active rental.");

        var rentedResult = vehicle.MarkRented();
        if (rentedResult.IsFailure)
            return rentedResult.Error;

        var rental = Rental.Start(RentalId.NewId(), vehicle.Id, driver.Id, timeProvider.GetUtcNow());
        var insertOutcome = await session.TryInsertAsync(rental, cancellationToken);
        if (insertOutcome == RentalInsertOutcome.VehicleAlreadyRented)
            return Error.Conflict("vehicle.already_rented", "Vehicle already has an active rental.");
        if (insertOutcome == RentalInsertOutcome.DriverAlreadyRented)
            return Error.Conflict("rental.driver.busy", "Driver already has an active rental.");

        if (!await session.UpdateVehicleStatusAsync(vehicle, cancellationToken))
            return Error.Conflict("vehicle.already_rented", "Vehicle already has an active rental.");

        await session.CommitAsync(cancellationToken);
        return RentalDto.From(rental);
    }
}
