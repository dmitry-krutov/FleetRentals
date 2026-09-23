using CSharpFunctionalExtensions;
using FleetRentals.Domain;
using FleetRentals.Domain.Common;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace FleetRentals.Api.EndpointResults;

public sealed class EndpointResult<TValue> : IResult
{
    private readonly IResult _result;

    private EndpointResult(IResult result) => _result = result;

    private EndpointResult(Result<TValue, Error> result)
    {
        _result = result.IsSuccess
            ? new SuccessResult<TValue>(result.Value)
            : new ErrorsResult(result.Error);
    }

    private EndpointResult(Result<TValue, ErrorList> result)
    {
        _result = result.IsSuccess
            ? new SuccessResult<TValue>(result.Value)
            : new ErrorsResult(result.Error);
    }

    public Task ExecuteAsync(HttpContext httpContext) => _result.ExecuteAsync(httpContext);

    public static implicit operator EndpointResult<TValue>(Result<TValue, Error> result) => new(result);

    public static implicit operator EndpointResult<TValue>(Result<TValue, ErrorList> result) => new(result);

    public static EndpointResult<TValue> Ok(TValue value) =>
        new(new SuccessResult<TValue>(value));

    public static EndpointResult<TValue> Created(string location, TValue value) =>
        new(new SuccessResult<TValue>(value, StatusCodes.Status201Created, location));

    public static EndpointResult<TValue> FromErrors(ErrorList errors) => new(new ErrorsResult(errors));
}

public sealed class EndpointResult : IResult
{
    private readonly IResult _result;

    private EndpointResult(IResult result) => _result = result;

    private EndpointResult(UnitResult<Error> result)
    {
        _result = result.IsSuccess
            ? NoContentResult.Instance
            : new ErrorsResult(result.Error);
    }

    private EndpointResult(UnitResult<ErrorList> result)
    {
        _result = result.IsSuccess
            ? NoContentResult.Instance
            : new ErrorsResult(result.Error);
    }

    public Task ExecuteAsync(HttpContext httpContext) => _result.ExecuteAsync(httpContext);

    public static implicit operator EndpointResult(UnitResult<Error> result) => new(result);

    public static implicit operator EndpointResult(UnitResult<ErrorList> result) => new(result);

    public static EndpointResult NoContent() => new(Results.NoContent());

    public static EndpointResult FromErrors(ErrorList errors) => new(new ErrorsResult(errors));
}