namespace ZenBlog.Application.Features.Monitoring;

public static class ActivityActions
{
    public const string AuthRegistered = "Auth.Registered";
    public const string BlogCreated = "Blog.Created";
    public const string BlogUpdated = "Blog.Updated";
    public const string BlogDeleted = "Blog.Deleted";
    public const string CommentCreated = "Comment.Created";
    public const string CommentUpdated = "Comment.Updated";
    public const string CommentDeleted = "Comment.Deleted";
    public const string CategoryCreated = "Category.Created";
    public const string CategoryUpdated = "Category.Updated";
    public const string CategoryDeleted = "Category.Deleted";
    public const string UserDeleted = "User.Deleted";
    // "User.PromotedToAdmin" is no longer written: role changes happen in AuthDeep, not
    // here. Historical rows keep the string, so the monitoring filter still lists it.
    public const string SettingsUpdated = "Settings.Updated";
}
