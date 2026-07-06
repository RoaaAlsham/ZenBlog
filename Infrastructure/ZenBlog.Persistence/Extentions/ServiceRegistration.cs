using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Options;
using ZenBlog.Domain.Entities;
using ZenBlog.Persistence.Concrete;
using ZenBlog.Persistence.Context;
using ZenBlog.Persistence.Intercepters.ZenBlog.Persistence.Interceptors;
using ZenBlog.Application.Contracts;

namespace ZenBlog.Persistence.Extentions
{
    public static class ServiceRegistration
    {
        public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration) { 
            services.AddDbContext<AppDbContext>(options => {
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

            // JWT authentication wiring.


            var jwtOptions = configuration.GetSection(nameof(JwtTokenOptions)).Get<JwtTokenOptions>()
                ?? new JwtTokenOptions();

            services.AddAuthentication(options =>
                {
                    // Make JWT bearer the default scheme for both authentication and challenges,
                    // so [Authorize]/.RequireAuthorization() know which handler to invoke.
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // Reject tokens signed by anyone other than us, for anyone other than us,
                        // and reject expired tokens - all four checks should normally be on.
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

                        // The library's default clock skew allowance is 5 minutes; tighten it to
                        // 1 minute here since server clocks in this deployment are expected to be
                        // NTP-synced, so tokens don't stay technically valid long after expiry.
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IJwtService, JwtService>();
        }
    }
}
