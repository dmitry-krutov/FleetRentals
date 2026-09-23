using System.Text.Json.Serialization;
using FleetRentals.Api.EndpointResults;

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
                    ModelStateToEnvelopeMapper.ToBadRequest(context.ModelState));

        services.AddSwaggerGen();
        return services;
    }
}
