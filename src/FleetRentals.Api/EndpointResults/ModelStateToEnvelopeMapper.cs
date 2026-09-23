using FleetRentals.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FleetRentals.Api.EndpointResults;

public static class ModelStateToEnvelopeMapper
{
    public static IActionResult ToBadRequest(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e =>
            {
                var message = string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? "Invalid value."
                    : e.ErrorMessage;

                return Error.Validation(
                    code: "common.validation.invalid_input",
                    message: message,
                    invalidField: x.Key);
            }))
            .ToList();

        return new BadRequestObjectResult(
            Envelope.Error(new ErrorList(errors)));
    }
}
