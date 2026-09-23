using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;
using FleetRentals.Domain.Rentals;

namespace FleetRentals.Application.Features.Rentals;

public sealed record GetRentalQuery(Guid Id);

public sealed class GetRentalQueryHandler(IRentalReadRepository repository)
{
    public async Task<Result<RentalDto, Error>> HandleAsync(
        GetRentalQuery query, CancellationToken cancellationToken)
    {
        var idResult = RentalId.Create(query.Id);
        if (idResult.IsFailure)
            return idResult.Error;

        var rental = await repository.GetByIdAsync(idResult.Value, cancellationToken);
        return rental is null
            ? Error.NotFound("rental.not_found", "Rental was not found.")
            : RentalDto.From(rental);
    }
}
