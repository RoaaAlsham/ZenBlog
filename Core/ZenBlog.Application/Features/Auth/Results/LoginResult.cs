namespace ZenBlog.Application.Features.Auth.Results
{
    public record LoginResult(
        string UserId,
        string Email,
        string Username,
        string FirstName,
        string LastName,
        string? ImageUrl,
        string Token,
        DateTime ExpiresAtUtc,
        string RefreshToken,
        DateTime RefreshTokenExpiresAtUtc);
}
