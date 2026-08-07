namespace ZenBlog.API.CustomMiddlewares
{
    /// <summary>
    /// Verified AuthDeep gateway credentials, resolved once at startup.
    /// Values come from environment variables only — never from committed config.
    /// </summary>
    public sealed class AuthDeepGatewayOptions
    {
        public const string ServiceSecretKey = "AUTHDEEP_SERVICE_SECRET";
        public const string GatewayKeyKey = "AUTHDEEP_GATEWAY_KEY";

        private AuthDeepGatewayOptions(string serviceSecret, string gatewayKey)
        {
            ServiceSecret = serviceSecret;
            GatewayKey = gatewayKey;
        }

        /// <summary>The full ssk_ string. Used as UTF-8 bytes for the HMAC key — never hex-decoded, never trimmed.</summary>
        public string ServiceSecret { get; }

        /// <summary>The gwk_ value every forwarded request must present in X-Gateway-Key.</summary>
        public string GatewayKey { get; }

        /// <summary>
        /// Reads both values from configuration. When <paramref name="required"/> is true a missing
        /// value throws so the host fails fast at startup instead of silently accepting unsigned
        /// traffic; when false (Testing host) absent values simply disable the middleware.
        /// </summary>
        public static AuthDeepGatewayOptions? FromConfiguration(IConfiguration configuration, bool required)
        {
            var serviceSecret = configuration[ServiceSecretKey];
            var gatewayKey = configuration[GatewayKeyKey];

            var hasServiceSecret = !string.IsNullOrWhiteSpace(serviceSecret);
            var hasGatewayKey = !string.IsNullOrWhiteSpace(gatewayKey);

            if (!required && !hasServiceSecret && !hasGatewayKey)
            {
                return null;
            }

            if (!hasServiceSecret)
            {
                throw new InvalidOperationException(
                    $"{ServiceSecretKey} is missing or empty. Set it to the AuthDeep service secret (the value starting with 'ssk_'); "
                    + "requests forwarded by the gateway cannot be verified without it.");
            }

            if (!hasGatewayKey)
            {
                throw new InvalidOperationException(
                    $"{GatewayKeyKey} is missing or empty. Set it to the AuthDeep gateway key (the value starting with 'gwk_') "
                    + "that forwarded requests must present in the X-Gateway-Key header.");
            }

            return new AuthDeepGatewayOptions(serviceSecret!, gatewayKey!);
        }
    }
}
