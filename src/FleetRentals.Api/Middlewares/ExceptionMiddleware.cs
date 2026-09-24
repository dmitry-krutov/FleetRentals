using FleetRentals.Application.Common;
using FluentValidation;

namespace FleetRentals.Api.Middlewares;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = exception.Errors.Select(failure => Error.Validation(
                failure.ErrorCode,
                failure.ErrorMessage,
                failure.PropertyName));
            await context.Response.WriteAsJsonAsync(Envelope.Failure(errors));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing the request.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(Envelope.Failure([
                Error.Internal("server.internal", "An unexpected server error occurred."),
            ]));
        }
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder) =>
        builder.UseMiddleware<ExceptionMiddleware>();
}
