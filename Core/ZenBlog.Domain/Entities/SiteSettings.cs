using ZenBlog.Domain.Entities.Common;

namespace ZenBlog.Domain.Entities
{
    /// <summary>
    /// Singleton site-wide configuration. Exactly one row should exist;
    /// use <see cref="SingletonId"/> to load it.
    /// </summary>
    public class SiteSettings : BaseEntity
    {
        public static readonly Guid SingletonId = Guid.Parse("a1000000-0000-4000-8000-000000000001");

        public bool AllowRegistrations { get; set; }
    }
}
