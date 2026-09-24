using CSharpFunctionalExtensions;
using FleetRentals.Application.Common;
using FleetRentals.Application.Features.Drivers.Common;
using FleetRentals.Domain.Drivers;
using FluentValidation;
using MediatR;

namespace FleetRentals.Application.Features.Drivers;

public sealed record RegisterDriverCommand(string? Name) : IRequest<Result<DriverDto, Error>>;

public sealed class RegisterDriverValidator : AbstractValidator<RegisterDriverCommand>
{
    public RegisterDriverValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("driver.name.required").WithMessage("Driver name is required.")
            .Must(x => x is null || x.Trim().Length <= 200)
            .WithErrorCode("driver.name.too_long")
            .WithMessage("Driver name cannot exceed 200 characters.");
    }
}

public sealed class RegisterDriverCommandHandler(IDriverRepository repository)
    : IRequestHandler<RegisterDriverCommand, Result<DriverDto, Error>>
{
    public async Task<Result<DriverDto, Error>> Handle(
        RegisterDriverCommand command, CancellationToken cancellationToken)
    {
        var driver = new Driver(Guid.NewGuid(), command.Name!.Trim());
        await repository.AddAsync(driver, cancellationToken);
        return DriverDto.From(driver);
    }
}
