using AutoMapper;
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
    public class UpdateCategoryCommandHandler(
        IRepository<Category> repository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IUserQueryService userQuery,
        IActivityLogger activityLogger) : IRequestHandler<UpdateCategoryCommand, BaseResult<bool>>
    {
        public async Task<BaseResult<bool>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await repository.GetByIdAsync(request.Id, cancellationToken);
            if (category == null)
            {
                return BaseResult<bool>.Failure($"Category with ID {request.Id} not found.");
            }

            mapper.Map(request, category);
            await repository.UpdateAsync(category);
            var response = await unitOfWork.SaveChangesAsync();
            if (!response)
            {
                return BaseResult<bool>.Failure("Failed to update category.");
            }

            var (actorId, actorName) = await ActivityActor.ResolveAsync(currentUser, userQuery, cancellationToken);
            await activityLogger.LogAsync(
                ActivityActions.CategoryUpdated,
                $"Updated category '{category.CategoryName}'",
                actorId,
                actorName,
                nameof(Category),
                category.Id.ToString(),
                cancellationToken: cancellationToken);

            return BaseResult<bool>.Success(true);
        }
    }
}
