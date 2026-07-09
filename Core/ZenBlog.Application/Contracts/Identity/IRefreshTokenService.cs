namespace ZenBlog.Application.Contracts.Identity;

public interface IRefreshTokenService
{
    (string Token, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken(int ttlDays);
    string HashToken(string token);
}
