namespace ZenBlog.Application.Options
{
    public class JwtTokenOptions
    {
        public string Issuer { get; set; } = string.Empty;  
        public string Audience { get; set; }

        public string SecretKey { get; set; } = string.Empty;   // Secret key used for signing the JWT token

        public int ExpirationMinutes { get; set; } = 60;   // Token expiration time in minutes
    }
}
