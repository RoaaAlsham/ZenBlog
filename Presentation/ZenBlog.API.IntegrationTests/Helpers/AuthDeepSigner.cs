using System.Security.Cryptography;
using System.Text;

namespace ZenBlog.API.IntegrationTests.Helpers;

/// <summary>
/// Independent re-implementation of AuthDeep's request signing, mirroring what the
/// gateway does rather than reusing the middleware's internals — so a canonicalisation
/// bug in the middleware cannot cancel itself out in the tests.
/// </summary>
public static class AuthDeepSigner
{
    /// <summary>Builds the "t={unix},v1={hex}" value for X-Gateway-Signature.</summary>
    public static string BuildSignatureHeader(
        string serviceSecret,
        string method,
        string path,
        long timestamp,
        byte[]? body = null)
    {
        var bodyHash = Convert.ToHexStringLower(SHA256.HashData(body ?? []));

        // The gateway strips a trailing slash before signing.
        var signedPath = path.Length > 1 && path[^1] == '/' ? path[..^1] : path;

        // METHOD\npath\ntimestamp\nhex(sha256(body)) — no trailing newline, query string excluded.
        var payload = string.Concat(
            method.ToUpperInvariant(), "\n",
            signedPath, "\n",
            timestamp.ToString(), "\n",
            bodyHash);

        // The whole ssk_ string is the key, as UTF-8 bytes.
        var mac = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(serviceSecret),
            Encoding.UTF8.GetBytes(payload));

        return $"t={timestamp},v1={Convert.ToHexStringLower(mac)}";
    }

    /// <summary>Attaches a valid gateway key + signature and the identity headers AuthDeep forwards.</summary>
    /// <param name="authType">
    /// session | web_token | api_key. Left unset by default so existing tests keep
    /// exercising the "gateway sent no Auth-Type" fallback.
    /// </param>
    /// <param name="apiKeyId">Set alongside <paramref name="authType"/> "api_key".</param>
    public static HttpRequestMessage Sign(
        this HttpRequestMessage request,
        string gatewayKey,
        string serviceSecret,
        byte[]? body = null,
        long? timestamp = null,
        string? userId = null,
        string? roles = null,
        string? authType = null,
        string? apiKeyId = null)
    {
        var effectiveTimestamp = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var path = request.RequestUri!.IsAbsoluteUri
            ? request.RequestUri.AbsolutePath
            : request.RequestUri.ToString().Split('?')[0];

        request.Headers.Add("X-Gateway-Key", gatewayKey);
        request.Headers.Add("X-Gateway-Signature", BuildSignatureHeader(
            serviceSecret, request.Method.Method, path, effectiveTimestamp, body));

        if (userId is not null)
        {
            request.Headers.Add("X-AuthDeep-User-ID", userId);
            request.Headers.Add("X-AuthDeep-User-Email", $"{userId}@authdeep.test");
            request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
        }

        if (roles is not null)
        {
            request.Headers.Add("X-AuthDeep-User-Roles", roles);
        }

        if (authType is not null)
        {
            request.Headers.Add("X-AuthDeep-Auth-Type", authType);
        }

        if (apiKeyId is not null)
        {
            request.Headers.Add("X-AuthDeep-API-Key-ID", apiKeyId);
            request.Headers.Add("X-AuthDeep-API-Key-Type", "service");
        }

        return request;
    }
}
