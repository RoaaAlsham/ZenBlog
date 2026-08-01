using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Monitoring;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Categories.Commands;
using ZenBlog.Application.Features.Categories.Results;
using ZenBlog.Application.Features.Monitoring;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Categories.Handlers;

public class CreateCategoryCommandHandler(
    IRepository<Category> repository,
    IUnitOfWork ufw,
    IMapper mapper,
    ICurrentUserService currentUser,
    IUserQueryService userQuery,
    IActivityLogger activityLogger)
    : IRequestHandler<CreateCategoryCommand, BaseResult<CreateCategoryResult>>
{
    public async Task<BaseResult<CreateCategoryResult>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = mapper.Map<Category>(request);

        await repository.CreateAsync(category);
        var result = await ufw.SaveChangesAsync();

        if (!result)
        {
            return BaseResult<CreateCategoryResult>.Failure("Failed to create category");
        }

        var (actorId, actorName) = await ActivityActor.ResolveAsync(currentUser, userQuery, cancellationToken);
        await activityLogger.LogAsync(
            ActivityActions.CategoryCreated,
            $"Created category '{category.CategoryName}'",
            actorId,
            actorName,
            nameof(Category),
            category.Id.ToString(),
            cancellationToken: cancellationToken);

        return BaseResult<CreateCategoryResult>.Success(new CreateCategoryResult
        {
            Id = category.Id,
            CategoryName = category.CategoryName
        });
    }
}
