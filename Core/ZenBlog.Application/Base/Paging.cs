namespace ZenBlog.Application.Base;

/// <summary>
/// Shared helpers for clamping page/pageSize query inputs.
/// </summary>
public static class Paging
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;
    public const int DefaultCommentsPageSize = 20;
    public const int MaxPageSize = 50;

    public static (int Page, int PageSize) Normalize(
        int? page,
        int? pageSize,
        int defaultPageSize = DefaultPageSize)
    {
        var p = page is null or < 1 ? DefaultPage : page.Value;
        var size = pageSize is null or < 1
            ? defaultPageSize
            : Math.Min(pageSize.Value, MaxPageSize);
        return (p, size);
    }
}
