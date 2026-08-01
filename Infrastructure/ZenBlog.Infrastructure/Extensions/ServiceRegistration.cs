using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Media;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Models;
using ZenBlog.Infrastructure.Identity;
using ZenBlog.Infrastructure.Media;
using ZenBlog.Infrastructure.Monitoring;

namespace ZenBlog.Infrastructure.Extensions
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettingsSection = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSettingsSection);
            services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));

            services.AddHttpContextAccessor();

            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IRoleChecker, RoleChecker>();
            services.AddScoped<IUserQueryService, UserQueryService>();
            services.AddScoped<IUserAccountService, UserAccountService>();
            services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();
            services.AddScoped<IClientRequestInfo, ClientRequestInfo>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<ISecurityRequestLogger, SecurityRequestLogger>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtSettings>>((options, jwtSettingsAccessor) =>
                {
                    var jwtSettings = jwtSettingsAccessor.Value;
                    if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
                    {
                        throw new InvalidOperationException("JwtSettings:Secret is missing from configuration.");
                    }

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();
        }
    }
}
