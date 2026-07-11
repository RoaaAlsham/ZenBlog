namespace ZenBlog.Application.Features.Auth.Results
{
    public record LoginResult(
        string UserId,
        string Email,
        string FirstName,
        string LastName,
        string? ImageUrl,
        string Token,
        DateTime ExpiresAtUtc,
        string RefreshToken,
        DateTime RefreshTokenExpiresAtUtc);
}
