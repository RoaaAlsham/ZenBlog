using MediatR;
using ZenBlog.Application.Features.Categories.Queries;
using ZenBlog.Application.Features.Categories.Commands;
namespace ZenBlog.API.Endpoints
{
   //
    public static class CategoryEndpoints
    {
        //Minimal API endpoints
        public static void RegisterCategoryEndpoints(this IEndpointRouteBuilder erb)
        {//extension method to register category endpoints
            // without (this) extension method, we would have to do something like:
            // CategoryEndpoints.RegisterCategoryEndpoints(app) in Program.cs

            var categories = erb.MapGroup("/categories").WithTags("Categories");// all routes starting with /categories will be grouped together 
            categories.MapGet("", async (IMediator _mediator) =>
            {
                var response = await _mediator.Send(new GetCategoryQuery());
                return response.IsSuccess ? Results.Ok(response.Data) : Results.BadRequest(response.Errors);
            });
            categories.MapPost("", async (IMediator _mediator, CreateCategoryCommand command) =>
            {
                var response = await _mediator.Send(command);

                if (response.IsFailure)
                {
                    return Results.BadRequest(new { Errors = response.Errors });
                }

                // Ideally, you return 201 Created with the ID of the created category
                return response.IsSuccess ? Results.Created($"/categories/{response.Data}", null) : Results.BadRequest("Could not create the category instance");
            }).RequireAuthorization();

            categories.MapGet("/{id}", async (IMediator _mediator,Guid id) =>
            {
                var response = await _mediator.Send(new GetCategoryByIdQuery(id));
                return response.IsSuccess ? Results.Ok(response.Data) : Results.NotFound(response.Errors);
            });

            categories.MapPut("/{id:guid}", async (IMediator _mediator, Guid id, UpdateCategoryCommand command) =>
            {
                if (id != command.Id)
                {
                    return Results.BadRequest("ID in the URL does not match ID in the body.");
                }
                var response = await _mediator.Send(command);
                return response.IsSuccess ? Results.NoContent() : Results.BadRequest(new { Errors=response.Errors});
            }).RequireAuthorization();

            categories.MapDelete("/{id:guid}", async (IMediator _mediator, Guid id) =>
            {
                var response = await _mediator.Send(new RemoveCategoryCommand(id));
                return response.IsSuccess ? Results.NoContent() : Results.BadRequest(new { Errors = response.Errors });
            }).RequireAuthorization();

        }
    }
    // Using Controllers would be like: 
    /*
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase {
    private readonly IMediator _m;
    public CategoryController( IMediator m) => _m = m;
    [HttpGet] 
    public async Task<IActionResult> GetAll()
    { var r = await _m.Send( new GetCategoryQuery());
    return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
    }
     */
}
