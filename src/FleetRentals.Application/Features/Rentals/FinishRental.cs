using CSharpFunctionalExtensions;
using FleetRentals.Application.Common;
using FleetRentals.Application.Features.Rentals.Common;
using FleetRentals.Domain.Rentals;
using FluentValidation;
using MediatR;

namespace FleetRentals.Application.Features.Rentals;

public sealed record FinishRentalCommand(Guid Id) : IRequest<Result<RentalDto, Error>>;

public sealed class FinishRentalValidator : AbstractValidator<FinishRentalCommand>
{
    public FinishRentalValidator() => RuleFor(x => x.Id)
        .NotEmpty().WithErrorCode("rental.id.invalid").WithMessage("Rental id cannot be empty.");
}

public sealed class FinishRentalCommandHandler(
    IRentalRepository repository,
    TimeProvider timeProvider) : IRequestHandler<FinishRentalCommand, Result<RentalDto, Error>>
{
    public async Task<Result<RentalDto, Error>> Handle(
        FinishRentalCommand command, CancellationToken cancellationToken)
    {
        var rental = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (rental is null)
            return Error.NotFound("rental.not_found", "Rental was not found.");
        if (rental.Status == RentalStatus.Finished)
            return Error.Conflict("rental.already_finished", "Rental is already finished.");

        var finishedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        if (finishedAtUtc < rental.StartedAtUtc)
            return Error.Validation("rental.finish_before_start", "Rental cannot finish before it starts.");
        if (!await repository.TryFinishAsync(command.Id, finishedAtUtc, cancellationToken))
            return Error.Conflict("rental.already_finished", "Rental is already finished.");

        rental.FinishedAtUtc = finishedAtUtc;
        return RentalDto.From(rental);
    }
}
