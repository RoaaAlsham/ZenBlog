using MediatR;
using ZenBlog.Application.Base;
using ZenBlog.Application.Features.Categories.Results;

namespace ZenBlog.Application.Features.Categories.Commands
{
    public record CreateCategoryCommand : IRequest<BaseResult<CreateCategoryResult>>
    {
        public string CategoryName { get; init; } = string.Empty;
    }
}
