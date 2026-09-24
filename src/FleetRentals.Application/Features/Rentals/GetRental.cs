using CSharpFunctionalExtensions;
using FleetRentals.Application.Common;
using FleetRentals.Application.Features.Rentals.Common;
using FluentValidation;
using MediatR;

namespace FleetRentals.Application.Features.Rentals;

public sealed record GetRentalQuery(Guid Id) : IRequest<Result<RentalDto, Error>>;

public sealed class GetRentalValidator : AbstractValidator<GetRentalQuery>
{
    public GetRentalValidator() => RuleFor(x => x.Id)
        .NotEmpty().WithErrorCode("rental.id.invalid").WithMessage("Rental id cannot be empty.");
}

public sealed class GetRentalQueryHandler(IRentalRepository repository)
    : IRequestHandler<GetRentalQuery, Result<RentalDto, Error>>
{
    public async Task<Result<RentalDto, Error>> Handle(
        GetRentalQuery query, CancellationToken cancellationToken)
    {
        var rental = await repository.GetByIdAsync(query.Id, cancellationToken);
        return rental is null
            ? Error.NotFound("rental.not_found", "Rental was not found.")
            : RentalDto.From(rental);
    }
}
