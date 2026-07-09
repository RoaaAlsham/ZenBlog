namespace ZenBlog.Application.Features.Auth.Results;

public record RefreshTokenResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
