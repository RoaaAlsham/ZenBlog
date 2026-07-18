using MediatR;
using Microsoft.AspNetCore.Identity;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users.Handlers;

public class DeleteMyAccountCommandHandler(
    UserManager<AppUser> userManager,
    ICurrentUserService currentUser,
    IRepository<Comment> commentRepository,
    IRepository<Blog> blogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMyAccountCommand, BaseResult<bool>>
{
    public async Task<BaseResult<bool>> Handle(
        DeleteMyAccountCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return BaseResult<bool>.Unauthorized("You are not authenticated.");
        }

        var user = await userManager.FindByIdAsync(currentUser.UserId);
        if (user is null)
        {
            return BaseResult<bool>.NotFound("User not found.");
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!passwordValid)
        {
            return BaseResult<bool>.Failure(new[]
            {
                new Error
                {
                    PropertyName = "CurrentPassword",
                    ErrorMessage = "Incorrect password."
                }
            });
        }

        await UserAccountHardDelete.PurgeContentAsync(
            user.Id,
            commentRepository,
            blogRepository,
            cancellationToken);
        _ = await unitOfWork.SaveChangesAsync();

        var deleteResult = await userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
            return BaseResult<bool>.Failure(errors);
        }

        return BaseResult<bool>.Success(true);
    }
}
