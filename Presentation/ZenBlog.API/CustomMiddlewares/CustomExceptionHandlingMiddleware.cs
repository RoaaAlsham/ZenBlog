using FluentValidation;
using ZenBlog.Application.Base;
namespace ZenBlog.API.CustomMiddlewares
{
    public class CustomExceptionHandlingMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try { await next(context); }
            catch (ValidationException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                var response = new BaseResult<object>()
                {
                    Errors = ex.Errors.GroupBy(x => x.PropertyName).Select(g => new Error
                    {
                        PropertyName = g.Key,
                        ErrorMessage = g.Select(x => x.ErrorMessage).FirstOrDefault()
                    }).ToList()

                };
                await context.Response.WriteAsJsonAsync(response);
            }
            catch (BadHttpRequestException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                var response = BaseResult<object>.Failure("Invalid request body. Please check your JSON structure and property names.");
                await context.Response.WriteAsJsonAsync(response);
            }
            catch (ArgumentNullException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                var response = BaseResult<object>.Failure("Missing or invalid request body.");
                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
               
                var response = BaseResult<object>.Failure("Unexpected Error: "+ex.Message);
                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
