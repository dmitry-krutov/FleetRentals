using System.Text.Json.Serialization;
using FleetRentals.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FleetRentals.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => Error.Validation(
                            "common.validation.invalid_input",
                            string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage,
                            x.Key)))
                        .ToArray();
                    return new BadRequestObjectResult(Envelope.Failure(errors));
                });

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSwaggerGen();
        return services;
    }
}
