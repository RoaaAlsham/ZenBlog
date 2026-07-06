namespace ZenBlog.Application.Models
{
    // Bound from the "JwtSettings" section in appsettings.json + User Secrets.
    // Kept in Application (not Infrastructure) because it's just data with
    // no dependency on the JWT library itself — both Application (if it ever
    // needs expiry info) and Infrastructure (to build/validate tokens) can see it.
    public class JwtSettings
    {
        public string Secret { get; set; } = default!;
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public int ExpiryMinutes { get; set; }
    }
}
