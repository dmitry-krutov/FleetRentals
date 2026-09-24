using CSharpFunctionalExtensions;
using FleetRentals.Application.Common;
using FleetRentals.Application.Features.Rentals.Common;
using FleetRentals.Domain.Rentals;
using FluentValidation;
using MediatR;

namespace FleetRentals.Application.Features.Rentals;

public sealed record StartRentalCommand(Guid VehicleId, Guid DriverId) : IRequest<Result<RentalDto, Error>>;

public sealed class StartRentalValidator : AbstractValidator<StartRentalCommand>
{
    public StartRentalValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithErrorCode("vehicle.id.invalid").WithMessage("Vehicle id cannot be empty.");
        RuleFor(x => x.DriverId)
            .NotEmpty().WithErrorCode("driver.id.invalid").WithMessage("Driver id cannot be empty.");
    }
}

public sealed class StartRentalCommandHandler(
    IRentalRepository repository,
    TimeProvider timeProvider) : IRequestHandler<StartRentalCommand, Result<RentalDto, Error>>
{
    public async Task<Result<RentalDto, Error>> Handle(
        StartRentalCommand command, CancellationToken cancellationToken)
    {
        var rental = new Rental(
            Guid.NewGuid(), command.VehicleId, command.DriverId, timeProvider.GetUtcNow());
        var outcome = await repository.TryAddAsync(rental, cancellationToken);
        return outcome switch
        {
            RentalInsertOutcome.Inserted => RentalDto.From(rental),
            RentalInsertOutcome.VehicleNotFound =>
                Error.NotFound("vehicle.not_found", "Vehicle was not found."),
            RentalInsertOutcome.DriverNotFound =>
                Error.NotFound("driver.not_found", "Driver was not found."),
            RentalInsertOutcome.VehicleAlreadyRented =>
                Error.Conflict("vehicle.already_rented", "Vehicle already has an active rental."),
            RentalInsertOutcome.DriverAlreadyRented =>
                Error.Conflict("rental.driver.busy", "Driver already has an active rental."),
            _ => throw new InvalidOperationException($"Unexpected rental insert outcome: {outcome}."),
        };
    }
}
