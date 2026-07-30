using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Features.Users.Queries;
using ZenBlog.Application.Features.Users.Results;

namespace ZenBlog.Application.Features.Users.Handlers
{
    public class GetAllUsersQueryHandler(
        IUserQueryService userQuery,
        IRoleChecker roleChecker)
        : IRequestHandler<GetAllUsersQuery, BaseResult<IEnumerable<GetAllUsersQueryResult>>>
    {
        public async Task<BaseResult<IEnumerable<GetAllUsersQueryResult>>> Handle(
            GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await userQuery.GetAllAsync(cancellationToken);

            var result = new List<GetAllUsersQueryResult>();
            foreach (var u in users)
            {
                var isAdmin = await roleChecker.IsInRoleAsync(
                    u.Id,
                    UserAccountHardDelete.AdminRoleName,
                    cancellationToken);

                result.Add(new GetAllUsersQueryResult(
                    Id: u.Id,
                    Username: u.UserName!,
                    Email: u.Email!,
                    FullName: $"{u.FirstName} {u.LastName}",
                    ImageUrl: u.ImageUrl,
                    IsAdmin: isAdmin
                ));
            }

            return BaseResult<IEnumerable<GetAllUsersQueryResult>>.Success(result);
        }
    }
}
