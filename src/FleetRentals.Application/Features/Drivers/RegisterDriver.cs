using CSharpFunctionalExtensions;
using FleetRentals.Domain.Common;
using FleetRentals.Domain.Drivers;

namespace FleetRentals.Application.Features.Drivers;

public sealed record RegisterDriverCommand(string? Name);

public sealed class RegisterDriverCommandHandler(IDriverRepository repository)
{
    public async Task<Result<DriverDto, Error>> HandleAsync(
        RegisterDriverCommand command, CancellationToken cancellationToken)
    {
        var nameResult = DriverName.Create(command.Name);
        if (nameResult.IsFailure)
            return nameResult.Error;

        var driver = Driver.Register(DriverId.NewId(), nameResult.Value);
        await repository.AddAsync(driver, cancellationToken);

        return DriverDto.From(driver);
    }
}
