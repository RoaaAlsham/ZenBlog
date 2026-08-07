using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ZenBlog.API.CustomMiddlewares
{
    /// <summary>
    /// Verifies that a request was forwarded by the AuthDeep gateway and not sent
    /// directly to the backend, by recomputing the HMAC-SHA256 signature AuthDeep
    /// attaches to every forwarded request.
    ///
    /// Signed payload (newline separated, NO trailing newline):
    ///     METHOD\npath\ntimestamp\nhex(SHA256(rawBody))
    /// HMAC key: the full service-secret string ("ssk_...") as UTF-8 bytes.
    ///
    /// Anything that fails verification is rejected with 401 and never reaches the
    /// rest of the pipeline. Identity headers are trusted only after that succeeds.
    /// </summary>
    public sealed class AuthDeepGatewayMiddleware
    {
        public const string IdentityItemKey = "AuthDeepIdentity";

        private const string GatewayKeyHeader = "X-Gateway-Key";
        private const string GatewaySignatureHeader = "X-Gateway-Signature";
        private const string GatewayTimestampHeader = "X-Gateway-Timestamp";
        private const string UserIdHeader = "X-AuthDeep-User-ID";
        private const string TenantIdHeader = "X-AuthDeep-Tenant-ID";
        private const string UserEmailHeader = "X-AuthDeep-User-Email";
        private const string UserRolesHeader = "X-AuthDeep-User-Roles";
        private const string RequestIdHeader = "X-Request-Id";

        /// <summary>Maximum tolerated clock skew, in seconds, between the gateway and this host.</summary>
        private const long ReplayWindowSeconds = 300;

        /// <summary>Hex length of a SHA-256 digest.</summary>
        private const int SignatureHexLength = 64;

        private readonly RequestDelegate _next;
        private readonly ILogger<AuthDeepGatewayMiddleware> _logger;
        private readonly IHostEnvironment _environment;
        private readonly string _expectedGatewayKey;

        /// <summary>
        /// UTF-8 bytes of the WHOLE ssk_ string. The secret is deliberately not hex-decoded
        /// and the prefix is not stripped — this matches AuthDeep's reference verifier.
        /// </summary>
        private readonly byte[] _hmacKey;

        public AuthDeepGatewayMiddleware(
            RequestDelegate next,
            AuthDeepGatewayOptions options,
            ILogger<AuthDeepGatewayMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
            _expectedGatewayKey = options.GatewayKey;
            _hmacKey = Encoding.UTF8.GetBytes(options.ServiceSecret);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            // 1. Gateway identity. Non-secret, so an ordinal compare is enough.
            var gatewayKey = request.Headers[GatewayKeyHeader].ToString();
            if (string.IsNullOrEmpty(gatewayKey) || !string.Equals(gatewayKey, _expectedGatewayKey, StringComparison.Ordinal))
            {
                await RejectAsync(context, "missing or foreign X-Gateway-Key");
                return;
            }

            // 2. Signature header: t={unix_seconds},v1={hex_hmac}
            var signatureHeader = request.Headers[GatewaySignatureHeader].ToString();
            if (!TryParseSignatureHeader(signatureHeader, out var timestampValue, out var receivedHex))
            {
                await RejectAsync(context, "malformed X-Gateway-Signature");
                return;
            }

            if (!long.TryParse(timestampValue, NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp))
            {
                await RejectAsync(context, "non-numeric signature timestamp");
                return;
            }

            // 3. AuthDeep also sends the timestamp as its own header. Only the t= value inside
            // the signature is actually covered by the HMAC, so if both are present they must
            // agree — otherwise a proxy could show one timestamp and have another verified.
            var timestampHeader = request.Headers[GatewayTimestampHeader].ToString();
            if (!string.IsNullOrEmpty(timestampHeader)
                && (!long.TryParse(timestampHeader, NumberStyles.None, CultureInfo.InvariantCulture, out var headerTimestamp)
                    || headerTimestamp != timestamp))
            {
                await RejectAsync(context, "X-Gateway-Timestamp disagrees with the signed t= value");
                return;
            }

            // 4. Replay window.
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - timestamp) > ReplayWindowSeconds)
            {
                await RejectAsync(context, $"signature timestamp outside the {ReplayWindowSeconds}s window (skew {now - timestamp}s)");
                return;
            }

            // 5. Recompute over the exact raw body bytes, then rewind so model binding still works.
            var bodyHashHex = await ComputeBodyHashAsync(request, context.RequestAborted);

            var payload = string.Concat(
                request.Method.ToUpperInvariant(), "\n",
                NormalisePath(request.Path.Value), "\n",
                timestampValue, "\n",
                bodyHashHex);

            var expected = HMACSHA256.HashData(_hmacKey, Encoding.UTF8.GetBytes(payload));

            if (!TryDecodeSignature(receivedHex, out var received)
                || !CryptographicOperations.FixedTimeEquals(expected, received))
            {
                LogSignatureMismatch(payload, expected, receivedHex);
                await RejectAsync(context, "signature mismatch");
                return;
            }

            // 6. Verified: the identity headers can now be trusted. Per the AuthDeep contract
            // these outrank any tenant/user id the client supplied in the body or query.
            context.Items[IdentityItemKey] = ReadIdentity(request);

            await _next(context);
        }

        /// <summary>
        /// The gateway signs the path with any trailing slash stripped, so "/api/users/" and
        /// "/api/users" produce the same payload. Without this, the one route mapped with a
        /// trailing slash (GET /api/users/) could never verify.
        /// </summary>
        private static string NormalisePath(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "/";
            }

            return path.Length > 1 && path[^1] == '/' ? path[..^1] : path;
        }

        /// <summary>
        /// Parses "t=...,v1=..." tolerating either field order and surrounding whitespace.
        /// Both fields must be present and non-empty.
        /// </summary>
        private static bool TryParseSignatureHeader(string header, out string timestamp, out string signature)
        {
            timestamp = string.Empty;
            signature = string.Empty;

            if (string.IsNullOrWhiteSpace(header))
            {
                return false;
            }

            foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var name = part[..separator];
                var value = part[(separator + 1)..];

                if (name.Equals("t", StringComparison.Ordinal))
                {
                    timestamp = value;
                }
                else if (name.Equals("v1", StringComparison.Ordinal))
                {
                    signature = value;
                }
            }

            return timestamp.Length > 0 && signature.Length > 0;
        }

        /// <summary>
        /// SHA-256 over the raw request body. Buffering is enabled first so the stream can be
        /// rewound; the hash streams rather than materialising the body, which matters for the
        /// multi-megabyte multipart uploads on /api/media/images. An empty body is valid and
        /// hashes to the digest of zero bytes.
        /// </summary>
        private static async Task<string> ComputeBodyHashAsync(HttpRequest request, CancellationToken cancellationToken)
        {
            request.EnableBuffering();
            request.Body.Position = 0;

            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(request.Body, cancellationToken);

            // Rewind so downstream model binding reads the body from the start.
            request.Body.Position = 0;

            return Convert.ToHexStringLower(hash);
        }

        private static bool TryDecodeSignature(string receivedHex, out byte[] signature)
        {
            signature = [];

            if (receivedHex.Length != SignatureHexLength)
            {
                return false;
            }

            try
            {
                signature = Convert.FromHexString(receivedHex);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static AuthDeepIdentity ReadIdentity(HttpRequest request)
        {
            var roles = request.Headers[UserRolesHeader].ToString();

            return new AuthDeepIdentity(
                UserId: NullIfEmpty(request.Headers[UserIdHeader].ToString()),
                TenantId: NullIfEmpty(request.Headers[TenantIdHeader].ToString()),
                Email: NullIfEmpty(request.Headers[UserEmailHeader].ToString()),
                Roles: roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                RequestId: NullIfEmpty(request.Headers[RequestIdHeader].ToString()));

            static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Diagnostic for canonicalisation drift: prints the exact payload that was signed
        /// plus both digests so they can be diffed against the gateway. Debug level and
        /// suppressed in Production — it never touches the service secret.
        /// </summary>
        private void LogSignatureMismatch(string payload, byte[] expected, string receivedHex)
        {
            if (_environment.IsProduction() || !_logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            _logger.LogDebug(
                "AuthDeep signature mismatch. Reconstructed payload (\\n shown as | ): {Payload} | expected v1={Expected} | received v1={Received}",
                payload.Replace("\n", " | "),
                Convert.ToHexStringLower(expected),
                receivedHex);
        }

        private async Task RejectAsync(HttpContext context, string reason)
        {
            _logger.LogDebug("AuthDeep gateway verification rejected {Method} {Path}: {Reason}",
                context.Request.Method, context.Request.Path, reason);

            // The client always gets the same generic message; the specific reason is a
            // server-side detail and would otherwise help an attacker probe the contract.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "unauthorized",
                message = "Request did not come through the AuthDeep gateway."
            });
        }
    }
}
