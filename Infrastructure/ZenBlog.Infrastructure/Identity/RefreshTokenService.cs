using System.Security.Cryptography;
using System.Text;
using ZenBlog.Application.Contracts.Identity;

namespace ZenBlog.Infrastructure.Identity;

public class RefreshTokenService : IRefreshTokenService
{
    public (string Token, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken(int ttlDays)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(tokenBytes);
        var tokenHash = HashToken(token);
        var expiresAtUtc = DateTime.UtcNow.AddDays(ttlDays);
        return (token, tokenHash, expiresAtUtc);
    }

    public string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
