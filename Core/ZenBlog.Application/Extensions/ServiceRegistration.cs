
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZenBlog.Application.Behaviors;
using ZenBlog.Application.Features.Categories.Mapping;
using ZenBlog.Application.Options;

/*
 An assembly is the compiled output of one .csproj project — a .dll file on disk

When you write services.AddAutoMapper(typeof(CategoryMappingProfile).Assembly),
you're telling AutoMapper: "open ZenBlog.Application.dll, find every class that inherits from Profile,
and register them all."

The same logic applies to MediatR: RegisterServicesFromAssembly(typeof(CategoryMappingProfile).Assembly) tells
it to scan the same DLL for every class implementing IRequestHandler<,>
— so GetCategoryQueryHandler, CreateCategoryCommandHandler,
and every future handler you write all get registered in that single line.
 */
namespace ZenBlog.Application.Extensions
{
    public static class ServiceRegistration
    {
        public static void AddApplication(this IServiceCollection services,IConfiguration configuration) {
            // AutoMapper — scans this assembly for all Profile classes
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(typeof(ServiceRegistration).Assembly);
            });

            // MediatR — scans same assembly for all IRequestHandler implementations
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(CategoryMappingProfile).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(CategoryMappingProfile).Assembly);

            services.Configure<JwtTokenOptions>(configuration.GetSection(nameof(JwtTokenOptions)));
        
        }

    }
    /*
cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
// order: Logging → Validation → Handler

 Pipeline behaviors run in the order they are registered 
     */
}
