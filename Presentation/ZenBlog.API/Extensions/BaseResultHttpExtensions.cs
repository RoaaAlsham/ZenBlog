using ZenBlog.Application.Base;

namespace ZenBlog.API.Extensions;

public static class BaseResultHttpExtensions
{
    public static IResult ToHttpResult<T>(this BaseResult<T> result)
    {
        return result.IsSuccess ? Results.Ok(result.Data) : ToFailureResult(result);
    }

    public static IResult ToHttpNoContentResult(this BaseResult<bool> result)
    {
        return result.IsSuccess ? Results.NoContent() : ToFailureResult(result);
    }

    public static IResult ToHttpDeleteResult(this BaseResult<bool> result)
    {
        return result.IsSuccess ? Results.Ok() : ToFailureResult(result);
    }

    private static IResult ToFailureResult<T>(BaseResult<T> result)
    {
        return result.Status switch
        {
            ResultStatus.NotFound => Results.NotFound(result.Errors),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(result.Errors)
        };
    }
}
