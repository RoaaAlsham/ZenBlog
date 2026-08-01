using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Categories.Commands;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Categories.Handlers
{
    public class RemoveCategoryCommandHandler(
        IRepository<Category> repository,
        IRepository<Blog> blogRepository,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IUserQueryService userQuery,
        IActivityLogger activityLogger) : IRequestHandler<RemoveCategoryCommand, BaseResult<bool>>
    {
        public async Task<BaseResult<bool>> Handle(RemoveCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await repository.GetByIdAsync(request.guid, cancellationToken);
            if (category == null)
            {
                return BaseResult<bool>.NotFound($"Category with id {request.guid} not found.");
            }

            var blogInCategory = await blogRepository.GetSingleAsync(
                b => b.CategoryId == request.guid,
                cancellationToken);
            if (blogInCategory is not null)
            {
                return BaseResult<bool>.Failure(
                    "Cannot delete a category that still has blogs. Reassign or delete those blogs first.");
            }

            var categoryName = category.CategoryName;
            var categoryId = category.Id;
            await repository.DeleteAsync(category);
            var response = await uow.SaveChangesAsync();
            if (!response)
            {
                return BaseResult<bool>.Failure("Failed to remove category.");
            }

            var (actorId, actorName) = await ActivityActor.ResolveAsync(currentUser, userQuery, cancellationToken);
            await activityLogger.LogAsync(
                ActivityActions.CategoryDeleted,
                $"Deleted category '{categoryName}'",
                actorId,
                actorName,
                nameof(Category),
                categoryId.ToString(),
                cancellationToken: cancellationToken);

            return BaseResult<bool>.Success(true);
        }
    }
}
