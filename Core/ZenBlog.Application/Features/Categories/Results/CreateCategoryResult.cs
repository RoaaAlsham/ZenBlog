namespace ZenBlog.Application.Features.Categories.Results;

public class CreateCategoryResult
{
    public Guid Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
