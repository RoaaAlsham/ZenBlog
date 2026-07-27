using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Settings;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users.Handlers
{
    public class CreateUserCommandHandler(
        IUserQueryService userQuery,
        IUserAccountService userAccount,
        IMapper mapper,
        IRepository<SiteSettings> settingsRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateUserCommand, BaseResult<CreateUserResult>>
    {

        public async Task<BaseResult<CreateUserResult>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var settings = await SiteSettingsAccess.GetOrCreateAsync(
                settingsRepository,
                unitOfWork,
                cancellationToken);

            if (!settings.AllowRegistrations)
            {
                return BaseResult<CreateUserResult>.Failure("Registration is currently disabled.");
            }

            var existingUser = await userQuery.FindByEmailAsync(request.Email, cancellationToken);
            if (existingUser != null)
            {
                return BaseResult<CreateUserResult>.Failure("Email is already in use.");
            }
            var user = mapper.Map<AppUser>(request);
            user.Id= Guid.NewGuid().ToString();

            var result = await userAccount.CreateAsync(user, request.Password, cancellationToken);
            if (!result.Succeeded)
            {
                var errors = string.Join(",", result.Errors);
                return BaseResult<CreateUserResult>.Failure(errors);
            }

        

            return BaseResult<CreateUserResult>.Success(new CreateUserResult(
            Id: user.Id,
            Username: user.UserName!,
            Email: user.Email!,
            FullName: $"{user.FirstName} {user.LastName}",
            CreatedAt: DateTime.UtcNow
             ));

        }
    }
}
