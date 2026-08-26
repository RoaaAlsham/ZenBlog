using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Queries;
using ZenBlog.Application.Features.Users.Results;

namespace ZenBlog.Application.Features.Users.Handlers
{
    /// <summary>
    /// Lists the local user rows. Deliberately says nothing about roles: AuthDeep owns
    /// them and asserts them per request, so this service only ever knows the roles of
    /// the caller in front of it — never those of the users in a list.
    /// </summary>
    public class GetAllUsersQueryHandler(IUserQueryService userQuery)
        : IRequestHandler<GetAllUsersQuery, BaseResult<IEnumerable<GetAllUsersQueryResult>>>
    {
        public async Task<BaseResult<IEnumerable<GetAllUsersQueryResult>>> Handle(
            GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await userQuery.GetAllAsync(cancellationToken);

            var result = users.Select(u => new GetAllUsersQueryResult(
                Id: u.Id,
                Username: u.UserName!,
                Email: u.Email!,
                FullName: $"{u.FirstName} {u.LastName}",
                ImageUrl: u.ImageUrl
            )).ToList();

            return BaseResult<IEnumerable<GetAllUsersQueryResult>>.Success(result);
        }
    }
}
