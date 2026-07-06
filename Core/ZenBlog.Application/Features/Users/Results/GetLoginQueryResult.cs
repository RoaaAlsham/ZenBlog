

namespace ZenBlog.Application.Features.Users.Results
{
    public class GetLoginQueryResult
    {
        public string Token { get; set; }

        public DateTime ExpirationTime { get; set;}
    }
}
