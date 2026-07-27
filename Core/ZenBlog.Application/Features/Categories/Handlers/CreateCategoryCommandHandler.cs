using AutoMapper;
using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Categories.Commands;
using ZenBlog.Application.Features.Categories.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Categories.Handlers;

public class CreateCategoryCommandHandler(
    IRepository<Category> repository,
    IUnitOfWork ufw,
    IMapper mapper)
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

        return BaseResult<CreateCategoryResult>.Success(new CreateCategoryResult
        {
            Id = category.Id,
            CategoryName = category.CategoryName
        });
    }
}
