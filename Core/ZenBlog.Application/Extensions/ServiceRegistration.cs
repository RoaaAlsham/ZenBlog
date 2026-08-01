
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZenBlog.Application.Behaviors;
using ZenBlog.Application.Features.Categories.Mapping;

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
        public static void AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            var licenseKey = configuration["LuckyPenny:LicenseKey"];

            // AutoMapper — scans this assembly for all Profile classes
            services.AddAutoMapper(cfg =>
            {
                if (!string.IsNullOrWhiteSpace(licenseKey))
                    cfg.LicenseKey = licenseKey;

                cfg.AddMaps(typeof(ServiceRegistration).Assembly);
            });

            // MediatR — scans same assembly for all IRequestHandler implementations
            services.AddMediatR(cfg =>
            {
                if (!string.IsNullOrWhiteSpace(licenseKey))
                    cfg.LicenseKey = licenseKey;

                cfg.RegisterServicesFromAssembly(typeof(CategoryMappingProfile).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(CategoryMappingProfile).Assembly);
        }

    }
    /*
cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
// order: Logging → Validation → Handler

 Pipeline behaviors run in the order they are registered 
     */
}
