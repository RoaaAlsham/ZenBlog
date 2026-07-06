using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Models;
using ZenBlog.Infrastructure.Identity;

namespace ZenBlog.Infrastructure.Extensions
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Binds the "JwtSettings" section (Issuer/Audience/ExpiryMinutes from appsettings.json,
            // Secret from User Secrets) into the JwtSettings POCO defined in Application.
            var jwtSettingsSection = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSettingsSection);
            var jwtSettings = jwtSettingsSection.Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");

            // Needed so CurrentUserService can reach the current request's HttpContext.
            services.AddHttpContextAccessor();

            // Application-defined ports -> Infrastructure implementations.
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            // This is what VALIDATES incoming bearer tokens on every request.
            // JwtTokenGenerator above only CREATES tokens at login time - two separate
            // responsibilities that happen to share the same secret/issuer/audience.
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    // Don't grant the default 5-minute grace period past expiry.
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization();
        }
    }
}
