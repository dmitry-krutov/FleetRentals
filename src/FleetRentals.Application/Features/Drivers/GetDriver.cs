using CSharpFunctionalExtensions;
using FleetRentals.Application.Common;
using FleetRentals.Application.Features.Drivers.Common;
using FluentValidation;
using MediatR;

namespace FleetRentals.Application.Features.Drivers;

public sealed record GetDriverQuery(Guid Id) : IRequest<Result<DriverDto, Error>>;

public sealed class GetDriverValidator : AbstractValidator<GetDriverQuery>
{
    public GetDriverValidator() => RuleFor(x => x.Id)
        .NotEmpty().WithErrorCode("driver.id.invalid").WithMessage("Driver id cannot be empty.");
}

public sealed class GetDriverQueryHandler(IDriverRepository repository)
    : IRequestHandler<GetDriverQuery, Result<DriverDto, Error>>
{
    public async Task<Result<DriverDto, Error>> Handle(
        GetDriverQuery query, CancellationToken cancellationToken)
    {
        var driver = await repository.GetByIdAsync(query.Id, cancellationToken);

        return driver is null
            ? Error.NotFound("driver.not_found", "Driver was not found.")
            : DriverDto.From(driver);
    }
}
