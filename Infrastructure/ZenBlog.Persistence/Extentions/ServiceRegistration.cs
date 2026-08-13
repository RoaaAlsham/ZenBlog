using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Domain.Entities;
using ZenBlog.Persistence.Concrete;
using ZenBlog.Persistence.Context;
using ZenBlog.Persistence.Identity;
using ZenBlog.Persistence.Intercepters.ZenBlog.Persistence.Interceptors;
using ZenBlog.Persistence.Options;

namespace ZenBlog.Persistence.Extentions
{
    public static class ServiceRegistration
    {
        public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration) { 
            services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));

            services.AddDbContext<AppDbContext>((serviceProvider, options) => {
                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                if (environment.IsEnvironment("Testing"))
                {
                    var databaseName = configuration["InMemoryDatabaseName"] ?? "ZenBlogTests";
                    options.UseInMemoryDatabase(databaseName);
                    options.AddInterceptors(new AuditDbContextInterceptor());
                    return;
                }

                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        // Optional: Configure PostgreSQL-specific options
                        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                    });
                options.AddInterceptors(new AuditDbContextInterceptor());
                options.UseLazyLoadingProxies();
            });

            services.AddIdentity<AppUser, AppRole>(options => { 
               options.User.RequireUniqueEmail = true;
            }).AddEntityFrameworkStores<AppDbContext>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

            // Backs the provisioner's "already seen this subject" check, so the common
            // request does not pay for a lookup.
            services.AddMemoryCache();
            services.AddScoped<IAuthDeepUserProvisioner, AuthDeepUserProvisioner>();
        }
    }
}
