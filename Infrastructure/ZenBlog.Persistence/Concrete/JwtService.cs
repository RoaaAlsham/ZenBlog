using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ZenBlog.Application.Contracts;
using ZenBlog.Application.Features.Users.Results;
using ZenBlog.Application.Options;
using ZenBlog.Domain.Entities;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ZenBlog.Persistence.Concrete
{
    public class JwtService(UserManager<AppUser> user_manager, IOptions<JwtTokenOptions> tokenOption) : IJwtService
    {
        private readonly JwtTokenOptions _jwtTokenOption = tokenOption.Value;
        public async Task<GetLoginQueryResult> GenerateJwtTokenAsync(GetAllUsersQueryResult userResult)
        {
            SymmetricSecurityKey key = new (Encoding.UTF8.GetBytes(_jwtTokenOption.SecretKey));
            var dateTimeNow = DateTime.UtcNow;

            List<Claim> claims = new() {

                new Claim(ClaimTypes.NameIdentifier, userResult.Id),
                new Claim(ClaimTypes.Name, userResult.Username),
                new Claim(ClaimTypes.Email, userResult.Email),
                new Claim(ClaimTypes.GivenName, userResult.FullName)

            };

            //We look the user back up by id to get their current roles from Identity and add one ClaimTypes.Role claim per role 

            var identityUser = await user_manager.FindByIdAsync(userResult.Id);
            if (identityUser is not null)
            {
                var roles = await user_manager.GetRolesAsync(identityUser);
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            }

            JwtSecurityToken token = new(
                    issuer: _jwtTokenOption.Issuer,
                    audience: _jwtTokenOption.Audience,
                    claims: claims,
                    expires: dateTimeNow.AddMinutes(_jwtTokenOption.ExpirationMinutes),
                    notBefore: dateTimeNow,
                    signingCredentials : new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                );

            GetLoginQueryResult response = new();
            response.Token = new JwtSecurityTokenHandler().WriteToken(token);
            response.ExpirationTime = dateTimeNow.AddMinutes(_jwtTokenOption.ExpirationMinutes);
            return response;
        }
    }
}
