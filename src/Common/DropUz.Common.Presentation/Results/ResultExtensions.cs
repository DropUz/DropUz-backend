using DropUz.Common.Domain;
using Microsoft.AspNetCore.Http;

namespace DropUz.Common.Presentation.Results;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        return ToProblem(result.Error);
    }

    public static IResult ToHttpResult<TValue>(this Result<TValue> result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }

        return ToProblem(result.Error);
    }

    private static IResult ToProblem(Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return TypedResults.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode);
    }
}
