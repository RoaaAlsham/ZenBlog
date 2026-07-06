using ZenBlog.Application.Features.Users.Results;
namespace ZenBlog.Application.Contracts
{
    public interface IJwtService
    {
        Task<GetLoginQueryResult> GenerateJwtTokenAsync(GetAllUsersQueryResult userResult);
    }
}