namespace FleetRentals.Api.EndpointResults;

public sealed class NoContentResult : IResult
{
    public static readonly NoContentResult Instance = new();

    private NoContentResult() { }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
        return Task.CompletedTask;
    }
}